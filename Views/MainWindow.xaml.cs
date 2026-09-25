using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ConsultationLedger.Models;
using ConsultationLedger.ViewModels;

namespace ConsultationLedger.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
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

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + F: Focus Search Box
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            // F5: Refresh Data
            else if (e.Key == Key.F5)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.LoadData();
                    e.Handled = true;
                }
            }
        }

        private void OnCustomerInfoInputChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CheckDuplicates();
            }
        }

        private void OnPhoneTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is MainViewModel vm)
            {
                string raw = tb.Text;
                string formatted = ConsultationRecord.FormatPhoneNumber(raw);
                if (raw != formatted)
                {
                    vm.EditRecord.ClientPhone = formatted;
                    tb.Text = formatted;
                }
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
