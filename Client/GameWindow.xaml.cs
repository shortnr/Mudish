using System;
using System.Threading;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Shared;
using System.Net.Sockets;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        public LoginWindow login;
        
        public MainWindow()
        {
            DataContext = this;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            login = new LoginWindow();
        }

        private string _gameText = "";
        public string gameText
        {
            get
            {
                return _gameText;
            }

            set
            {
                _gameText = value;
                OnPropertyChanged();

                if (Scroller.VerticalOffset == Scroller.ScrollableHeight)
                    Scroller.ScrollToBottom();
            }
        }

        private void Connect()
        {
            IsEnabled = false;
            login.Owner = this;
            
            login.Show();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ClientCore.IssueCommand(Input.Text);
            Input.Text = "";
        }

        public void AppendTextBlock(string newText)
        {
            gameText += Environment.NewLine + newText;
        }

        private void GameWindow_Initialized(object sender, EventArgs e)
        {
            
        }

        private void GameWindow_Closing(object sender, CancelEventArgs e)
        {
            Command quitCommand = new Command();
            quitCommand.CommandType = Types.Commands.QUIT;

            byte[] quitBytes = Message.GenerateMessage(quitCommand, Types.MessageType.COMMAND);
            AsynchronousClient.Send(ClientCore.GetServer(), quitBytes);
        }

        private void Input_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Return) && Input.Text.Length > 0)
            {
                ClientCore.IssueCommand(Input.Text);
                Input.Text = "";
            }
        }

        private void GameWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Connect();
        }
    }
}
