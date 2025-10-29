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
using Yazlab1.ViewModel;

namespace Yazlab1.Views
{
    public partial class KullaniciListeWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public KullaniciListeWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;


            _viewModel.LoadAllUsers();
            dgKullanicilar.ItemsSource = _viewModel.KullaniciListesi;
        }

        private void BtnKullaniciSil_Click(object sender, RoutedEventArgs e)
        {
            var selectedKullanici = dgKullanicilar.SelectedItem as Model.Kullanici;
            _viewModel.DeleteUser(selectedKullanici);
        }
    }
}
