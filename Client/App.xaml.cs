using Client.Core;
using Client.ViewModels;
using Client.Services;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Represents the entry point and application-level services for the WPF application.
    /// </summary>
    /// <remarks>
    /// The App class initializes core services such as navigation and messaging, and provides a
    /// factory for creating view model instances. These services are exposed as static properties
    /// for use throughout the application. App derives from Application and manages application
    /// startup and main window display.
    /// </remarks>
    public partial class App : Application
    {
        // Services
        public static INavigationService NavigationService { get; private set; }
        public static IMessageService MessageService { get; private set; }
        
        // ViewModel factory delegate
        public static Func<Type, ViewModelBase>? ViewModelFactory { get; private set; }

        // Startup override to initialize services and show main window
        protected override void OnStartup(StartupEventArgs e)
        {
            // Call base startup logic
            base.OnStartup(e);

            // Initialize services
            NavigationService = new NavigationService(CreateViewModel);
            MessageService = new MessageService();

            // Assign ViewModel factory
            ViewModelFactory = CreateViewModel;

            // Create and show main window
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Navigate to initial view model
            NavigationService.NavigateTo<ServerConnectViewModel>();
        }

        // Factory method to create ViewModel instances based on type
        private ViewModelBase CreateViewModel(Type vmType)
        {
            if (vmType == typeof(ServerConnectViewModel))
                return new ServerConnectViewModel(NavigationService);

            if (vmType == typeof(NewExistingCharacterViewModel))
                return new NewExistingCharacterViewModel(NavigationService);

            if (vmType == typeof(NewCharacterViewModel))
                return new NewCharacterViewModel(NavigationService);

            if (vmType == typeof(ExistingCharacterViewModel))
                return new ExistingCharacterViewModel(NavigationService);

            if (vmType == typeof(GameViewModel))
                return new GameViewModel(NavigationService, MessageService);

            throw new Exception("Unknown ViewModel type " + vmType.Name);
        }
    }
}
