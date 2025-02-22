using System.Windows;
using System.Windows.Controls;
using UserSecretsManager.ViewModels;

namespace UserSecretsManager.Views
{
    public partial class SecretsWindowControl : UserControl
    {
        public SecretsWindowControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecretsViewModel viewModel)
            {
                viewModel.ShowMessage += OnShowMessage;
            }
        }

        private void OnShowMessage(object sender, string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}