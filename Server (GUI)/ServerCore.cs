using System;
using System.Net.Sockets;
using System.Collections.Generic;
using Shared;
using System.Data;
using System.Threading;
using System.Diagnostics;

namespace Server
{
    public class ServerCore
    {
        // A list of socket connections
        public static List<Socket> Connections = new List<Socket>();
        
        // Database class instance
        private static Database db = new Database();

        // Tick timer variables and events. The server tick period is 50ms
        private static AutoResetEvent thresholdEvent = new AutoResetEvent(false);
        private static Timer Ticks = new Timer(TickTimer, null, 50, 50);
        private static int tickCount = 0;

        // Reusable byte array for heartbeat message. Could probably be stored in Message class
        // so it doesn't need to be remade on the client-side.
        private static byte[] heartbeatBytes = Message.GenerateMessage(null, Types.MessageType.HEARBEAT);

        public static int Main(String[] args)
        {
            // Connect to the database, reset all players socket handles to -1 (no one is logged
            // in yet). Start the TCP socket listener.
            db.Connect();
            ResetAllSockets();
            AsynchronousSocketListener.StartListening();
            
            // Start the tick timer.
            Ticks.Change(50, 50);
            
            return 0;
        }

        // Tick timer event handler.
        private static void TickTimer(object state)
        {
            tickCount++;
            if (tickCount % (5 * 20) == 0) Connections.ForEach(recipient => AsynchronousSocketListener.Send(recipient, heartbeatBytes));
        }

        // Resets a players socket handle (after disconnection).
        public static void ResetPlayerSocket(IntPtr socketHandle)
        {
            string id = db.Query("select BIN_TO_UUID(id) as id from players where socket_id = " +
                socketHandle).Rows[0].Field<string>("id");
            
            string query = "update players set socket_id = -1 where id = UUID_TO_BIN(\"" + id + "\")";
            
            db.Update(query);
        }

        // Iterates though all rows in the players table in the database, resetting the socket field to -1.
        // Helps to minimize "already logged in" errors.
        public static void ResetAllSockets()
        {
            DataTable dt = db.Query("select BIN_TO_UUID(id) as id from players");
            
            foreach (DataRow row in dt.Rows)
            {
                string updateQuery = String.Format("update players set socket_id = -1 where id = UUID_TO_BIN(\"{0}\")",
                    row.Field<string>("id"));
                
                db.Update(updateQuery);
            }
        }

        // Dispatches incoming messages.
        public static void TakeAction(IntPtr socketHandle, ushort type, byte[] messageBytes)
        {
            switch (type)
            {
                case (ushort)Types.MessageType.HEARBEAT:
                    break;
                case (ushort)Types.MessageType.ERROR:
                    break;
                case (ushort)Types.MessageType.LOGIN:
                    Login loginMess = (Login)Message.Deserialize<Login>(messageBytes);
                    UserLogin(loginMess, socketHandle);
                    break;
                case (ushort)Types.MessageType.COMMAND:
                    Command commandMess = (Command)Message.Deserialize<Command>(messageBytes);
                    Command(socketHandle, commandMess);
                    break;
                default:
                    break;
            }
        }

