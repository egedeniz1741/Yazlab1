using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MigraDoc.Rendering.UnitTest;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Model;
using Yazlab1.Views;

namespace Yazlab1.ViewModel
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly Kullanici _aktifKullanici;
        private readonly int _aktifKullaniciBolumId;
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public MainWindowViewModel(Kullanici aktifKullanici)
        {
            _aktifKullanici = aktifKullanici;
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;
        }

        [RelayCommand]
        private void BilgisayarEkraninaGit()
        {
            DerslikYonetimWindow derslikYonetimWindow = new DerslikYonetimWindow(_aktifKullanici);
            derslikYonetimWindow.Show();
        }

        [RelayCommand]
        private void DigerBolumler(string bolumAdi)
        {
            MessageBox.Show(
                $"{bolumAdi} henüz kullanılmamaktadır.",
                "Bilgilendirme",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

    }
}
