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
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Client
{
    // State object for receiving data from remote device.  
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 4096;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
        public Header header = new Header();
        public bool lengthKnown = false;
        public int transmissionLength = -1;
        public MemoryStream stream = new MemoryStream();
        public int totalBytesRead = 0;
    }

    public class AsynchronousClient
    {
        // The port number for the remote device.  
        //private const int port = 11000;

        //DelegateSend del = new DelegateSend(this.Send);

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;

        public static void StartClient(int port, string ip)
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // The name of the
                // remote device is "host.contoso.com".  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(ip);
                Trace.WriteLine(ipHostInfo.HostName);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                Trace.WriteLine(ipAddress.ToString());
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Receive the response from the remote device.  
                Receive(client);
                try
                {
                    receiveDone.WaitOne();
                }
                catch
                {

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                // Signal that the connection has been made.  
                connectDone.Set();

                ClientCore.SetServer(client); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, 4, 0,
                    new AsyncCallback(HeaderCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void HeaderCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = 0;

            try
            {
                 bytesRead = handler.EndReceive(ar);
            }
            catch (SocketException) {}
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

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
                try
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(HeaderCallback), state);
                }
                catch
                {
                    
                }
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
               /* if (!state.lengthKnown && bytesRead == 4)
                {
                    byte[] lengthByte = new byte[4];
                    Buffer.BlockCopy(state.buffer, 0, lengthByte, 0, 4);
                    state.transmissionLength = BitConverter.ToInt32(lengthByte, 0);
                }*/
                // There  might be more data, so store the data received so far.  
                state.stream.Write(state.buffer, 0, bytesRead);
                state.totalBytesRead += bytesRead;
                // Check for end-of-file tag. If it is not there, read
                // more data.  

                if (state.totalBytesRead == state.header.Length - 4)
                {
                    byte[] messageBytes = new byte[state.totalBytesRead];
                    Buffer.BlockCopy(state.buffer, 0, messageBytes, 0, state.totalBytesRead);

                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    state.transmissionLength = -1;
                    state.totalBytesRead = 0;

                    handler.BeginReceive(state.buffer, 0, 4, 0,
                        new AsyncCallback(HeaderCallback), state);

                    ClientCore.TakeAction(state.header.Type, messageBytes);
                }
                else
                {
                    // Not all data received. Get more.  
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(BodyCallback), state);
                }
            }
            else
            {
                ClientCore.TakeAction(0, null);

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

        public static void Send(Socket client, byte[] data)
        {
            try
            {
                // Begin sending the data to the remote device.  
                client.BeginSend(data, 0, data.Length, 0,
                    new AsyncCallback(SendCallback), client);
            }
            catch
            {

            }
        }

        //public delegate void DelegateSend(byte[] data);

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                
                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}