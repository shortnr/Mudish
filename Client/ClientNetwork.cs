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
    /// <summary>
    /// Holds per-connection state for asynchronous socket operations.
    /// Maintains the working socket, receive buffer, an expandable string accumulator,
    /// and metadata required to assemble complete protocol messages.
    /// </summary>
    public class StateObject
    {
        // Client socket
        public Socket workSocket = null;
        // Size of receive buffer
        public const int BufferSize = 4096;
        // Receive buffer
        public byte[] buffer = new byte[BufferSize];
        // Received data string
        public StringBuilder sb = new StringBuilder();
        // Header for current message
        public Header header = new Header();
        // Body length tracking
        public bool lengthKnown = false;
        // Total expected length of current transmission
        public int transmissionLength = -1;
        // Memory stream to accumulate message body
        public MemoryStream stream = new MemoryStream();
        // Total bytes read for current message body
        public int totalBytesRead = 0;
    }

    public class AsynchronousClient
    {
        // ManualResetEvent instances signal completion
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // Response string
        private static String response = String.Empty;

        // Starts the client connection to the server
        public static void StartClient(int port, string ip)
        {
            try
            {
                // Establish the remote endpoint for the socket
                IPHostEntry ipHostInfo = Dns.GetHostEntry(ip);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Receive the response from the remote device
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

        // Callback for connection completion; signals when connection is made
        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection
                client.EndConnect(ar);

                // Signal that the connection has been made
                connectDone.Set();

                // Set the client socket in ClientCore
                ClientCore.SetServer(client); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Begins receiving data from the server
        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device; first the header
                client.BeginReceive(state.buffer, 0, 4, 0,
                    new AsyncCallback(HeaderCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        // Callback for header reception
        private static void HeaderCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = 0;

            try
            {
                // Read data from the client socket
                bytesRead = handler.EndReceive(ar);
            }
            catch (SocketException) {}
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // If we received 4 bytes, we have the full header
            if (bytesRead >= 4)
            {
                // Extract header information
                byte[] headerBytes = new byte[4];
                Buffer.BlockCopy(state.buffer, 0, headerBytes, 0, 4);

                // Parse header
                state.header.Type = (ushort)(headerBytes[0] + (headerBytes[1] << 8));
                state.header.Length = (ushort)(headerBytes[2] + (headerBytes[3] << 8));

                // Begin receiving the body based on the length specified in the header
                handler.BeginReceive(state.buffer, 0, state.header.Length - 4, 0,
                    new AsyncCallback(BodyCallback), state);
            }
            else
            {
                try
                {
                    // Not all data received. Get more
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(HeaderCallback), state);
                }
                catch
                {
                    
                }
            }
        }

        // Callback for body reception
        public static void BodyCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = 0;

            try
            {
                // Read data from the client socket
                bytesRead = handler.EndReceive(ar);
            }
            catch
            {

            }

            // If we received data, process it
            if (bytesRead > 0)
            {
                // Write the received data to the memory stream and update total bytes read
                state.stream.Write(state.buffer, 0, bytesRead);
                state.totalBytesRead += bytesRead;

                // Check if we have received the entire message body
                if (state.totalBytesRead == state.header.Length - 4)
                {
                    // Full message received; process it
                    byte[] messageBytes = new byte[state.totalBytesRead];
                    Buffer.BlockCopy(state.buffer, 0, messageBytes, 0, state.totalBytesRead);

                    // Reset state for next message
                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    state.transmissionLength = -1;
                    state.totalBytesRead = 0;

                    // Begin receiving the next message header
                    handler.BeginReceive(state.buffer, 0, 4, 0,
                        new AsyncCallback(HeaderCallback), state);

                    // Take action based on the received message
                    ClientCore.TakeAction(state.header.Type, messageBytes);
                }
                else
                {
                    // Not all data received. Get more
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(BodyCallback), state);
                }
            }
            else
            {
                // No data received; possibly a disconnection. Reset and wait for new messages
                ClientCore.TakeAction(0, null);

                try
                {
                    // Begin receiving the next message header
                    handler.BeginReceive(state.buffer, 0, 4, 0,
                        new AsyncCallback(HeaderCallback), state);
                }
                catch
                {

                }
            }
        }

        // Sends data to the server
        public static void Send(Socket client, byte[] data)
        {
            try
            {
                // Begin sending the data to the remote device
                client.BeginSend(data, 0, data.Length, 0,
                    new AsyncCallback(SendCallback), client);
            }
            catch
            {

            }
        }

        // Callback for send completion
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device
                int bytesSent = client.EndSend(ar);
                
                // Signal that all bytes have been sent
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}