using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for ExistingCharacterWindow.xaml
    /// </summary>
    public partial class ExistingCharacterWindow : Window
    {
        public ExistingCharacterWindow()
        {
            InitializeComponent();
        }

        private void NameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.Text == "Character Name")
            {
                NameTextBox.Text = "";
                NameTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.Text == "")
            {
                NameTextBox.Text = "Character Name";
                NameTextBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void PasswordTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PasswordTextBox.Text == "Enter Password")
            {
                PasswordTextBox.Text = "";
                PasswordTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void PasswordTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PasswordTextBox.Text == "")
            {
                PasswordTextBox.Text = "Enter Password";
                PasswordTextBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void PasswordTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Return) &&
                NameTextBox.Text.Length > 0 && PasswordTextBox.Text.Length > 0)
                SendMessage();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.Text.Length > 0 && PasswordTextBox.Text.Length > 0)
                SendMessage();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            Owner = Application.Current.MainWindow;
        }

        private void SendMessage()
        {
            ClientCore.ExistingCharacter(NameTextBox.Text, PasswordTextBox.Text);
        }
    }
}
