using Client.Services;
using Shared;
using System.ComponentModel;

namespace Client.Core
{
    /// <summary>
    /// Represents the main window of the application, providing the primary user interface and
    /// handling application-level events.
    /// </summary>
    /// <remarks>
    /// This class is instantiated by the application startup logic and serves as the entry point
    /// for user interaction.
    /// </remarks>
    public partial class MainWindow
    {
        // Constructor
        public MainWindow()
        {
            InitializeComponent();
        }

        // Event handler for window closing event
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Create a quit command to notify the server of the client's intent to disconnect
            Command quitCommand = new Command();
            quitCommand.CommandType = Types.Commands.QUIT;

            // Serialize the quit command into a byte array message
            byte[] quitBytes = Message.GenerateMessage(quitCommand, Types.MessageType.COMMAND);

            // Send the quit message to the server asynchronously
            AsynchronousClient.Send(ClientCore.GetServer(), quitBytes);
        }
    }
}
