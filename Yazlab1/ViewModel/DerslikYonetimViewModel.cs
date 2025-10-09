using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Yazlab1.Data;
using Yazlab1.Model;
using Yazlab1.Models;

namespace Yazlab1.ViewModel
{
    
    public partial class DerslikYonetimViewModel : ObservableObject
    {
        private readonly SinavTakvimDbContext _dbContext = new();

       
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
        private string _siraYapisi;

   
        [ObservableProperty]
        private Derslik _selectedDerslik;

       
        public ObservableCollection<Derslik> Derslikler { get; set; }

        public DerslikYonetimViewModel()
        {
            Derslikler = new ObservableCollection<Derslik>();
            DerslikleriYukle();
        }

        private void DerslikleriYukle()
        {
            Derslikler.Clear();
            var derslikler = _dbContext.Derslikler.ToList();
            foreach (var derslik in derslikler)
            {
                Derslikler.Add(derslik);
            }
        }

       
        [RelayCommand]
        private void Ekle()
        {
            var yeniDerslik = new Derslik
            {
                
                BolumID = 1,
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

            SelectedDerslik.DerslikKodu = DerslikKodu;
            SelectedDerslik.DerslikAdi = DerslikAdi;
            SelectedDerslik.Kapasite = Kapasite;
            SelectedDerslik.EnineSiraSayisi = EnineSiraSayisi;
            SelectedDerslik.BoyunaSiraSayisi = BoyunaSiraSayisi;
            SelectedDerslik.SiraYapisi = SiraYapisi;

            _dbContext.SaveChanges();
            DerslikleriYukle();
            MessageBox.Show("Derslik başarıyla güncellendi.");
        }

        [RelayCommand]
        private void Sil()
        {
            if (SelectedDerslik == null)
            {
                MessageBox.Show("Lütfen silmek için bir derslik seçin.");
                return;
            }

            _dbContext.Derslikler.Remove(SelectedDerslik);
            _dbContext.SaveChanges();
            DerslikleriYukle();
            MessageBox.Show("Derslik başarıyla silindi.");
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
            }
        }
    }
}