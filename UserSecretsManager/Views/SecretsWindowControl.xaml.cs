using System.Windows;
using System.Windows.Controls;
using UserSecretsManager.ViewModels;

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

            var _ = new Microsoft.Xaml.Behaviors.DefaultTriggerAttribute(typeof(Trigger), typeof(Microsoft.Xaml.Behaviors.TriggerBase), null);
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
