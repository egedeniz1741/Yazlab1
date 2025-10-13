using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Yazlab1.Data;
using Yazlab1.Model;
using Yazlab1.Models;

namespace Yazlab1.ViewModel
{
    public partial class DerslikYonetimViewModel : ObservableObject
    {
        private readonly SinavTakvimDbContext _dbContext = new SinavTakvimDbContext();
        private readonly int _aktifKullaniciBolumId;

        [ObservableProperty] private string _derslikKodu;
        [ObservableProperty] private string _derslikAdi;
        [ObservableProperty] private int _kapasite;
        [ObservableProperty] private int _enineSiraSayisi;
        [ObservableProperty] private int _boyunaSiraSayisi;
        [ObservableProperty] private int _siraYapisi;
        [ObservableProperty] private Derslik _selectedDerslik;

        public ObservableCollection<Derslik> Derslikler { get; set; }
        public ObservableCollection<object> SeatLayout { get; set; }

        public DerslikYonetimViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;
            Derslikler = new ObservableCollection<Derslik>();
            SeatLayout = new ObservableCollection<object>();
            DerslikleriYukle();
        }

        private void DerslikleriYukle()
        {
            Derslikler.Clear();
            var derslikler = _dbContext.Derslikler
                                       .Where(d => d.BolumID == _aktifKullaniciBolumId)
                                       .ToList();
            foreach (var derslik in derslikler)
            {
                Derslikler.Add(derslik);
            }
        }

        [RelayCommand]
        private void Ekle()
        {
         
            if (string.IsNullOrWhiteSpace(DerslikKodu) || string.IsNullOrWhiteSpace(DerslikAdi))
            {
                MessageBox.Show("Derslik Kodu ve Adı alanları boş bırakılamaz.");
                return;
            }

          
            bool isDuplicate = _dbContext.Derslikler.Any(d => d.DerslikKodu == DerslikKodu && d.BolumID == _aktifKullaniciBolumId);
            if (isDuplicate)
            {
                MessageBox.Show("Bu derslik kodu zaten mevcut. Lütfen farklı bir kod girin.");
                return;
            }

          
           

            if (SiraYapisi * EnineSiraSayisi * BoyunaSiraSayisi != Kapasite)
            {
                MessageBox.Show("Verdiğiniz sıra bilgileri (Sıra Yapısı * Enine Sıra * Boyuna Sıra) kapasite ile eşleşmiyor lütfen düzeltiniz");
                return;
            }

          
            var yeniDerslik = new Derslik
            {
                BolumID = _aktifKullaniciBolumId,
                DerslikKodu = DerslikKodu,
                DerslikAdi = DerslikAdi,
                Kapasite = Kapasite,
                EnineSiraSayisi = EnineSiraSayisi,
                BoyunaSiraSayisi = BoyunaSiraSayisi,
                SiraYapisi = SiraYapisi
            };

            _dbContext.Derslikler.Add(yeniDerslik);
            _dbContext.SaveChanges();
            DerslikleriYukle(); // Listeyi yenile
            MessageBox.Show("Derslik başarıyla eklendi.");
        }

        // Guncelle metodu aynı, SelectedDerslik'e bakar
        [RelayCommand]
        private void Guncelle()
        {
            if (SelectedDerslik == null)
            {
                MessageBox.Show("Lütfen güncellemek için bir derslik seçin.");
                return;
            }
            // ... (Guncelle kodunun geri kalanı doğru)
        }

        // Sil metodu aynı, SelectedDerslik'e bakar
        [RelayCommand]
        private void Sil()
        {
            if (SelectedDerslik == null)
            {
                MessageBox.Show("Lütfen silmek için bir derslik seçin.");
                return;
            }
            // ... (Sil kodunun geri kalanı doğru)
        }

        // ... (OnSelectedDerslikChanged ve GenerateSeatLayout aynı) ...

        /// <summary>
        /// "2'şerli", "3 lu", "1" gibi metinlerden sayıyı çıkaran yardımcı metot.
        /// </summary>
        private int ParseSiraYapisi(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            // Metnin başındaki sayıları bulur (örn: "2'şerli" -> "2")
            var match = Regex.Match(text, @"^\d+");
            if (match.Success && int.TryParse(match.Value, out int sayi))
            {
                return sayi;
            }
            return 0; // Geçerli bir sayı bulunamazsa
        }
    }
}