using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Shared
{
    /// <summary>
    /// Helper for creating and parsing framed messages sent between client and server.
    /// Uses a 4-byte header (type + length) followed by a JSON-serialized body.
    /// </summary>
    public static class Message
    {
        /// <summary>
        /// Build a framed message consisting of a 4-byte header followed by an optional
        /// JSON body. Header layout: [type(low)][type(high)][length(low)][length(high)].
        /// </summary>
        /// <param name="message">The payload object (will be JSON-serialized) or null for heartbeat.</param>
        /// <param name="messageType">Logical message type enum.</param>
        /// <returns>Byte array ready for network transmission.</returns>
        public static byte[] GenerateMessage(object message, Types.MessageType messageType)
        {
            byte[] headerBytes = new byte[4];
            byte[] bodyBytes;
            Header header = new Header();
            header.Type = (ushort)messageType;

            // If the header type is non-zero then serialize the body and compute length.
            if (header.Type != 0)
            {
                bodyBytes = Serialize(message);
                header.Length = (ushort)(bodyBytes.Length + 4);
            }
            else
            {
                // Type zero used for heartbeat: length is header-only.
                header.Length = 4;
                bodyBytes = new byte[0];
            }

            // Pack header fields into little-endian byte order.
            headerBytes[0] = (byte)(header.Type & 0xFF);
            headerBytes[1] = (byte)((header.Type >> 8) & 0xFF);
            headerBytes[2] = (byte)(header.Length & 0xFF);
            headerBytes[3] = (byte)((header.Length >> 8) & 0xFF);

            // Allocate final message and copy header + body into it.
            byte[] messageBytes = new byte[header.Length];

            Buffer.BlockCopy(headerBytes, 0, messageBytes, 0, 4);
            Buffer.BlockCopy(bodyBytes, 0, messageBytes, 4, bodyBytes.Length);

            return messageBytes;
        }

        /// <summary>
        /// Serialize the provided object to UTF8 JSON bytes.
        /// Returns null if the message is null.
        /// Exceptions are logged and rethrown.
        /// </summary>
        public static byte[] Serialize(object message)
        {
            if (message == null) return null;

            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(message);
            }
            catch (Exception e)
            {
                // Log serialization errors for diagnostics then rethrow.
                Console.WriteLine(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Deserialize the provided UTF8 JSON bytes into the requested type T.
        /// A Utf8JsonReader is constructed over the raw bytes and passed to the
        /// System.Text.Json deserializer.
        /// </summary>
        public static object Deserialize<T>(byte[] data)
        {
            var utf8Reader = new Utf8JsonReader(data);
            object message = JsonSerializer.Deserialize<T>(ref utf8Reader);
            return message;
        }
    }

    /// <summary>
    /// Message header that precedes every framed payload. Also serves as the
    /// heartbeat indicator when Type == 0.
    /// </summary>
    public class Header
    {
        // Logical message type identifier.
        public ushort Type { get; set; }

        // Total message length including the 4-byte header.
        public ushort Length { get; set; }
    }
    
    /// <summary>
    /// Login payload used when a client attempts to authenticate or create a new character.
    /// </summary>
    public class Login
    {
        public Types.LoginType LoginType { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
    }

    /// <summary>
    /// General server-to-client message wrapper for text notifications. The
    /// MessageType field distinguishes console-style messages from popup alerts.
    /// </summary>
    public class ServerMessage
    {
        public Types.ServerMessageType MessageType { get; set; }
        public string MessageText { get; set; }
    }

    /// <summary>
    /// Acknowledgement message used to confirm operations such as a successful login.
    /// </summary>
    public class Ack
    {
        public Types.AckType Type { get; set; }
    }

    /// <summary>
    /// Command message sent from client to server indicating a gameplay action
    /// (look, move, say, tell, etc.) together with any arguments.
    /// </summary>
    public class Command
    {
        public Types.Commands CommandType { get; set; }
        public string Arguments { get; set; }
    }

    /// <summary>
    /// Room description payload sent to clients when they enter or inspect a room.
    /// Contains textual metadata and lists of visible entities.
    /// </summary>
    public class Room
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Exits { get; set; }
        public List<string> Players { get; set; }
        public List<string> Mobs { get; set; }
        public List<string> Items { get; set; }
    }

    /// <summary>
    /// Who message payload containing a list of active player names.
    /// </summary>
    public class Who
    {
        public List<string> Players { get; set; }
    }
}