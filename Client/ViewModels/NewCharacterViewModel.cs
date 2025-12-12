using Client.Services;
using Client.Utilities;

namespace Client.ViewModels
{
    /// <summary>
    /// Represents the view model for creating a new character, providing properties and commands for user input and
    /// actions in the character creation workflow.
    /// </summary>
    /// <remarks>
    /// This view model exposes commands for submitting new character information and clearing input
    /// fields. It is used in UI scenarios where users enter and confirm credentials for a new character.
    /// The class manages basic input validation and navigation through the provided navigation service.
    /// </remarks>
    class NewCharacterViewModel : ViewModelBase
    {
        // Commands
        public RelayCommand LoginCommand { get; }
        public RelayCommand ClearCommand { get; }

        // Navigation Service
        private readonly INavigationService _nav;

        // Constructor taking navigation service as a parameter
        public NewCharacterViewModel(INavigationService nav)
        {
            LoginCommand = new RelayCommand(Login);
            ClearCommand = new RelayCommand(Clear);
            _nav = nav;
        }

        // Properties for character name and passwords
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

        // Method to handle login action
        private void Login()
        {
            // Basic validation before sending new character request
            if (_characterName?.Length > 0 && _password?.Length > 0
                && _password == _passwordRepeat)
                Core.ClientCore.NewCharacter(_characterName, _password);
        }

        // Method to clear input fields
        private void Clear()
        {
            CharacterName = "Character Name";
            Password = "Enter Password";
            PasswordRepeat = "Reenter Password";
        }
    }
}
