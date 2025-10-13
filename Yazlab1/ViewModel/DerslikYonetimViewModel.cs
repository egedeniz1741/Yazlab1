using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Yazlab1.Data;
using Yazlab1.Model;
using Yazlab1.Views;

namespace Yazlab1.ViewModel
{
    public partial class DerslikYonetimViewModel : ObservableObject
    {
        private readonly SinavTakvimDbContext _dbContext = new();
        private readonly Kullanici _aktifKullanici;
        private readonly int _aktifKullaniciBolumId;

        [ObservableProperty]
        private string _derslikKodu;

        [ObservableProperty]
        private string _derslikAdi;

        [ObservableProperty]
        private int _kapasite;

        [ObservableProperty]
        private int _enineSiraSayisi;

        [ObservableProperty]
        private int _boyunaSiraSayisi;

        
        [ObservableProperty]
        private int _siraYapisi;

        [ObservableProperty]
        private Derslik _selectedDerslik;

        public ObservableCollection<Derslik> Derslikler { get; set; }
        public ObservableCollection<object> SeatLayout { get; set; }

        public DerslikYonetimViewModel(Kullanici aktifKullanici)
        {
            _aktifKullanici = aktifKullanici;
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
            DerslikleriYukle();
            MessageBox.Show("Derslik başarıyla eklendi.");
        }

        [RelayCommand]
        private void Guncelle()
        {
            if (SelectedDerslik == null)
            {
                MessageBox.Show("Lütfen güncellemek için bir derslik seçin.");
                return;
            }

            var dbDerslik = _dbContext.Derslikler.Find(SelectedDerslik.DerslikID);
            if (dbDerslik != null)
            {
                if (dbDerslik.DerslikKodu != DerslikKodu && _dbContext.Derslikler.Any(d => d.DerslikKodu == DerslikKodu && d.BolumID == _aktifKullaniciBolumId))
                {
                    MessageBox.Show("Girmek istediğiniz yeni derslik kodu başka bir dersliğe aittir.");
                    return;
                }

             
                dbDerslik.DerslikKodu = DerslikKodu;
                dbDerslik.DerslikAdi = DerslikAdi;
                dbDerslik.Kapasite = Kapasite;
                dbDerslik.EnineSiraSayisi = EnineSiraSayisi;
                dbDerslik.BoyunaSiraSayisi = BoyunaSiraSayisi;
                dbDerslik.SiraYapisi = SiraYapisi;

                _dbContext.SaveChanges();
                DerslikleriYukle();
                MessageBox.Show("Derslik başarıyla güncellendi.");
            }
        }

        [RelayCommand]
        private void Sil()
        {
            if (SelectedDerslik == null)
            {
                MessageBox.Show("Lütfen silmek için bir derslik seçin.");
                return;
            }

            var dbDerslik = _dbContext.Derslikler.Find(SelectedDerslik.DerslikID);
            if (dbDerslik != null)
            {
                _dbContext.Derslikler.Remove(dbDerslik);
                _dbContext.SaveChanges();
                DerslikleriYukle();
                MessageBox.Show("Derslik başarıyla silindi.");
            }
        }

        [RelayCommand]
        private void ExcelEkraninaGit()
        {
            ParsingWindow parsingWindow = new(_aktifKullanici);
            parsingWindow.Show();
        }

        partial void OnSelectedDerslikChanged(Derslik value)
        {
            if (value != null)
            {
                DerslikKodu = value.DerslikKodu;
                DerslikAdi = value.DerslikAdi;
                Kapasite = value.Kapasite;
                EnineSiraSayisi = value.EnineSiraSayisi;
                BoyunaSiraSayisi = value.BoyunaSiraSayisi;
                SiraYapisi = value.SiraYapisi;
                GenerateSeatLayout();
            }
        }

        private void GenerateSeatLayout()
        {
            SeatLayout.Clear();
            if (SelectedDerslik == null || SelectedDerslik.BoyunaSiraSayisi <= 0 || SelectedDerslik.EnineSiraSayisi <= 0)
            {
                return;
            }
            int totalSeats = SelectedDerslik.BoyunaSiraSayisi * SelectedDerslik.EnineSiraSayisi;
            for (int i = 0; i < totalSeats; i++)
            {
                SeatLayout.Add(new object());
            }
        }

    }
}