using Client.Core;
using Client.ViewModels;
using System.Windows;

namespace Client.Services
{
    // Interface for navigation service
    public interface INavigationService
    {
        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    }

    /// <summary>
    /// Provides navigation functionality for displaying views associated with specific view models within the
    /// application.
    /// </summary>
    /// <remarks>
    /// The NavigationService uses a view model factory and a view locator to resolve and display
    /// views. It is used to navigate between different pages or screens in an application by specifying the
    /// target view model type.
    /// </remarks>
    public class NavigationService : INavigationService
    {
        // Factory function to create ViewModel instances
        private Func<Type, ViewModelBase> _viewModelFactory;

        // Constructor accepting a ViewModel factory
        public NavigationService(Func<Type, ViewModelBase> viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        // Navigate to a view associated with the specified ViewModel type
        public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
        {
            // Create the ViewModel
            var vm = _viewModelFactory(typeof(TViewModel));

            // Ask ViewLocator for the View associated with this VM type
            var view = ViewLocator.CreateViewForViewModel(vm);

            // Display it in the MainWindow (cast is safe because App.Current.MainWindow is your MainWindow)
            ((MainWindow)App.Current.MainWindow).MainFrame.Navigate(view);
        }
    }

    /// <summary>
    /// Provides a method for locating and instantiating views corresponding to view model instances based on naming
    /// conventions.
    /// </summary>
    /// <remarks>
    /// This class uses a convention-based approach to map view models to their associated views.
    /// </remarks>
    public static class ViewLocator
    {
        // Resolves and creates a view for the given ViewModel instance
        public static FrameworkElement CreateViewForViewModel(ViewModelBase vm)
        {
            // Get the type and name of the ViewModel
            var vmType = vm.GetType();
            var vmName = vmType.FullName;

            // Convention: View is same namespace but replace "ViewModel" with "View"
            var viewName = vmName.Replace("ViewModel", "View");

            // Get the Type of the View
            var viewType = Type.GetType(viewName);

            // If view type not found, throw an exception
            if (viewType == null)
                throw new Exception($"View not found for {vmType}");

            // Create an instance of the View and set its DataContext to the ViewModel
            var view = (FrameworkElement)Activator.CreateInstance(viewType);
            view.DataContext = vm;

            return view;
        }
    }
}
