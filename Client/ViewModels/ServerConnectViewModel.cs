using Client.Services;
using Client.Utilities;

namespace Client.ViewModels
{
    /// <summary>
    /// Represents the view model for connecting to a server, providing properties and commands for specifying the
    /// server address and port, and initiating a connection.
    /// </summary>
    /// <remarks>
    /// This view model is used when the user must enter or select a server address and port before connecting.
    /// It exposes commands for connecting to the specified server and for resetting the address and port to default
    /// values.
    /// </remarks>
    class ServerConnectViewModel : ViewModelBase
    {
        // Fields for storing the server address and port and a reference to the navigation service.
        private readonly INavigationService _nav;
        private string? _address;
        private string? _port;

        // Constructor that initializes the view model with the navigation service and sets up commands.
        public ServerConnectViewModel(INavigationService nav)
        {
            ConnectCommand = new RelayCommand(Connect);
            DefaultCommand = new RelayCommand(Default);
            _nav = nav;
        }

        // Properties for binding the server address and port to the view.
        public string? Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }
        
        public string? Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        // Commands for connecting to the server and resetting to default values.
        public RelayCommand ConnectCommand { get; }
        public RelayCommand DefaultCommand { get; }

        // Method to connect to the server using the specified address and port.
        private void Connect()
        {
            if (_address?.Length > 0 && _port?.Length > 0)
            {
                int portNum = Int32.Parse(_port);
                Core.ClientCore.Connect(_address, portNum);
                _nav.NavigateTo<NewExistingCharacterViewModel>();
            }
        }

        // Method to reset the server address and port to default values. Empty strings
        // result in a watermarked default value in the view, defined in the XAML.
        private void Default()
        {
            Address = "";
            Port = "";
        }
    }
}
