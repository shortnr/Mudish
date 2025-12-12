using Client.Services;
using Client.Utilities;

namespace Client.ViewModels
{
    class ExistingCharacterViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private string? _characterName;
        private string? _password;

        public ExistingCharacterViewModel(INavigationService nav)
        {
            LoginCommand = new RelayCommand(Login);
            _nav = nav;
        }

        public string? CharacterName
        {
            get => _characterName;
            set => SetProperty(ref _characterName, value);
        }

        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public RelayCommand LoginCommand { get; }

        private void Login()
        {
            if (_characterName?.Length > 0 && _password?.Length > 0)
            {
                Core.ClientCore.ExistingCharacter(_characterName, _password);
                _nav.NavigateTo<GameViewModel>();
            }
        }
    }
}
