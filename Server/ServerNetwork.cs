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
    /// <summary>
    /// State object used when reading data from a client socket asynchronously.
    /// Contains a fixed-size buffer, a reference to the client socket and
    /// bookkeeping for a two-stage (header/body) receive workflow.
    /// </summary>
    public class StateObject
    {
        // Size of receive buffer.
        public const int BufferSize = 1024;

        // Receive buffer used for both header and body reads.
        public byte[] buffer = new byte[BufferSize];

        // The client socket associated with this state.
        public Socket workSocket = null;

        // Bytes received so far for the current body read.
        public int totalBytesRead = 0;

        // Header for the current incoming message (type + length).
        public Header header = new Header();
    }

    /// <summary>
    /// Asynchronous TCP socket listener that accepts connections and reads
    /// framed messages using a 4-byte header (type + length) followed by body.
    /// Delegates message handling to ServerCore.
    /// </summary>
    public class AsynchronousSocketListener
    {
        // Signal used to block the accept loop until a connection arrives.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener()
        {
        }

        /// <summary>
        /// Start the TCP listener, bind to the configured endpoint and accept
        /// incoming connections. Each accepted socket begins the header read.
        /// </summary>
        public static void StartListening()
        {
            // Configure local endpoint (IPAddress.Any:11000).
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 11000);

            // Create a TCP/IP listening socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(500);

                // Accept loop: begin an async accept then block until signaled.
                while (true)
                {
                    allDone.Reset();

                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    try
                    {
                        // Wait for a connection to be accepted (allDone set in AcceptCallback).
                        allDone.WaitOne();
                    }
                    catch
                    {
                        // Swallow thread abort/interruption exceptions silently in this example.
                    }
                }
            }
            catch (Exception e)
            {
                // Log binding/listening errors.
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        /// <summary>
        /// Callback invoked when a client connection is accepted. Initializes state
        /// and starts an asynchronous read for the fixed-size header.
        /// </summary>
        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the accept loop to continue.
            allDone.Set();

            // Obtain the listener and accept the incoming connection.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Initialize state for this client and register the connection.
            StateObject state = new StateObject();
            state.workSocket = handler;

            ServerCore.Connections.Add(handler);

            Console.WriteLine(String.Format("Accepted socket connection handle: {0}", handler.Handle));

            // Begin an async receive for the 4-byte header (type + length).
            handler.BeginReceive(state.buffer, 0, 4, 0,
                new AsyncCallback(HeaderCallback), state);
        }

        /// <summary>
        /// Callback for reading the 4-byte header. Once complete this starts the
        /// body receive of the length specified in the header.
        /// </summary>
        public static void HeaderCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead >= 4)
                {
                    // Extract message type and length from the header bytes.
                    byte[] headerBytes = new byte[4];
                    Buffer.BlockCopy(state.buffer, 0, headerBytes, 0, 4);

                    state.header.Type = (ushort)(headerBytes[0] + (headerBytes[1] << 8));
                    state.header.Length = (ushort)(headerBytes[2] + (headerBytes[3] << 8));

                    // Begin receiving the body with the exact length indicated by header.
                    handler.BeginReceive(state.buffer, 0, state.header.Length - 4, 0,
                    new AsyncCallback(BodyCallback), state);
                }
                else
                {
                    // Partial header read — continue reading header bytes.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(HeaderCallback), state);
                }
            }
            catch
            {
                // Ignore individual receive errors in this simple example.
            }
        }

        /// <summary>
        /// Callback for reading the message body. Collects body bytes until the
        /// expected length is reached, then dispatches the message to ServerCore.
        /// </summary>
        public static void BodyCallback(IAsyncResult ar)
        {
            // Retrieve the state and socket for this async operation.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead = 0;

            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch
            {
                // Read error — leave bytesRead at 0 and handle as disconnect below.
            }

            if (bytesRead > 0)
            {
                // Accumulate bytes for the current body read.
                state.totalBytesRead += bytesRead;

                // If we've read the full body, extract the payload and reset state.
                if (state.totalBytesRead == state.header.Length - 4)
                {
                    byte[] messageBytes = new byte[state.totalBytesRead];
                    Buffer.BlockCopy(state.buffer, 0, messageBytes, 0, state.totalBytesRead);

                    // Clear the buffer and reset counters for the next message.
                    Array.Clear(state.buffer, 0, state.buffer.Length);
                    state.totalBytesRead = 0;

                    try
                    {
                        // Begin reading the next header for subsequent messages.
                        handler.BeginReceive(state.buffer, 0, 4, 0,
                        new AsyncCallback(HeaderCallback), state);
                    }
                    catch
                    {
                        // If re-issuing the header read fails, swallow the exception.
                    }

                    // Dispatch the fully-received message to the server core for handling.
                    ServerCore.TakeAction(state.workSocket.Handle, state.header.Type, messageBytes);
                }
                else
                {
                    try
                    {
                        // Not all body bytes received yet — continue receiving into buffer.
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(BodyCallback), state);
                    }
                    catch
                    {
                        // Ignore additional receive errors in this example.
                    }
                }
            }
            else
            {
                // Zero bytes read likely indicates a disconnect — notify ServerCore.
                ServerCore.TakeAction(state.workSocket.Handle, 0, null);
                try
                {
                    // Try to begin reading header again in case socket remains usable.
                    handler.BeginReceive(state.buffer, 0, 4, 0,
                        new AsyncCallback(HeaderCallback), state);
                }
                catch
                {
                    // Swallow exceptions on attempting to rearm the receive.
                }
            }
        }

        /// <summary>
        /// Send data to a connected client using an asynchronous send operation.
        /// If the send fails, the player's socket is reset in the database.
        /// </summary>
        public static void Send(Socket handler, byte[] data)
        {
            try
            {
                // Kick off an asynchronous send and handle completion in SendCallback.
                handler.BeginSend(data, 0, data.Length, 0,
                    new AsyncCallback(SendCallback), handler);
            }
            catch
            {
                // If sending fails (socket closed), clear player's socket state.
                ServerCore.ResetPlayerSocket(handler.Handle);
            }
        }

        /// <summary>
        /// Completion callback for asynchronous sends. Currently only finalizes the
        /// send; any errors are logged to the console.
        /// </summary>
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket and complete the send.
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                // No further action needed here for persistent connections.
            }
            catch (Exception e)
            {
                // Log send errors for diagnostics.
                Console.WriteLine(e.ToString());
            }
        }
    }
}
