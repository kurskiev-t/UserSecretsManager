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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserSecretsManager.Viewmodels;

namespace UserSecretsManager.Views
{
    /// <summary>
    /// Interaction logic for SecretsWindowControl.xaml
    /// </summary>
    public partial class SecretsWindowControl : UserControl
    {
        public SecretsWindowControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecretsViewModel viewModel)
                viewModel.ShowMessage += OnShowMessage;
        }

        private void OnShowMessage(object sender, string message)
        {
            // Здесь можно либо показать MessageBox, либо вызвать отдельную View для сообщения
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
