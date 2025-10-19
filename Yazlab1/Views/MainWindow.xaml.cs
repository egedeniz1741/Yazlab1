using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Windows;
using System.Windows.Controls; // Button ve ComboBox için eklendi
using Yazlab1.Model;
 // Model -> Models olarak düzeltildi
using Yazlab1.ViewModel;

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

        /// <summary>
        /// "Kullanıcı Ekle" butonuna tıklandığında çalışır.
        /// </summary>
        private void BtnKullaniciEkle_Click(object sender, RoutedEventArgs e)
        {
      
            string email = txtEmail.Text;
            string password = txtPassword.Password;
            string adSoyad = txtAdSoyad.Text;
            Bolum selectedBolum = cmbBolum.SelectedItem as Bolum;

            
            _viewModel.AddNewCoordinator(email, password, adSoyad, selectedBolum);

            // Başarılı eklemeden sonra PasswordBox'ı temizle
            txtPassword.Password = "";
        }

        /// <summary>
        /// Sağ taraftaki bölüm butonlarından herhangi birine tıklandığında çalışır.
        /// </summary>
        private void BtnBolumeGit_Click(object sender, RoutedEventArgs e)
        {
            // Tıklanan butonun 'Tag' özelliğinden bölümün adını al
            if (sender is Button clickedButton && clickedButton.Tag != null)
            {
                string bolumAdi = clickedButton.Tag.ToString();
                _viewModel.NavigateToBolum(bolumAdi);
            }
        }
    }
}