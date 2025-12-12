using System.Windows.Input;

namespace Client.Utilities
{
    /// <summary>
    /// Represents a command that relays its functionality to specified delegates. Used to bind UI actions to
    /// logic.
    /// </summary>
    /// <remarks>
    /// RelayCommand enables the separation of UI and business logic by allowing actions and their
    /// availability to be defined in view models. It implements the ICommand interface, making it suitable
    /// for use with data binding. The command's ability to execute can be dynamically controlled by providing
    /// a canExecute delegate and raising the CanExecuteChanged event when conditions change.
    /// </remarks>
    public class RelayCommand : ICommand
    {
        // Action to execute when the command is invoked.
        private readonly Action _execute;

        // Function to determine if the command can execute.
        private readonly Func<bool>? _canExecute;

        // Constructor accepting the execute action and an optional canExecute function.
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Event raised when the ability of the command to execute changes.
        public event EventHandler? CanExecuteChanged;

        // Determines whether the command can execute in its current state.
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        // Executes the command.
        public void Execute(object? parameter) => _execute();

        // Raises the CanExecuteChanged event to notify that the command's ability to execute has changed.
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}