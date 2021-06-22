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
    /// Interaction logic for NewExistingCharacterWindow.xaml
    /// </summary>
    public partial class NewExistingCharacterWindow : Window
    {
        public ExistingCharacterWindow existingCharacter;
        public NewCharacterWindow newCharacter;
        
        public NewExistingCharacterWindow()
        {
            InitializeComponent();
        }

        private void ExistingCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            existingCharacter = new ExistingCharacterWindow();
            existingCharacter.Show();
            Close();
        }

        private void NewCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            newCharacter = new NewCharacterWindow();
            newCharacter.Show();
            Close();
        }
    }
}
