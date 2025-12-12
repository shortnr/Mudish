using Client.Services;
using Client.Utilities;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.ViewModels
{
    class NewCharacterViewModel : ViewModelBase
    {
        public RelayCommand LoginCommand { get; }
        public RelayCommand ClearCommand { get; }

        private readonly INavigationService _nav;

        public NewCharacterViewModel(INavigationService nav)
        {
            LoginCommand = new RelayCommand(Login);
            ClearCommand = new RelayCommand(Clear);
            _nav = nav;
        }

        private string? _characterName;
        public string? CharacterName
        {
            get => _characterName;
            set => SetProperty(ref _characterName, value);
        }

        private string? _password;
        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string? _passwordRepeat;
        public string? PasswordRepeat
        {
            get => _passwordRepeat;
            set => SetProperty(ref _passwordRepeat, value);
        }

        private void Login()
        {
            if (_characterName?.Length > 0 && _password?.Length > 0
                && _password == _passwordRepeat)
                Core.ClientCore.NewCharacter(_characterName, _password);
        }

        private void Clear()
        {
            CharacterName = "Character Name";
            Password = "Enter Password";
            PasswordRepeat = "Reenter Password";
        }
    }
}
