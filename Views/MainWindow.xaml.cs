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
