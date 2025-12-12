using Client.Core;
using Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Client.Services
{
    public interface INavigationService
    {
        void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    }

    public class NavigationService : INavigationService
    {
        private Func<Type, ViewModelBase> _viewModelFactory;

        public NavigationService(Func<Type, ViewModelBase> viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }
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

    public static class ViewLocator
    {
        public static FrameworkElement CreateViewForViewModel(ViewModelBase vm)
        {
            var vmType = vm.GetType();
            var vmName = vmType.FullName;

            // Convention: View is same namespace but replace "ViewModel" with "View"
            var viewName = vmName.Replace("ViewModel", "View");

            var viewType = Type.GetType(viewName);
            if (viewType == null)
                throw new Exception($"View not found for {vmType}");

            var view = (FrameworkElement)Activator.CreateInstance(viewType);
            view.DataContext = vm;

            return view;
        }
    }
}
