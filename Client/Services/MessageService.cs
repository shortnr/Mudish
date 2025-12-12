using Client.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Services
{
    /// <summary>
    /// Provides messaging services to the client application. Used to handle incoming
    /// messages from the ClientCore directed at the user interface.
    /// </summary>
    public class MessageService : IMessageService
    {
        // Message received event
        public event Action<string>? MessageReceived;

        // Reference to the ClientCore
        private readonly ClientCore _clientCore;

        // Constructor
        public MessageService()
        {
            // Subscribe to ClientCore message received event
            ClientCore.MessageReceived += OnClientMessageReceived;
        }

        // Handler for ClientCore message received event
        private void OnClientMessageReceived(string msg)
        {
            MessageReceived?.Invoke(msg);
        }
    }

    // Interface for message service
    public interface IMessageService
    {
        event Action<string>? MessageReceived;
    }
}
