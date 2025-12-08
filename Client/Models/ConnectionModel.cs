using System.Net.Sockets;

namespace Client.Models
{
    /// <summary>
    /// Represents a network connection model holding a Socket and connection endpoint information (IP and port).
    /// </summary>
    class ConnectionModel
    {
        private Socket _connection;
        private string _ip;
        private string _port;

        public Socket Connection { get; set; }
        public string IP { get; set; }
        public string port { get; set; }
    }
}
