using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace Client
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public Views.NewExistingCharacterWindow newExisting;

        public LoginWindow()
        {
            InitializeComponent();
            
            newExisting = new Views.NewExistingCharacterWindow();
        }

        private void ServerInputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if(ServerInputBox.Text == "Server")
            {
                ServerInputBox.Text = "";
                ServerInputBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void ServerInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ServerInputBox.Text == "")
            {
                ServerInputBox.Text = "Server";
                ServerInputBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void PortInputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PortInputBox.Text == "Port")
            {
                PortInputBox.Text = "";
                PortInputBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void PortInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PortInputBox.Text == "")
            {
                PortInputBox.Text = "Port";
                PortInputBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string ip = ServerInputBox.Text;
            string ipFixed = "";
            int port = Int32.Parse(PortInputBox.Text);

            try
            {
                string[] ipChopped = ip.Split('.');
                ipFixed = Int32.Parse(ipChopped[0]).ToString() + "." +
                          Int32.Parse(ipChopped[1]).ToString() + "." +
                          Int32.Parse(ipChopped[2]).ToString() + "." +
                          Int32.Parse(ipChopped[3]).ToString() + ".";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

            ClientCore.NewAsyncClient(port, ipFixed);
            ClientCore.StartSocket();

            ConnectionAccepted();
        }

        public void ConnectionAccepted()
        {
            Close();
            newExisting.Owner = Owner;
            newExisting.Show();
        }
                private void Default_Click(object sender, RoutedEventArgs e)
        {
            ServerInputBox.Text = "mudish.com";
            PortInputBox.Text = "11000";
        }
    }
}
