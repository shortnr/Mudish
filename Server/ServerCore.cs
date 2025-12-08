using System;
using System.Net.Sockets;
using System.Collections.Generic;
using Shared;
using System.Data;
using System.Threading;

namespace Server
{
    /// <summary>
    /// Core server functionality: connection tracking, message dispatching, and high-level
    /// game command handling. This class coordinates between network listeners and the
    /// database to implement login, movement, chat, and related features.
    /// </summary>
    public class ServerCore
    {
        // A list of socket connections currently active on the server.
        public static List<Socket> Connections = new List<Socket>();
        
        // Database helper instance for executing queries and updates.
        private static Database db = new Database();

        // Tick timer variables and events. The server tick period is 50ms.
        // Used to perform periodic tasks such as sending heartbeats.
        private static AutoResetEvent thresholdEvent = new AutoResetEvent(false);
        private static Timer Ticks = new Timer(TickTimer, null, 50, 50);
        private static int tickCount = 0;

        // Reusable byte array for the heartbeat message. Constructed once to avoid
        // recreating the same bytes repeatedly.
        private static byte[] heartbeatBytes = Message.GenerateMessage(null, Types.MessageType.HEARBEAT);

        /// <summary>
        /// Application entry point for the server process. Connects to the database,
        /// resets stale socket references, starts the network listener and tick timer.
        /// </summary>
        public static int Main(String[] args)
        {
            // Establish database connection and clear old socket references so nobody
            // shows as "already logged in" after a restart.
            db.Connect();
            ResetAllSockets();

            // Start listening for incoming TCP connections.
            AsynchronousSocketListener.StartListening();
            
            // Ensure the tick timer is running.
            Ticks.Change(50, 50);
            
            return 0;
        }

        /// <summary>
        /// Timer callback executed every server tick. Tracks ticks and sends a heartbeat
        /// to all connected clients at a fixed interval (every 5 seconds by default).
        /// </summary>
        private static void TickTimer(object state)
        {
            tickCount++;

            // Send heartbeat every (5 seconds) = 5 * (1000ms / 50ms) = 100 ticks
            if (tickCount % (5 * 20) == 0)
                Connections.ForEach(recipient => AsynchronousSocketListener.Send(recipient, heartbeatBytes));
        }

        /// <summary>
        /// Reset the socket handle for a specific player in the database. Typically
        /// called after a client disconnects to clear their socket_id.
        /// </summary>
        public static void ResetPlayerSocket(IntPtr socketHandle)
        {
            // Look up the player's id by socket handle and set socket_id to -1.
            string id = db.Query("select BIN_TO_UUID(id) as id from players where socket_id = " +
                socketHandle).Rows[0].Field<string>("id");
            
            string query = "update players set socket_id = -1 where id = UUID_TO_BIN(\"" + id + "\")";
            
            db.Update(query);
        }

        /// <summary>
        /// Iterate through all players and set their socket_id to -1. Useful on server
        /// startup to clear stale state left from previous runs.
        /// </summary>
        public static void ResetAllSockets()
        {
            DataTable dt = db.Query("select BIN_TO_UUID(id) as id from players");
            
            // For each player record set the socket_id to -1.
            foreach (DataRow row in dt.Rows)
            {
                string updateQuery = String.Format("update players set socket_id = -1 where id = UUID_TO_BIN(\"{0}\")",
                    row.Field<string>("id"));
                
                db.Update(updateQuery);
            }
        }

        /// <summary>
        /// Entry point for incoming messages from clients. Deserializes messages and
        /// dispatches them to the appropriate handler based on message type.
        /// </summary>
        public static void TakeAction(IntPtr socketHandle, ushort type, byte[] messageBytes)
        {
            switch (type)
            {
                case (ushort)Types.MessageType.HEARBEAT:
                    // Heartbeat received from a client; currently ignored by server.
                    break;
                case (ushort)Types.MessageType.ERROR:
                    // Client reported an error; no server-side handling at this time.
                    break;
                case (ushort)Types.MessageType.LOGIN:
                    // Deserialize login payload and handle login/new-character flow.
                    Login loginMess = (Login)Message.Deserialize<Login>(messageBytes);
                    UserLogin(loginMess, socketHandle);
                    break;
                case (ushort)Types.MessageType.COMMAND:
                    // Deserialize a command message and execute the requested command.
                    Command commandMess = (Command)Message.Deserialize<Command>(messageBytes);
                    Command(socketHandle, commandMess);
                    break;
                default:
                    // Unknown or unhandled message types are ignored.
                    break;
            }
        }

