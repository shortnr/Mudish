using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Client.ViewModels
{
    /// <summary>
    /// Provides a base class for view models that implements the INotifyPropertyChanged interface to
    /// support property change notifications.
    /// </summary>
    /// <remarks>
    /// This class is intended to be used as a base for view models in applications that follow the
    /// Model-View-ViewModel (MVVM) pattern. It provides helper methods to raise property change
    /// notifications and to simplify property setters. Derived classes can use the SetProperty method
    /// to update property values and automatically notify listeners of changes.
    /// </remarks>
    public class ViewModelBase : INotifyPropertyChanged
    {
        // PropertyChanged event from INotifyPropertyChanged interface
        public event PropertyChangedEventHandler? PropertyChanged;

        // Notify listeners that a property value has changed
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Set the property value and notify listeners if it has changed
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propName = null)
        {
            // Check if the new value is equal to the current value
            if (Equals(storage, value))
                return false;

            // Update the storage and notify listeners of the change
            storage = value;
            OnPropertyChanged(propName);
            return true;
        }
    }
}
