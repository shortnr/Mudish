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
    public class ClientCore
    {
        static Thread socketThread;
        private static Socket server;
        //static Types.ClientState State = Types.ClientState.DISCONNECTED;
        private static byte[] heartbeatBytes = Message.GenerateMessage(null, Types.MessageType.HEARBEAT);

        public static void Game()
        {
            /*while (State != Types.ClientState.LOGOUT)
            {

            }*/
        }

        public static void NewAsyncClient(int port, string ip)
        {
            socketThread = new Thread(() => AsynchronousClient.StartClient(port, ip));
        }

        public static void StartSocket()
        {
            socketThread.Start();
        }

        public static void SetServer(Socket serverSocket)
        {
            server = serverSocket;
        }

        public static Socket GetServer()
        {
            return server;
        }

        public static void ExistingCharacter(string name, string password)
        {
            string encryptedString = EncryptPassword(password);

            Login login = new Login
            {
                LoginType = Types.LoginType.EXISTING,
                Hash = encryptedString,
                Name = name
            };

            byte[] loginBytes = Message.GenerateMessage(login, Types.MessageType.LOGIN);
            AsynchronousClient.Send(server, loginBytes);
        }

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

        private static string EncryptPassword(string password)
        {
            SHA256 encryptor = SHA256.Create();
            byte[] encryptedPassword = encryptor.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < encryptedPassword.Length; i++)
                sb.Append(encryptedPassword[i].ToString("x2"));
            
            return sb.ToString();
        }

        public static void IssueCommand(string commandText)
        {
            string[] splitText = commandText.Split(' ');
            Command command = new Command();
            bool valid = false;
            switch(splitText[0])
            {
                case "west":
                case "east":
                case "south":
                case "north":
                    command.CommandType = Types.Commands.MOVE;
                    command.Arguments = splitText[0];
                    valid = true;
                    break;
                case "look":
                    command.CommandType = Types.Commands.LOOK;
                    valid = true;
                    break;
                case "who":
                    command.CommandType = Types.Commands.WHO;
                    valid = true;
                    break;
                case "tell":
                    try
                    {
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
                case "say":
                    try
                    {
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
                case "shout":
                    try
                    {
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
                case "ooc":
                    try
                    {
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
                case "quit":
                    Command quit = new Command();
                    quit.CommandType = Types.Commands.QUIT;
                    byte[] quitBytes = Message.GenerateMessage(quit, Types.MessageType.COMMAND);
                    AsynchronousClient.Send(server, quitBytes);
                    server.Shutdown(SocketShutdown.Both);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).Close();
                    });
                    Environment.Exit(0);
                    break;
                default:
                    AddToTextBlock("Unknown command: \"" + commandText + "\".\n");
                    break;
            }
            if (valid)
            {
                byte[] messageBytes = Message.GenerateMessage(command, Types.MessageType.COMMAND);
                AsynchronousClient.Send(server, messageBytes);
            }
        }

        public static void TakeAction(ushort type, byte[] messageBytes)
        {
            switch (type)
            {
                case (ushort)Types.MessageType.HEARBEAT:
                    AsynchronousClient.Send(server, heartbeatBytes);
                    break;
                case (ushort)Types.MessageType.ACK:
                    Ack ack = (Ack)Message.Deserialize<Ack>(messageBytes);
                    switch(ack.Type)
                    {
                        case Types.AckType.LOGIN:
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ((MainWindow)Application.Current.MainWindow).Activate();
                                ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
                                ((MainWindow)Application.Current.MainWindow).Input.Focus();
                            });
                            try
                            {
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
                case (ushort)Types.MessageType.SERVERMESSAGE:
                    ServerMessage serverMessage = (ServerMessage)Message.Deserialize<ServerMessage>(messageBytes);
                    PrintServerMessage(serverMessage);
                    break;
                case (ushort)Types.MessageType.ROOM:
                    Room roomMess = (Room)Message.Deserialize<Room>(messageBytes);
                    PrintRoom(roomMess);
                    break;
                case (ushort)Types.MessageType.WHO:
                    Who whoMess = (Who)Message.Deserialize<Who>(messageBytes);
                    PrintWho(whoMess);
                    break;
                case (ushort)Types.MessageType.SCORE:
                    break;
                default:
                    break;
            }
        }

        public static void PrintRoom(Room room)
        {
            List<string> roomText = new List<string>();
            roomText.Add(room.Title);
            roomText.AddRange(Wrap(room.Description, 60));
            roomText.Add(room.Exits);
            foreach (string name in room.Players) roomText.Add(name);
            roomText.Add("");
            foreach (string line in roomText) AddToTextBlock(line);
        }

        public static void PrintWho(Who whoMessage)
        {
            AddToTextBlock("Players currently logged in:");
            foreach (string player in whoMessage.Players)
            {
                AddToTextBlock("   " + player);
            }
            AddToTextBlock("");
        }
        
        public static void PrintServerMessage(ServerMessage message)
        {
            if (message.MessageType == Types.ServerMessageType.CONSOLE)
            {
                List<string> text = Wrap(message.MessageText, 60);
                foreach (string line in text) AddToTextBlock(line);
                AddToTextBlock("");
            }
            else
                MessageBox.Show(message.MessageText);
        }

        public static List<string> Wrap(string text, int width)
        {
            List<string> returnText = new List<string>();

            while (text.Length > width)
            {
                string substr = text.Substring(0, width);
                int lastSpace = substr.LastIndexOf(" ");
                returnText.Add(text.Substring(0, lastSpace + 1));
                text = text.Substring(lastSpace + 1);
            }
            returnText.Add(text);

            return returnText;
        }

        private static void AddToTextBlock(string newText)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)Application.Current.MainWindow).AppendTextBlock(newText);
            });
        }
    }
}
