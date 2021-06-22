// Much of this class file was adapted from the Asynchronous Client-Server socket example on Microsoft's
// website. Some of the "chaff" has been removed, and I've split the standard example callback into a 
// header and body callback. This is much more simple, as the header is a fixed size, and tells the body
// callback exactly how many bytes to read. The website I referenced for this is:
//
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-server-socket-example

using Shared;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Client socket.
        public Socket workSocket = null;

        public int totalBytesRead = 0;

        public Header header = new Header();
    }

    public class AsynchronousSocketListener
    {
        // Thread signal.  
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener()
        {
        }

        public static void StartListening()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            // running the listener is "host.contoso.com".  
            IPHostEntry ipHostInfo = Dns.GetHostEntry("10.0.0.71");
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            //IPAddress newIp = new IPAddress(;
            Console.WriteLine(ipAddress.ToString());
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(500);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    try
                    {
                        allDone.WaitOne();
                    }
                    catch
                    {

                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = handler;

            ServerCore.Connections.Add(handler);

            Console.WriteLine(String.Format("Accepted socket connection handle: {0}", handler.Handle));

            handler.BeginReceive(state.buffer, 0, 4, 0,
                new AsyncCallback(HeaderCallback), state);
        }

        public static void HeaderCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead >= 4)
                {
                    byte[] headerBytes = new byte[4];
                    Buffer.BlockCopy(state.buffer, 0, headerBytes, 0, 4);

                    state.header.Type = (ushort)(headerBytes[0] + (headerBytes[1] << 8));
                    state.header.Length = (ushort)(headerBytes[2] + (headerBytes[3] << 8));

                    handler.BeginReceive(state.buffer, 0, state.header.Length - 4, 0,
                    new AsyncCallback(BodyCallback), state);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(HeaderCallback), state);
                }
            }
            catch
            {

            }
        }

        public static void BodyCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = 0;
            // Read data from the client socket.
            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch
            {

            }

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.  
                state.totalBytesRead += bytesRead;
                // Check for end-of-file tag. If it is not there, read
                // more data.  

                if (state.totalBytesRead == state.header.Length - 4)
                {
                    byte[] messageBytes = new byte[state.totalBytesRead];
                    Buffer.BlockCopy(state.buffer, 0, messageBytes, 0, state.totalBytesRead);

                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    state.totalBytesRead = 0;
                    
                    try
                    {
                        handler.BeginReceive(state.buffer, 0, 4, 0,
                        new AsyncCallback(HeaderCallback), state);
                    }
                    catch
                    {

                    }

                    ServerCore.TakeAction(state.workSocket.Handle, state.header.Type, messageBytes);
                }
                else
                {
                    try
                    {
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(BodyCallback), state);
                    }
                    catch
                    {

                    }
                }
            }
            else
            {
                ServerCore.TakeAction(state.workSocket.Handle, 0, null);
                try
                {
                    handler.BeginReceive(state.buffer, 0, 4, 0,
                        new AsyncCallback(HeaderCallback), state);
                }
                catch
                {

                }
            }
        }

        public static void Send(Socket handler, byte[] data)
        {
            try
            {
                // Begin sending the data to the remote device.  
                handler.BeginSend(data, 0, data.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            catch
            {
                ServerCore.ResetPlayerSocket(handler.Handle);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
