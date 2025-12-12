using Client.Core;
using Client.ViewModels;
using Client.Services;
using System.Windows;
using System.Diagnostics;

namespace Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static INavigationService Navigation { get; private set; }
        public static IMessageService MessageService { get; private set; }
        public static Func<Type, ViewModelBase>? ViewModelFactory { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Navigation = new NavigationService(CreateViewModel);
            MessageService = new MessageService();

            // VM resolver/factory
            ViewModelFactory = CreateViewModel;

            var mainWindow = new MainWindow();

            mainWindow.Show();

            Navigation.NavigateTo<ServerConnectViewModel>();
        }

        private ViewModelBase CreateViewModel(Type vmType)
        {
            if (vmType == typeof(ServerConnectViewModel))
                return new ServerConnectViewModel(Navigation);

            if (vmType == typeof(NewExistingCharacterViewModel))
                return new NewExistingCharacterViewModel(Navigation);

            if (vmType == typeof(NewCharacterViewModel))
                return new NewCharacterViewModel(Navigation);

            if (vmType == typeof(ExistingCharacterViewModel))
                return new ExistingCharacterViewModel(Navigation);

            if (vmType == typeof(GameViewModel))
                return new GameViewModel(Navigation, MessageService);

            throw new Exception("Unknown ViewModel type " + vmType.Name);
        }
    }
}
