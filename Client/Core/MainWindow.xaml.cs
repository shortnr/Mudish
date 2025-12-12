using Client.Services;
using Client.ViewModels;
using Shared;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Client.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {   
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Command quitCommand = new Command();
            quitCommand.CommandType = Types.Commands.QUIT;

            byte[] quitBytes = Message.GenerateMessage(quitCommand, Types.MessageType.COMMAND);
            Client.Services.AsynchronousClient.Send(ClientCore.GetServer(), quitBytes);
        }
    }
}
