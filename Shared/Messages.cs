using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Shared
{
    public static class Message
    {
        // The entrypoint for sending messages. This does it all: header generation,
        // serialization, block copying of bytes into one message.
        public static byte[] GenerateMessage(object message, Types.MessageType messageType)
        {
            byte[] headerBytes = new byte[4];
            byte[] bodyBytes;
            Header header = new Header();
            header.Type = (ushort)messageType;

            if (header.Type != 0)
            {
                bodyBytes = Serialize(message);
                header.Length = (ushort)(bodyBytes.Length + 4);
            }
            else
            {
                header.Length = 4;
                bodyBytes = new byte[0];
            }

            headerBytes[0] = (byte)(header.Type % 256);
            headerBytes[1] = (byte)((header.Type >> 8) % 256);
            headerBytes[2] = (byte)(header.Length % 256);
            headerBytes[3] = (byte)((header.Length >> 8) % 256);

            byte[] messageBytes = new byte[header.Length];

            Buffer.BlockCopy(headerBytes, 0, messageBytes, 0, 4);
            Buffer.BlockCopy(bodyBytes, 0, messageBytes, 4, bodyBytes.Length);

            return messageBytes;
        }

        // Serializer. Called by GenerateMessage.
        public static byte[] Serialize(object message)
        {
            if (message == null) return null;

            try
            {
                return JsonSerializer.SerializeToUtf8Bytes(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        // Deserializer
        public static object Deserialize<T>(byte[] data)
        {
            var utf8Reader = new Utf8JsonReader(data);
            object message = JsonSerializer.Deserialize<T>(ref utf8Reader);
            return message;
        }
    }

    // Header class. Also acts as the Heartbeat message.
    public class Header
    {
        public ushort Type { get; set; }
        public ushort Length { get; set; }
    }
    
    // Login message class.
    public class Login
    {
        public Types.LoginType LoginType { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
    }

    // ServerMessage message class.
    public class ServerMessage
    {
        public Types.ServerMessageType MessageType { get; set; }
        public string MessageText { get; set; }
    }

    // Ack messgage class
    public class Ack
    {
        public Types.AckType Type { get; set; }
    }

    // Command message class
    public class Command
    {
        public Types.Commands CommandType { get; set; }
        public string Arguments { get; set; }
    }

    // Room message class
    public class Room
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Exits { get; set; }
        public List<string> Players { get; set; }
        public List<string> Mobs { get; set; }
        public List<string> Items { get; set; }
    }

    public class NewRoom
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Direction { get; set; }
        public bool Hidden { get; set; } = false;
    }

    // Who message class
    public class Who
    {
        public List<string> Players { get; set; }
    }
}