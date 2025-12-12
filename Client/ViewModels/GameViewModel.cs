using Client.Core;
using Client.Services;
using Client.Utilities;
using Shared;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client.ViewModels
{
    class GameViewModel : ViewModelBase
    {

        private readonly INavigationService _nav;

        private string? _inputText;

        public GameViewModel(INavigationService nav, IMessageService mess)
        {
            SendCommand = new RelayCommand(Send);
            mess.MessageReceived += msg =>
            {
                Application.Current.Dispatcher.Invoke(() => GameLines.Add(msg));
            };
            _nav = nav;
        }

        public string? InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public RelayCommand SendCommand { get; }
        public ObservableCollection<string> GameLines { get; } = [];

        private void Send()
        {
            ClientCore.IssueCommand(_inputText ?? string.Empty);
            InputText = string.Empty;
        }
    }
}
