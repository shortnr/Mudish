using Client.Core;
using Client.Services;
using Client.Utilities;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client.ViewModels
{
    /// <summary>
    /// Represents the view model for the game interface, managing user input and game messages.
    /// </summary>
    /// <remarks>
    /// This view model coordinates user interactions within the game view, including sending
    /// commands and displaying received messages. It exposes properties and commands for data
    /// binding in the UI and relies on a messaging service for communication from the core game
    /// logic.
    /// </remarks>
    class GameViewModel : ViewModelBase
    {
        // Navigation service for view transitions; not cuurently used but available for future use.
        private readonly INavigationService _nav;

        // Backing field for the InputText property, storing the current user input.
        private string? _inputText;

        // GameViewModel constructor initializes commands and subscribes to message service.
        public GameViewModel(INavigationService nav, IMessageService mess)
        {
            SendCommand = new RelayCommand(Send);
            mess.MessageReceived += msg =>
            {
                Application.Current.Dispatcher.Invoke(() => GameLines.Add(msg));
            };
            _nav = nav;
        }

        // Property for user input text
        public string? InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        // Command to send user input to the game core
        public RelayCommand SendCommand { get; }

        // Collection of game messages to be displayed in the UI
        public ObservableCollection<string> GameLines { get; } = [];

        // Method to send the current input text to the game core and clear the input field
        private void Send()
        {
            ClientCore.IssueCommand(_inputText ?? string.Empty);
            InputText = string.Empty;
        }
    }
}
