using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls; 
using Yazlab1.Model;

using Yazlab1.ViewModel;
using Yazlab1.Views;

namespace Yazlab1
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel; 

        public MainWindow(Kullanici aktifKullanici)
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel(aktifKullanici);
            this.DataContext = _viewModel;
        }

     
        private void BtnKullaniciEkle_Click(object sender, RoutedEventArgs e)
        {
      
            string email = txtEmail.Text;
            string password = txtPassword.Password;
           
            Bolum selectedBolum = cmbBolum.SelectedItem as Bolum;

            
            _viewModel.AddNewCoordinator(email, password, selectedBolum);

           
            txtPassword.Password = "";
        }

      
        private void BtnBolumeGit_Click(object sender, RoutedEventArgs e)
        {
           
            if (sender is Button clickedButton && clickedButton.Tag != null)
            {
                string bolumAdi = clickedButton.Tag.ToString();
                _viewModel.NavigateToBolum(bolumAdi);
            }
        }
        private void BtnKullaniciGoruntule_Click(object sender, RoutedEventArgs e)
        {
            KullaniciListeWindow kullaniciListeWindow = new KullaniciListeWindow(_viewModel);
            kullaniciListeWindow.Owner = this;
            kullaniciListeWindow.ShowDialog();
        }

    }
}