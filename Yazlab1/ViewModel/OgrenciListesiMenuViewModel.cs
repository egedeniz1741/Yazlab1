using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Yazlab1.Data;
using Yazlab1.Model;

namespace Yazlab1.ViewModel
{
    public partial class OgrenciListesiMenuViewModel : ObservableObject
    {
        private readonly SinavTakvimDbContext _dbContext = new SinavTakvimDbContext();
        private readonly int _aktifKullaniciBolumId;

        public OgrenciListesiMenuViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;
            OgrenciNumaralariniYukle();
        }

        [ObservableProperty]
        private ObservableCollection<string> ogrenciNumaralari;

        [ObservableProperty]
        private string secilenOgrenciNumarasi;

        [ObservableProperty]
        private string ogrenciBilgi = "Öğrenci bilgisi burada görünecek...";

        [ObservableProperty]
        private ObservableCollection<DersDto> dersler;

        private void OgrenciNumaralariniYukle()
        {
            try
            {
                var numaralar = _dbContext.Ogrenciler
                    .Where(o => o.BolumID == _aktifKullaniciBolumId)
                    .Select(o => o.OgrenciNo)
                    .OrderBy(no => no)
                    .ToList();

                OgrenciNumaralari = new ObservableCollection<string>(numaralar);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Öğrenciler yüklenirken hata oluştu: {ex.Message}",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OgrenciBilgileriniGetir()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SecilenOgrenciNumarasi))
                {
                    MessageBox.Show("Lütfen bir öğrenci numarası seçiniz veya giriniz!",
                        "Uyarı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Önce öğrenciyi bul
                var ogrenci = _dbContext.Ogrenciler
                    .FirstOrDefault(o => o.OgrenciNo == SecilenOgrenciNumarasi 
                                      && o.BolumID == _aktifKullaniciBolumId);

                if (ogrenci == null)
                {
                    MessageBox.Show("Bu numaraya ait öğrenci bulunamadı!",
                        "Uyarı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    OgrenciBilgi = "Öğrenci bulunamadı...";
                    Dersler = null;
                    return;
                }

                // Öğrenci bilgilerini göster
                OgrenciBilgi = $"Öğrenci İsmi: {ogrenci.AdSoyad}\n" +
                              $"Öğrenci No: {ogrenci.OgrenciNo}\n" +
                              $"Sınıf: {ogrenci.Sinif}";

                // Dersleri ayrı sorguda getir (OgrenciDersKayitlari üzerinden)
                var dersList = _dbContext.OgrenciDersKayitlari
                    .Where(odk => odk.OgrenciID == ogrenci.OgrenciID)
                    .Join(_dbContext.Dersler,
                        odk => odk.DersID,
                        d => d.DersID,
                        (odk, d) => new DersDto
                        {
                            DersAdi = d.DersAdi,
                            DersKodu = d.DersKodu,
                            DersYapisi = d.DersYapisi,
                            Sinif = d.Sinif
                        })
                    .ToList();

                if (dersList.Any())
                {
                    Dersler = new ObservableCollection<DersDto>(dersList);
                }
                else
                {
                    Dersler = null;
                    MessageBox.Show("Bu öğrencinin kayıtlı dersi bulunmamaktadır.",
                        "Bilgi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}\n\nDetay: {ex.InnerException?.Message}",
                    "Hata",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        partial void OnSecilenOgrenciNumarasiChanged(string value)
        {
            // ComboBox'ta seçim değiştiğinde otomatik getir
            if (!string.IsNullOrWhiteSpace(value))
            {
                OgrenciBilgileriniGetir();
            }
        }
    }

    // DTO sınıfı - Ders modeline göre genişletildi
    public class DersDto
    {
        public string DersAdi { get; set; }
        public string DersKodu { get; set; }
        public string DersYapisi { get; set; }
        public int? Sinif { get; set; }
    }
}