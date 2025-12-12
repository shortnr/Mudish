using Client.Utilities;
using Client.Services;

namespace Client.ViewModels
{
    class NewExistingCharacterViewModel : ViewModelBase
    {
        public RelayCommand NewCharacterCommand { get; }
        public RelayCommand ExistingCharacterCommand { get; }

        private readonly INavigationService _nav;

        public NewExistingCharacterViewModel(INavigationService nav)
        {
            NewCharacterCommand = new RelayCommand(NewCharacter);
            ExistingCharacterCommand = new RelayCommand(ExistingCharacter);
            _nav = nav;
        }

        private void NewCharacter()
        {
            _nav.NavigateTo<NewCharacterViewModel>();
        }

        private void ExistingCharacter()
        {
            _nav.NavigateTo<ExistingCharacterViewModel>();
        }
    }
}