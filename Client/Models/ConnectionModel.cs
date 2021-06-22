using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    class ConnectionModel
    {
        private Socket _connection;
        private string _ip;
        private string _port;

        public Socket Connection
        {
            get
            {
                return _connection;
            }
            set
            {
                _connection = value;
            }
        }
        public string IP
        {
            get
            {
                return _ip;
            }
            set
            {
                _ip = value;
            }
        }
        public string port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }
    }
}
