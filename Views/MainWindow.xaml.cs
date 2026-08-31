using System.Windows;
using System.Windows.Controls;
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
            Activate();
            Focus();
            Topmost = true;
            Topmost = false;
        }

        private void OnCustomerInfoInputChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CheckDuplicates();
            }
        }
    }
}
