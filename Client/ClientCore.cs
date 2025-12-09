using System;
using System.Collections.Generic;
using System.Text;
using Shared;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    /// <summary>
    /// Core client logic.
    /// Manages the socket thread and server socket, constructs and sends protocol messages (login, command, heartbeat),
    /// hashes passwords using SHA256, parses incoming protocol messages and routes them to UI helpers,
    /// and marshals all UI interactions to the WPF UI thread via Application.Current.Dispatcher.
    /// </summary>
    /// <remarks>
    /// - Uses AsynchronousClient for socket I/O and Message helpers from the Shared assembly.
    /// - Maintains a static server Socket and a socket thread; external callers should coordinate access when needed.
    /// - Responds to HEARBEAT messages by sending a heartbeat reply.
    /// - ACK messages (such as LOGIN) cause UI windows to be enabled/closed on the UI thread.
    /// </remarks>
    public class ClientCore
    {
        // Socket thread for asynchronous client operations
        static Thread socketThread;
        // Server socket for communication
        private static Socket server;
        // Pre-generated heartbeat message bytes
        private static byte[] heartbeatBytes = Message.GenerateMessage(null, Types.MessageType.HEARBEAT);

        // Creates a new asynchronous client socket thread
        public static void NewAsyncClient(int port, string ip)
        {
            // Initialize socket thread
            socketThread = new Thread(() => AsynchronousClient.StartClient(port, ip));
        }

        // Starts the socket thread
        public static void StartSocket()
        {
            socketThread.Start();
        }

        // Sets the server socket
        public static void SetServer(Socket serverSocket)
        {
            server = serverSocket;
        }

        // Gets the server socket
        public static Socket GetServer()
        {
            return server;
        }

        // Sends login message for existing character
        public static void ExistingCharacter(string name, string password)
        {
            // Encrypt password
            string encryptedString = EncryptPassword(password);

            // Create login message
            Login login = new Login
            {
                LoginType = Types.LoginType.EXISTING,
                Hash = encryptedString,
                Name = name
            };

            // Serialize and send login message
            byte[] loginBytes = Message.GenerateMessage(login, Types.MessageType.LOGIN);
            AsynchronousClient.Send(server, loginBytes);
        }

        // Sends login message for new character
        public static void NewCharacter(string name, string password1, string password2)
        {
            string encryptedString = EncryptPassword(password1);

            Login login = new Login
            {
                LoginType = Types.LoginType.NEW,
                Name = name,
                Hash = encryptedString
            };

            byte[] loginBytes = Message.GenerateMessage(login, Types.MessageType.LOGIN);
            AsynchronousClient.Send(server, loginBytes);
        }

        // Encrypts password using SHA256
        private static string EncryptPassword(string password)
        {
            SHA256 encryptor = SHA256.Create();
            byte[] encryptedPassword = encryptor.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < encryptedPassword.Length; i++)
                sb.Append(encryptedPassword[i].ToString("x2"));

            return sb.ToString();
        }

        // Issues a command to the server based on user input
        public static void IssueCommand(string commandText)
        {
            // Split command text into parts
            string[] splitText = commandText.Split(' ');
            Command command = new Command();

            // Flag to track if command is valid
            bool valid = false;

            switch (splitText[0])
            {
                // Movement commands
                case "west":
                case "east":
                case "south":
                case "north":
                case "up":
                case "down":
                    command.CommandType = Types.Commands.MOVE;
                    command.Arguments = splitText[0];
                    valid = true;
                    break;
                // Look command
                case "look":
                    command.CommandType = Types.Commands.LOOK;
                    valid = true;
                    break;
                // Who command
                case "who":
                    command.CommandType = Types.Commands.WHO;
                    valid = true;
                    break;
                // * Communication commands *
                // Tell command (private message)
                case "tell":
                    try
                    {
                        // Ensure there are enough arguments
                        if (commandText.Split(' ').Length > 2)
                        {
                            command.CommandType = Types.Commands.TELL;
                            command.Arguments = commandText.Substring(5);
                            valid = true;
                        }
                        else AddToTextBlock("Tell who, what?\n");
                    }
                    catch
                    {
                        AddToTextBlock("Tell who, what?\n");
                    }
                    break;
                // Say command (within room)
                case "say":
                    try
                    {
                        // Ensure there are enough arguments
                        if (commandText.Split(' ').Length > 1)
                        {
                            command.CommandType = Types.Commands.SAY;
                            command.Arguments = commandText.Substring(4);
                            valid = true;
                        }
                        else AddToTextBlock("Say what?\n");
                    }
                    catch
                    {
                        AddToTextBlock("Say what?\n");
                    }
                    break;
                // Shout command (within area, x # of rooms, tbd)
                case "shout":
                    try
                    {
                        // Ensure there are enough arguments
                        if (commandText.Split(' ').Length > 1)
                        {
                            command.CommandType = Types.Commands.SHOUT;
                            command.Arguments = commandText.Substring(6);
                            valid = true;
                        }
                        else AddToTextBlock("Why are you shouting?!\n");
                    }
                    catch
                    {
                        AddToTextBlock("Why are you shouting?!\n");
                    }
                    break;
                // OOC command (out-of-character)
                case "ooc":
                    try
                    {
                        // Ensure there are enough arguments
                        if (commandText.Split(' ').Length > 1)
                        {
                            command.CommandType = Types.Commands.OOC;
                            command.Arguments = commandText.Substring(4);
                            valid = true;
                        }
                        else
                        {
                            AddToTextBlock("You can't say NOTHING..\n");
                        }
                    }
                    catch
                    {
                        AddToTextBlock("You can't say NOTHING..\n");
                    }
                    break;
                // Ignore command (other player)
                case "ignore":
                    try
                    {
                        command.CommandType = Types.Commands.IGNORE;
                        command.Arguments = commandText.Substring(7);
                        valid = true;
                    }
                    catch
                    {

                    }
                    break;
                // Quit command
                case "quit":
                    // Send quit command and close application
                    Command quit = new Command();
                    quit.CommandType = Types.Commands.QUIT;
                    byte[] quitBytes = Message.GenerateMessage(quit, Types.MessageType.COMMAND);
                    AsynchronousClient.Send(server, quitBytes);
                    // Close the server socket
                    server.Shutdown(SocketShutdown.Both);
                    // Close the main window on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).Close();
                    });
                    // Exit the application
                    Environment.Exit(0);
                    break;
                // Unknown command
                default:
                    AddToTextBlock("Unknown command: \"" + commandText + "\".\n");
                    break;
            }
            // If command is valid, send it to the server
            if (valid)
            {
                byte[] messageBytes = Message.GenerateMessage(command, Types.MessageType.COMMAND);
                AsynchronousClient.Send(server, messageBytes);
            }
        }

        // Routes incoming messages based on type
        public static void TakeAction(ushort type, byte[] messageBytes)
        {
            switch (type)
            {
                // Heartbeat message
                case (ushort)Types.MessageType.HEARBEAT:
                    AsynchronousClient.Send(server, heartbeatBytes);
                    break;
                // Acknowledgment message
                case (ushort)Types.MessageType.ACK:
                    Ack ack = (Ack)Message.Deserialize<Ack>(messageBytes);
                    switch (ack.Type)
                    {
                        // Login acknowledgment
                        case Types.AckType.LOGIN:
                            // Enable main window and focus input on UI thread
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ((MainWindow)Application.Current.MainWindow).Activate();
                                ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
                                ((MainWindow)Application.Current.MainWindow).Input.Focus();
                            });
                            try
                            {
                                // Close character selection/creation windows on UI thread
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    foreach (Window window in Application.Current.Windows)
                                    {
                                        if (window.GetType().Name == typeof(Views.ExistingCharacterWindow).Name ||
                                            window.GetType().Name == typeof(Views.NewCharacterWindow).Name)
                                            window.Close();
                                    }
                                });
                            }
                            catch
                            {
                                // In case of error, at least try to close the new character window
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    foreach (Window window in Application.Current.Windows)
                                    {
                                        if (window.GetType().Name == typeof(Views.NewCharacterWindow).Name)
                                        {
                                            window.Close();
                                        }
                                    }
                                });
                            }

                            break;
                        default:
                            break;
                    }
                    break;
                // Server message
                case (ushort)Types.MessageType.SERVERMESSAGE:
                    ServerMessage serverMessage = (ServerMessage)Message.Deserialize<ServerMessage>(messageBytes);
                    PrintServerMessage(serverMessage);
                    break;
                // Room information message
                case (ushort)Types.MessageType.ROOM:
                    Room roomMess = (Room)Message.Deserialize<Room>(messageBytes);
                    PrintRoom(roomMess);
                    break;
                // Who message
                case (ushort)Types.MessageType.WHO:
                    Who whoMess = (Who)Message.Deserialize<Who>(messageBytes);
                    PrintWho(whoMess);
                    break;
                // Score message (not yet implemented)
                case (ushort)Types.MessageType.SCORE:
                    break;
                default:
                    break;
            }
        }

        // Prints room information to the UI
        public static void PrintRoom(Room room)
        {
            // Build room text
            List<string> roomText = new List<string>();
            roomText.Add(room.Title);
            roomText.AddRange(Wrap(room.Description, 60));
            roomText.Add(room.Exits);
            // List players in room
            foreach (string name in room.Players) roomText.Add(name);
            roomText.Add("");
            // Print room text to UI
            foreach (string line in roomText) AddToTextBlock(line);
        }

        // Prints "who" message to the UI
        public static void PrintWho(Who whoMessage)
        {
            // Build and print who text
            AddToTextBlock("Players currently logged in:");
            foreach (string player in whoMessage.Players)
            {
                AddToTextBlock("   " + player);
            }
            AddToTextBlock("");
        }

        // Prints server message to the UI or message box
        public static void PrintServerMessage(ServerMessage message)
        {
            // If console message, print to text block
            if (message.MessageType == Types.ServerMessageType.CONSOLE)
            {
                List<string> text = Wrap(message.MessageText, 60);
                foreach (string line in text) AddToTextBlock(line);
                AddToTextBlock("");
            }
            // Else, show message box
            else
                MessageBox.Show(message.MessageText);
        }

        // Wraps text to specified width
        public static List<string> Wrap(string text, int width)
        {
            // List to hold wrapped text lines
            List<string> returnText = new List<string>();

            // Wrap text while it exceeds width
            while (text.Length > width)
            {
                // Get substring and find last space
                string substr = text.Substring(0, width);
                int lastSpace = substr.LastIndexOf(" ");

                // Add line to return text and update remaining text
                returnText.Add(text.Substring(0, lastSpace + 1));
                text = text.Substring(lastSpace + 1);
            }
            // Add remaining text
            returnText.Add(text);

            return returnText;
        }

        // Appends text to the UI text block on the UI thread
        private static void AddToTextBlock(string newText)
        {
            // Marshal to UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Append text to main window text block
                ((MainWindow)Application.Current.MainWindow).AppendTextBlock(newText);
            });
        }
    }
}
