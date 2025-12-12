using Client.Services;
using Client.Utilities;

namespace Client.ViewModels
{
    /// <summary>
    /// Represents the view model for logging in with an existing character.
    /// </summary>
    /// <remarks>
    /// This view model exposes properties for the character name and password, and provides a
    /// command to initiate the login process. Navigation to the game view occurs upon
    /// successful login.
    /// </remarks>
    class ExistingCharacterViewModel : ViewModelBase
    {
        // Fields
        private readonly INavigationService _nav;
        private string? _characterName;
        private string? _password;

        // Constructor taking navigation service as a parameter
        public ExistingCharacterViewModel(INavigationService nav)
        {
            // Initialize the login command with the Login method
            LoginCommand = new RelayCommand(Login);
            _nav = nav;
        }

        // Properties for character name and password
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

        // Command to execute the login process
        public RelayCommand LoginCommand { get; }

        // Method to handle the login logic
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
