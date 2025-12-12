using Client.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Services
{
    public class MessageService : IMessageService
    {
        public event Action<string>? MessageReceived;

        private readonly ClientCore _clientCore;

        public MessageService()
        {
            ClientCore.MessageReceived += OnClientMessageReceived;
        }

        private void OnClientMessageReceived(string msg)
        {
            // Optionally do preprocessing here
            MessageReceived?.Invoke(msg);
        }
    }

    public interface IMessageService
    {
        event Action<string>? MessageReceived;
    }
}