        /// <summary>
        /// Handle login messages for both existing characters and new character creation.
        /// Updates database socket references and sends acknowledgement/room data back to client.
        /// </summary>
        private static void UserLogin(Login login, IntPtr socketHandle)
        {
            switch(login.LoginType)
            {
                case Types.LoginType.EXISTING:
                    try
                    {
                        // Determine whether credentials match an existing player and whether
                        // that player's socket_id corresponds to an active connection.
                        bool exists = false;
                        int queriedSocket = -2;
                        try
                        {
                            queriedSocket = db.Query(
                            Queries.ExistingLoginQuery,
                            login.Name,
                            login.Hash).Rows[0].Field<int>("socket_id");
                        }
                        catch (Exception e)
                        {
                            // Query failure likely means no matching record; log for diagnostics.
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);
                        }

                        // Check active connection list to see if this socket is already connected.
                        foreach (Socket connection in Connections)
                        {
                            if ((int)connection.Handle == queriedSocket)
                            {
                                exists = true;
                            }
                        }
                        
                        if (!exists)
                        {
                            // Persist this connection's socket handle for the player and send
                            // acknowledgement and initial room data.
                            db.Update(Queries.ExistingLogin, socketHandle.ToString(), login.Name);
                            Ack loginAck = new Ack { Type = Types.AckType.LOGIN };
                            byte[] ackBytes = Message.GenerateMessage(loginAck, Types.MessageType.ACK);
                            AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), ackBytes);

                            Room room = GetRoom(socketHandle);
                            byte[] roomBytes = Message.GenerateMessage(room, Types.MessageType.ROOM);
                            AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), roomBytes);
                        }
                        // Someone is already logged in with that character; notify client.
                        else
                        {
                            ServerMessage error = new ServerMessage();
                            error.MessageType = Types.ServerMessageType.POPUP;
                            error.MessageText = "That character is currently in use.";
                            byte[] errorMessageBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                            AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), errorMessageBytes);
                        }
                    }
                    catch (Exception e)
                    {
                        // Authentication failed or other unexpected error; notify client.
                        Console.WriteLine(e.Message);
                        ServerMessage error = new ServerMessage();
                        error.MessageType = Types.ServerMessageType.POPUP;
                        error.MessageText = "Invalid Login";
                        byte[] errorMessageBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                        AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), errorMessageBytes);
                    }
                    break;

                case Types.LoginType.NEW:
                    try
                    {
                        // If a character with that name exists the query will succeed and
                        // we inform the client that the name is taken.
                        DataTable dt = db.Query(Queries.NewLogin, login.Name);
                        dt.Rows[0].Field<string>("id");

                        ServerMessage error = new ServerMessage
                        {
                            MessageType = Types.ServerMessageType.POPUP,
                            MessageText = "A character already exists by that name."
                        };

                        byte[] errorMessageBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                        AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), errorMessageBytes);
                    }
                    catch
                    {
                        // No existing character found: create a new player record, send ack and room.
                        db.Update(String.Format(Queries.CreateCharacter, login.Name, login.Hash, socketHandle));

                        Ack loginAck = new Ack { Type = Types.AckType.LOGIN };
                        byte[] ackBytes = Message.GenerateMessage(loginAck, Types.MessageType.ACK);
                        AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), ackBytes);

                        Room room = GetRoom(socketHandle);
                        byte[] roomBytes = Message.GenerateMessage(room, Types.MessageType.ROOM);
                        AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), roomBytes);
                    }
                    break;

                default:
                    // Unknown login type; ignore.
                    break;
            }
        }
     
        /// <summary>
        /// Process a client-issued command (movement, chat, player actions). This method
        /// translates high-level commands into database updates and outgoing messages.
        /// </summary>
        public static void Command(IntPtr socketHandle, Command command)
        {
            switch (command.CommandType)
            {
                case Types.Commands.LOOK:
                    // Return the current room description to the calling client.
                    Room room = GetRoom(socketHandle);
                    byte[] roomBytes = Message.GenerateMessage(room, Types.MessageType.ROOM);

                    AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), roomBytes);
                    break;

                case Types.Commands.MOVE:
                    // Attempt to resolve the target room and update the player's room in DB.
                    string name = db.Query(Queries.NameFromSocket, socketHandle.ToString()).Rows[0].Field<string>("character_name");
                    byte[] newRoomBytes;
                    try
                    {
                        string toRoom = db.Query(Queries.RoomInDirection, command.Arguments, name).Rows[0].Field<string>("new_room");
                        db.Update(String.Format(Queries.UpdatePlayerRoom, toRoom, name));

                        Room newRoom = GetRoom(socketHandle);
                        newRoomBytes = Message.GenerateMessage(newRoom, Types.MessageType.ROOM);
                    }
                    catch (Exception e)
                    {
                        // Invalid move; notify the player with a console message.
                        Console.WriteLine(e.Message);
                        ServerMessage error = new ServerMessage
                        {
                            MessageType = Types.ServerMessageType.CONSOLE,
                            MessageText = "You cannot move in that direction."
                        };
                        newRoomBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                    }

                    AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), newRoomBytes);
                    break;

                case Types.Commands.WHO:
                    // Build a list of active player names and send it back as a Who message.
                    List<string> activePlayers = new List<string>();

                    foreach (Socket connection in Connections)
                    {
                        if (connection.Connected)
                        {
                            activePlayers.Add(db.Query(Queries.NameFromSocket, connection.Handle).Rows[0].Field<string>("character_name"));
                        }
                    }

                    Who whoMessage = new Who { Players = activePlayers };
                    byte[] whoBytes = Message.GenerateMessage(whoMessage, Types.MessageType.WHO);
                    AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), whoBytes);
                    break;

                case Types.Commands.TELL:
                    // Private message handling: lookup sockets, enforce ignore settings, and send.
                    bool tellSent = false;

                    string tellFromName = db.Query(Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");
                    string tellToName = command.Arguments.Split(' ')[0];
                    try
                    {
                        int toSocket = db.Query(Queries.SocketFromName, tellToName).Rows[0].Field<int>("socket_id");

                        string messageString = command.Arguments.Substring(command.Arguments.IndexOf(" ") + 1);

                        string receiverQuery = "select ignore_tells from players where character_name = \"" + tellToName + "\"";

                        sbyte receiverIgnores = (sbyte)db.Query(receiverQuery).Rows[0].Field<sbyte>("ignore_tells");

                        if (receiverIgnores == 1)
                        {
                            throw new InvalidOperationException(String.Format("{0} is not accepting tells right now.", tellToName));
                        }

                        ServerMessage tell = new ServerMessage { MessageText = tellFromName + " tells you, \"" + messageString + "\"" };
                        ServerMessage tellSelf = new ServerMessage { MessageText = "You tell " + tellToName + ", \"" + messageString + "\"" };

                        byte[] messageBytes = Message.GenerateMessage(tell, Types.MessageType.SERVERMESSAGE);
                        byte[] selfMessageBytes = Message.GenerateMessage(tellSelf, Types.MessageType.SERVERMESSAGE);

                        // Send the message to the recipient if connected.
                        foreach (Socket connection in Connections)
                        {
                            if (connection.Handle == (IntPtr)toSocket && connection.Connected)
                            {
                                AsynchronousSocketListener.Send(connection, messageBytes);
                                tellSent = true;
                            }
                        }

                        // If delivered, notify the sender that the tell was sent.
                        if (tellSent)
                            AsynchronousSocketListener.Send(Connections.Find(socket => socket.Handle == socketHandle), selfMessageBytes);
                    }
                    catch (Exception e)
                    {
                        // Notify the sender about the failure.
                        ServerMessage error = new ServerMessage { MessageText = e.Message, MessageType = Types.ServerMessageType.CONSOLE };
                        byte[] errorBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                        AsynchronousSocketListener.Send(Connections.Find(socket => socket.Handle == socketHandle), errorBytes);
                    }
                    break;

                case Types.Commands.SAY:
                    // Broadcast a chat message to every player in the same room.
                    List<int> roomPlayers = new List<int>();
                    string sayFromName = db.Query(Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");
                    DataTable sayDt = (DataTable)db.Query(Queries.RoomPlayersBySocket, socketHandle.ToString(), socketHandle.ToString());

                    ServerMessage recipientMessage = new ServerMessage
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

                    // Send to each socket that matches the players in the room.
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

                    // Always send the sender their own confirmation message.
                    AsynchronousSocketListener.Send(Connections.Find(sender => sender.Handle == socketHandle), senderMessageBytes);
                    break;

                case Types.Commands.SHOUT:
                    // Not implemented: planned radial broadcast behaviour.
                    break;

                case Types.Commands.OOC:
                    // Out-of-character global broadcast (subject to player ignore settings).
                    string senderName = db.Query(Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");
                    ServerMessage OocRecipientMessage = new ServerMessage
                    {
                        MessageType = Types.ServerMessageType.CONSOLE,
                        MessageText = String.Format("{0} says, out of character, \"{1}\"", senderName, command.Arguments)
                    };

                    ServerMessage OocSenderMessage = new ServerMessage
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

                case Types.Commands.IGNORE:
                    // Toggle ignore settings for tells or global chat for the current player.
                    string ignoreWhat = "";
                    int ignored = 0;
                    try
                    {
                        string[] ignoreArgs = command.Arguments.Split(" ");
                        ignoreWhat = ignoreArgs[0];

                        if (ignoreArgs[0] == "tells") ignoreWhat = "ignore_tells";
                        else if (ignoreArgs[0] == "ooc") ignoreWhat = "ignore_global";

                        if (ignoreArgs[1] == "true") ignored = 1;
                        
                        string ignoreName = db.Query(Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");

                        string ignoreQuery = "update players set {0} = {1} where character_name = \"{2}\"";

                        db.Update(ignoreQuery, ignoreWhat, ignored, ignoreName);

                        // Prepare a feedback message for the player about the new state.
                        string text = (ignored == 1) ? "You are now ignoring {0}." : "You are now listening to {0}.";
                        ServerMessage successMessage = new ServerMessage
                        {
                            MessageType = Types.ServerMessageType.CONSOLE,
                            MessageText = String.Format(text, ignoreArgs[0])
                        };

                        SendMessage(socketHandle, successMessage, Types.MessageType.SERVERMESSAGE);
                    }
                    catch
                    {
                        // Invalid ignore command; inform the player.
                        ServerMessage error = new ServerMessage { MessageText = "You can't ignore that.", MessageType = Types.ServerMessageType.CONSOLE };
                        byte[] errorBytes = Message.GenerateMessage(error, Types.MessageType.SERVERMESSAGE);
                        AsynchronousSocketListener.Send(Connections.Find(connection => connection.Handle == socketHandle), errorBytes);
                    }
                    
                    break;

                case Types.Commands.QUIT:
                    // Cleanly disconnect a client and clear their database socket reference.
                    Socket client = Connections.Find(client => client.Handle == socketHandle);
                    ResetPlayerSocket(socketHandle);
                    client.Shutdown(SocketShutdown.Both);
                    Connections.Remove(client);
                    break;

                default:
                    // Unhandled command types are ignored.
                    break;
            }
        }

        /// <summary>
        /// Construct a Room message for the player associated with the given socket handle.
        /// Queries the database for room meta information and compiles a list of visible players.
        /// </summary>
        public static Room GetRoom(IntPtr socketHandle)
        {
            Room room = new Room();

            // Resolve the player's character name from the socket handle first.
            string name = db.Query(Queries.NameFromSocket, socketHandle).Rows[0].Field<string>("character_name");

            // Query the stored procedure that returns room details for this character.
            DataTable dt = (DataTable)db.Query(Queries.Room, name);
            if (dt.Rows.Count == 1)
            {
                // Populate room metadata.
                room.Title = dt.Rows[0].Field<string>("title");
                room.Description = dt.Rows[0].Field<string>("description");
                //room.Exits = dt.Rows[0].Field<string>("room_exits");
                room.Players = new List<string>();

                // Get a list of other players in the room and map them to active sockets.
                dt = (DataTable)db.Query(Queries.RoomPlayers, name, name);

                foreach (DataRow row in dt.Rows)
                {
                    string nameCheck = row.Field<string>("character_name");
                    DataTable dt2 = (DataTable)db.Query(Queries.SocketFromName, nameCheck);

                    // Only include players whose socket is present and connected.
                    foreach (Socket connection in Connections)
                    {
                        if (connection.Handle == (IntPtr)dt2.Rows[0].Field<int>("socket_id") && connection.Connected)
                            room.Players.Add(nameCheck + " is here!");
                    }
                }
            }
            else
            {
                // Fallback if room lookup fails.
                room.Title = "NULL";
            }
            return room;
        }

        /// <summary>
        /// Helper to generate and send a message object to a single client identified by
        /// their socket handle.
        /// </summary>
        private static void SendMessage(IntPtr socketHandle, object message, Types.MessageType type)
        {
            byte[] messageBytes = Message.GenerateMessage(message, type);
            AsynchronousSocketListener.Send(Connections.Find(client => client.Handle == socketHandle), messageBytes);
        }
    }
}