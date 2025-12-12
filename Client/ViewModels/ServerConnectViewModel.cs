using Client.Services;
using Client.Utilities;

namespace Client.ViewModels
{
    class ServerConnectViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private string? _address;
        private string? _port;

        public ServerConnectViewModel(INavigationService nav)
        {
            ConnectCommand = new RelayCommand(Connect);
            DefaultCommand = new RelayCommand(Default);
            _nav = nav;
        }

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

        public RelayCommand ConnectCommand { get; }
        public RelayCommand DefaultCommand { get; }

        private void Connect()
        {
            if (_address?.Length > 0 && _port?.Length > 0)
            {
                int portNum = Int32.Parse(_port);
                Core.ClientCore.Connect(_address, portNum);
                _nav.NavigateTo<NewExistingCharacterViewModel>();
            }
        }

        private void Default()
        {
            Address = "127.0.0.1";
            Port = "11000";
        }
    }
}