        // Handles login messages, both existing users and new users.
        private static void UserLogin(Login login, IntPtr socketHandle)
        {
            switch(login.LoginType)
            {
                // Existing character login case.
                case Types.LoginType.EXISTING:
                    try
                    {
                        // Queries the database for an entry with matching name and SHA256 hash values.
                        bool exists = false;
                        int queriedSocket = -2;
                        try
                        {
                            queriedSocket = db.Query(
                            Types.Queries.ExistingLoginQuery,
                            login.Name,
                            login.Hash).Rows[0].Field<int>("socket_id");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }

                        // Iterates through the active connections to see if the socket handle associate
                        // with the character attempting to login is a handle on an active connection.
                        foreach (Socket connection in Connections)
                        {
                            if ((int)connection.Handle == queriedSocket)
                            {
                                exists = true;
                            }
                        }
                        
                        // If there isn't an active connection associate with a socket handle, the player
                        // is successfully logged in (the catch block handles username/password authentication
                        // failures.
                        if (!exists)
                        {
                            db.Update(Types.Queries.ExistingLogin, socketHandle.ToString(), login.Name);
                            Ack loginAck = new Ack();
                            loginAck.Type = Types.AckType.LOGIN;
                            byte[] ackBytes = Message.GenerateMessage(loginAck, Types.MessageType.ACK);
                            AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), ackBytes);

                            //Console.WriteLine("Here");
                            Room room = GetRoom(socketHandle);
                            
                            byte[] roomBytes = Message.GenerateMessage(room, Types.MessageType.ROOM);

                            AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), roomBytes);
                        }
                        // Else, there's someone logged in with that character and an error message is sent
                        // (since the user is not signed in, the error comes as a MessageBox, rather than a
                        // message to the game window).
                        else
                        {
                            ServerMessage error = new ServerMessage();
                            error.MessageType = Types.ServerMessageType.POPUP;
                            error.MessageText = "That character is currently in use.";
                            byte[] errorMessageBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                            AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), errorMessageBytes);
                        }
                    }
                    // The login was invalid and the user is notified via a MessageBox.
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        ServerMessage error = new ServerMessage();
                        error.MessageType = Types.ServerMessageType.POPUP;
                        error.MessageText = "Invalid Login";
                        byte[] errorMessageBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                        AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), errorMessageBytes);
                    }
                    break;
                // New character. Checks to see if the requested name already exists in
                // the database.
                case Types.LoginType.NEW:
                    try
                    {
                        DataTable dt = db.Query(Types.Queries.NewLogin, login.Name);
                        dt.Rows[0].Field<string>("id");

                        ServerMessage error = new ServerMessage
                        {
                            MessageType = Types.ServerMessageType.POPUP,
                            MessageText = "A character already exists by that name."
                        };
                    }
                    // The catch block catches a database exception (which, in this case
                    // indicates a good "new character" attempt. The character is created
                    // with a database query, and the user is dropped into the spawn room
                    // (delivered with a Room message).
                    catch
                    {
                        string newCharQuery = String.Format(Types.Queries.CreateCharacter,
                            login.Name, login.Hash, socketHandle);

                        db.Update(String.Format(Types.Queries.CreateCharacter,
                            login.Name, login.Hash, socketHandle));

                        Ack loginAck = new Ack
                        {
                            Type = Types.AckType.LOGIN
                        };

                        byte[] ackBytes = Message.GenerateMessage(loginAck, Types.MessageType.ACK);
                        AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), ackBytes);

                        Room room = GetRoom(socketHandle);
                        byte[] roomBytes = Message.GenerateMessage(room, Types.MessageType.ROOM);

                        AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), roomBytes);
                    }
                    break;
                default:
                    break;
            }
        }
 
        // Handles user generated command messages (movement, information generating,
        // communication, etc.).
        public static void Command(IntPtr socketHandle, Command command)
        {
            switch (command.CommandType)
            {
                // Look command. Returns a Room message.
                case Types.Commands.LOOK:
                    Room room = GetRoom(socketHandle);
                    byte[] roomBytes = Message.GenerateMessage(room, Types.MessageType.ROOM);

                    AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), roomBytes);

                    break;
                // Move command. Moves in the requested direction, if possible. Updates
                // the database with the new player location and sends a new Room message.
                case Types.Commands.MOVE:
                    string name = db.Query(Types.Queries.NameFromSocket, socketHandle.ToString()).Rows[0].Field<string>("character_name");
                    byte[] newRoomBytes;
                    try
                    {
                        string toRoom = db.Query(Types.Queries.RoomInDirection, command.Arguments, name).Rows[0].Field<string>("new_room");
                        db.Update(String.Format(Types.Queries.UpdatePlayerRoom, toRoom, name));
                        Room newRoom = GetRoom(socketHandle);
                        newRoomBytes = Message.GenerateMessage(newRoom, Types.MessageType.ROOM);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        ServerMessage error = new ServerMessage();
                        error.MessageType = Types.ServerMessageType.CONSOLE;
                        error.MessageText = "You cannot move in that direction.";
                        newRoomBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                    }

                    AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), newRoomBytes);
                    break;
                // Who command. Returns a Who message with a list of active player names.
                case Types.Commands.WHO:
                    List<string> activePlayers = new List<string>();

                    foreach (Socket connection in Connections)
                    {
                        if (connection.Connected)
                        {
                            activePlayers.Add(db.Query(Types.Queries.NameFromSocket, connection.Handle).Rows[0].Field<string>("character_name"));
                        }
                    }
                    Who whoMessage = new Who();
                    whoMessage.Players = activePlayers;
                    byte[] whoBytes = Message.GenerateMessage(whoMessage, Types.MessageType.WHO);
                    AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), whoBytes);
                    break;
                // Tell command. Sends a private message to another active player, provided that player
                // has not disabled private messages.
                case Types.Commands.TELL:
                    bool tellSent = false;

                    string tellFromName = db.Query(Types.Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");
                    string tellToName = command.Arguments.Split(' ')[0];
                    try
                    {
                        int toSocket = db.Query(Types.Queries.SocketFromName, tellToName).Rows[0].Field<int>("socket_id");

                        string messageString = command.Arguments.Substring(command.Arguments.IndexOf(" ") + 1);

                        string receiverQuery = "select ignore_tells from players where character_name = \"" + tellToName + "\"";

                        sbyte receiverIgnores = (sbyte)db.Query(receiverQuery).Rows[0].Field<sbyte>("ignore_tells");

                        if (receiverIgnores == 1)
                        {
                            throw new InvalidOperationException(String.Format("{0} is not accepting tells right now.", tellToName));
                        }

                        ServerMessage tell = new ServerMessage
                        {
                            MessageText = tellFromName + " tells you, \"" + messageString + "\""
                        };

                        ServerMessage tellSelf = new ServerMessage
                        {
                            MessageText = "You tell " + tellToName + ", \"" + messageString + "\""
                        };

                        byte[] messageBytes = Message.GenerateMessage(tell, Types.MessageType.SERVERMESSAGE);
                        byte[] selfMessageBytes = Message.GenerateMessage(tellSelf, Types.MessageType.SERVERMESSAGE);

                        foreach (Socket connection in Connections)
                        {
                            Trace.WriteLine(String.Format("Target socket_id: {0}    Connection socket_id: {0}    Connected? {2}", toSocket, connection.Handle, connection.Connected));
                            if (connection.Handle == (IntPtr)toSocket && connection.Connected)
                            {
                                AsynchronousSocketListener.Send(connection, messageBytes);
                                tellSent = true;
                            }
                        }
                        if (tellSent)
                            AsynchronousSocketListener.Send(Connections.Find(
                                socket => socket.Handle == socketHandle), selfMessageBytes);
                    }
                    catch (Exception e)
                    {
                        ServerMessage error = new ServerMessage();
                        error.MessageText = e.Message;
                        error.MessageType = Types.ServerMessageType.CONSOLE;
                        byte[] errorBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                        AsynchronousSocketListener.Send(Connections.Find(socket => socket.Handle == socketHandle), errorBytes);
                    }
                    break;
                // Say command. Sends a message to every active character in a room.
                case Types.Commands.SAY:
                    List<int> roomPlayers = new List<int>();
                    string sayFromName = db.Query(Types.Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");
                    DataTable sayDt = (DataTable)db.Query(Types.Queries.RoomPlayersBySocket, socketHandle.ToString(), socketHandle.ToString());

                    ServerMessage recipientMessage = new ServerMessage()
                    {
                        MessageType = Types.ServerMessageType.CONSOLE,
                        MessageText = String.Format("{0} says, \"{1}\"", sayFromName, command.Arguments)
                    };

                    byte[] receipientMessageBytes = Message.GenerateMessage(recipientMessage, Types.MessageType.SERVERMESSAGE);

                    ServerMessage senderMessage = new ServerMessage
                    {
                        MessageType = Types.ServerMessageType.CONSOLE,
                        MessageText = String.Format("You say, \"{0}\"", command.Arguments)
                    };

                    byte[] senderMessageBytes = Message.GenerateMessage(senderMessage, Types.MessageType.SERVERMESSAGE);

                    foreach (Socket player in Connections)
                    {
                        foreach (DataRow row in sayDt.Rows)
                        {
                            if ((IntPtr)row.Field<int>("socket_id") == player.Handle)
                            {
                                AsynchronousSocketListener.Send(player, receipientMessageBytes);
                            }
                        }
                    }
                    AsynchronousSocketListener.Send(Connections.Find(sender => sender.Handle == socketHandle), senderMessageBytes);
                    break;
                // Shout command. Not yet implemented. Will broadcast a message radially outword. The
                // message will be readable in the next room in all directions, but will not have a 
                // player name associated with it. One room further out, there will be an indication
                // that someone shouted, but the actual message will not be delivered.
                // 
                // Ex. "You hear someone shout from the east."
                case Types.Commands.SHOUT:
                    break;
                // OOC command. Broadcasts a message to every active player who has not chosen
                // to ignore general chat messages.
                case Types.Commands.OOC:
                    string senderName = db.Query(Types.Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");
                    
                    ServerMessage OocRecipientMessage = new ServerMessage()
                    {
                        MessageType = Types.ServerMessageType.CONSOLE,
                        MessageText = String.Format("{0} says, out of character, \"{1}\"", senderName, command.Arguments)
                    };

                    ServerMessage OocSenderMessage = new ServerMessage()
                    {
                        MessageType = Types.ServerMessageType.CONSOLE,
                        MessageText = String.Format("You say, out of character, \"{0}\"", command.Arguments)
                    };

                    byte[] OocRecipientMessageBytes = Message.GenerateMessage(OocRecipientMessage, Types.MessageType.SERVERMESSAGE);
                    byte[] OocSenderMessageBytes = Message.GenerateMessage(OocSenderMessage, Types.MessageType.SERVERMESSAGE);

                    foreach (Socket socket in Connections)
                    {
                        if (socket.Handle != socketHandle && socket.Connected)
                        {
                            AsynchronousSocketListener.Send(socket, OocRecipientMessageBytes);
                        }
                        else if (socket.Handle == socketHandle)
                        {
                            AsynchronousSocketListener.Send(socket, OocSenderMessageBytes);
                        }
                    }

                    break;
                // Ignore command. Allows the user to ignore, or listen to, private messages
                // and OOC chat. (Ex. "ignore tells true" will set the player to ignore private
                // messages.
                case Types.Commands.IGNORE:
                    string ignoreWhat = "";
                    int ignored = 0;
                    try
                    {
                        string[] ignoreArgs = command.Arguments.Split(" ");
                        ignoreWhat = ignoreArgs[0];

                        if (ignoreArgs[0] == "tells") ignoreWhat = "ignore_tells";
                        else if (ignoreArgs[0] == "ooc") ignoreWhat = "ignore_global";

                        if (ignoreArgs[1] == "true") ignored = 1;
                        
                        string ignoreName = db.Query(Types.Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");

                        string ignoreQuery = "update players set {0} = {1} where name = \"{2}\"";

                        db.Update(ignoreQuery, ignoreWhat, ignored, ignoreName);
                    }
                    catch
                    {
                        ServerMessage error = new ServerMessage();
                        error.MessageText = "You can't ignore that.";
                        error.MessageType = Types.ServerMessageType.CONSOLE;

                        byte[] errorBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                        AsynchronousSocketListener.Send(Connections.Find(connection =>
                            connection.Handle == socketHandle), errorBytes);
                    }
                    
                    break;
                // Quit command. When requested by the player, the socket is shutdown, and the Socket
                // object is removed from the list of connections.
                case Types.Commands.QUIT:
                    Socket client = Connections.Find(client => client.Handle == socketHandle);
                    ResetPlayerSocket(socketHandle);
                    client.Shutdown(SocketShutdown.Both);
                    Connections.Remove(client);
                    break;
                default:
                    break;
            }
        }

        // Generates a Room message. Queries the database for room title, description,
        // the exits associated with the room, and a list of players. Will later implement
        // a list of items, mobs, and visible containers.
        public static Room GetRoom(IntPtr socketHandle)
        {
            Room room = new Room();

            string name = db.Query(Types.Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");

            DataTable dt = (DataTable)db.Query(Types.Queries.Room, name);
            if (dt.Rows.Count == 1)
            {
                room.Title = dt.Rows[0].Field<string>("title");
                room.Description = dt.Rows[0].Field<string>("description");
                room.Exits = dt.Rows[0].Field<string>("room_exits");
                room.Players = new List<string>();
                dt = (DataTable)db.Query(Types.Queries.RoomPlayers, name, name);

                foreach (DataRow row in dt.Rows)
                {
                    string nameCheck = row.Field<string>("character_name");
                    
                    DataTable dt2 = (DataTable)db.Query(Types.Queries.SocketFromName, nameCheck);

                    foreach (Socket connection in Connections)
                    {
                        if (connection.Handle == (IntPtr)dt2.Rows[0].Field<int>("socket_id"))
                            if (connection.Connected)
                                room.Players.Add(nameCheck + " is here!");
                    }
                }
            } 
            else
            {
                room.Title = "NULL";
            }
            return room;
        }
    }   
}