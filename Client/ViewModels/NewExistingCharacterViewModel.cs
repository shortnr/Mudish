using Client.Utilities;
using Client.Services;

namespace Client.ViewModels
{
    /// <summary>
    /// Represents the view model for selecting between creating a new character or using an existing character.
    /// </summary>
    /// <remarks>
    /// This view model provides commands for navigating to either the new character creation workflow or the
    /// existing character selection workflow. It is used as the initial step in a character management user
    /// interface.
    /// </remarks>
    class NewExistingCharacterViewModel : ViewModelBase
    {
        // Commands for navigating to new or existing character workflows
        public RelayCommand NewCharacterCommand { get; }
        public RelayCommand ExistingCharacterCommand { get; }

        // Navigation service for handling view transitions
        private readonly INavigationService _nav;

        // Constructor initializing commands and navigation service
        public NewExistingCharacterViewModel(INavigationService nav)
        {
            NewCharacterCommand = new RelayCommand(NewCharacter);
            ExistingCharacterCommand = new RelayCommand(ExistingCharacter);
            _nav = nav;
        }

        // Method to navigate to the new character creation view
        private void NewCharacter()
        {
            _nav.NavigateTo<NewCharacterViewModel>();
        }

        // Method to navigate to the existing character selection view
        private void ExistingCharacter()
        {
            _nav.NavigateTo<ExistingCharacterViewModel>();
        }
    }
}