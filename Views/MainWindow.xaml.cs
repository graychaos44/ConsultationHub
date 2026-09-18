using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConsultationLedger.ViewModels;

namespace ConsultationLedger.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Activate();
                Focus();
            }
            catch
            {
                // Fallback gracefully
            }
        }

        private void OnCustomerInfoInputChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CheckDuplicates();
            }
        }

        private void OnWritingTextBoxPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (DataContext is MainViewModel vm)
                {
                    if (e.Delta > 0)
                    {
                        vm.IncreaseFontSizeCommand.Execute(null);
                    }
                    else if (e.Delta < 0)
                    {
                        vm.DecreaseFontSizeCommand.Execute(null);
                    }
                    e.Handled = true;
                }
            }
        }
    }
}
