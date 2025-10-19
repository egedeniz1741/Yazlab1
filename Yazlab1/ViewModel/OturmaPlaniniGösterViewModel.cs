using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
    public class OturmaPlaniGosterViewModel : INotifyPropertyChanged
    {
       
        private Derslik _secilenDerslik;
        private readonly AtanmisSinav _gosterilecekSinav;
        private List<OturmaPlaniOgrenciDetay> _tumYerlesimListesi; 

        
        public string PencereBasligi { get; }
        public ObservableCollection<Derslik> DerslikListesi { get; set; }
        public ObservableCollection<OturmaPlaniOgrenciDetay> GorselOturmaPlani { get; private set; }

        public Derslik SecilenDerslik
        {
            get => _secilenDerslik;
            set
            {
                if (_secilenDerslik != value)
                {
                    _secilenDerslik = value;
                    OnPropertyChanged();
                    SecilenDerslikDegisti(value); 
                }
            }
        }

       


        public OturmaPlaniGosterViewModel(AtanmisSinav gosterilecekSinav, string sinavAdiBasligi)
        {
            _gosterilecekSinav = gosterilecekSinav ?? throw new ArgumentNullException(nameof(gosterilecekSinav));
            PencereBasligi = $"{sinavAdiBasligi} - {gosterilecekSinav.SinavDetay?.Ders?.DersAdi ?? ""} Oturma Planı";

            var siraliDerslikler = gosterilecekSinav.AtananDerslikler != null
                ? gosterilecekSinav.AtananDerslikler.OrderBy(d => d.DerslikKodu).ToList()
                : new List<Derslik>();

            DerslikListesi = new ObservableCollection<Derslik>(siraliDerslikler);
            GorselOturmaPlani = new ObservableCollection<OturmaPlaniOgrenciDetay>();
            _tumYerlesimListesi = new List<OturmaPlaniOgrenciDetay>();

          
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
           
            await Task.Run(() => TumOturmaPlaniniHesapla());

            
            Application.Current.Dispatcher.Invoke(() =>
            {
                SecilenDerslik = DerslikListesi.FirstOrDefault();
            });
        }


      
        private void TumOturmaPlaniniHesapla()
        {
            var hesaplananListe = new List<OturmaPlaniOgrenciDetay>();
            var ogrenciler = _gosterilecekSinav?.SinavDetay?.Ogrenciler?.OrderBy(o => o.OgrenciNo).ToList() ?? new List<Ogrenci>();
            var atananDerslikler = _gosterilecekSinav?.AtananDerslikler?.OrderBy(d => d.DerslikKodu).ToList() ?? new List<Derslik>();

            int ogrenciIndex = 0;
            int toplamOgrenci = ogrenciler.Count;

        
            var derslikKapasiteleri = new Dictionary<Derslik, int>();
            foreach (var derslik in atananDerslikler)
            {
                if (derslik != null)
                {
                    derslikKapasiteleri[derslik] = derslik.Kapasite;
                }
            }

           
            bool yerlesimDevam = true;
            int derslikIndex = 0;

            while (yerlesimDevam && ogrenciIndex < ogrenciler.Count)
            {
                yerlesimDevam = false;

                for (int i = 0; i < atananDerslikler.Count; i++)
                {
                    if (ogrenciIndex >= ogrenciler.Count) break;

                    var derslik = atananDerslikler[derslikIndex];
                    if (derslik == null) continue;

                   
                    int buDersliktekiMevcutOgrenci = hesaplananListe.Count(o => o.Derslik?.DerslikID == derslik.DerslikID);
                    int derslikKapasite = derslikKapasiteleri[derslik];

                    if (buDersliktekiMevcutOgrenci < derslikKapasite)
                    {
                     
                        var ogrenci = ogrenciler[ogrenciIndex];

                     
                        var bosYer = BosYeriBul(derslik, hesaplananListe);

                        if (bosYer.satir > 0 && bosYer.sutun > 0)
                        {
                            hesaplananListe.Add(new OturmaPlaniOgrenciDetay
                            {
                                Ogrenci = ogrenci,
                                Derslik = derslik,
                                Satir = bosYer.satir,
                                Sutun = bosYer.sutun
                            });

                            ogrenciIndex++;
                            yerlesimDevam = true;
                        }
                    }

                    derslikIndex = (derslikIndex + 1) % atananDerslikler.Count;
                }
            }

            _tumYerlesimListesi = hesaplananListe;
        }

       
        private (int satir, int sutun) BosYeriBul(Derslik derslik, List<OturmaPlaniOgrenciDetay> mevcutYerlesim)
        {
           
            var dersliktekiDoluYerler = mevcutYerlesim
                .Where(o => o.Derslik?.DerslikID == derslik.DerslikID)
                .Select(o => new { o.Satir, o.Sutun })
                .ToList();

            int toplamSutunSayisi = derslik.EnineSiraSayisi * derslik.SiraYapisi;
            int toplamSatirSayisi = derslik.BoyunaSiraSayisi;

            for (int satir = 1; satir <= toplamSatirSayisi; satir++)
            {
                for (int sutun = 1; sutun <= toplamSutunSayisi; sutun++)
                {
                  
                    bool pozisyonDolu = dersliktekiDoluYerler.Any(y => y.Satir == satir && y.Sutun == sutun);

                    if (!pozisyonDolu)
                    {
                        return (satir, sutun);
                    }
                }
            }

            return (0, 0); 
        }


        private async void SecilenDerslikDegisti(Derslik secilenDerslik)
        {
            Application.Current.Dispatcher.Invoke(() => GorselOturmaPlani.Clear());
            if (secilenDerslik == null || _tumYerlesimListesi == null) return;

            try
            {
                var gorselPlan = await Task.Run(() => OturmaPlaniOlusturGorsel(secilenDerslik));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    GorselOturmaPlani.Clear();
                    if (gorselPlan != null)
                    {
                        foreach (var slot in gorselPlan) GorselOturmaPlani.Add(slot);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Oturma planı oluşturulurken hata: {ex.Message}", "Hata");
                Application.Current.Dispatcher.Invoke(() => GorselOturmaPlani.Clear());
            }
        }

        private ObservableCollection<OturmaPlaniOgrenciDetay> OturmaPlaniOlusturGorsel(Derslik secilenDerslik)
        {
            var derslikPlaniGorsel = new ObservableCollection<OturmaPlaniOgrenciDetay>();

            if (secilenDerslik == null) return derslikPlaniGorsel;

            // Bu dersliğe ait öğrencileri getir
            var buDersliktekiOgrenciler = _tumYerlesimListesi
                .Where(o => o.Derslik?.DerslikID == secilenDerslik.DerslikID)
                .ToList();

            // GÖRSEL İÇİN DOĞRU SATIR/SÜTUN HESAPLAMA
            // Örnek: 7 sütun x 9 satır grid oluştur

            int toplamSutunSayisi = secilenDerslik.EnineSiraSayisi * secilenDerslik.SiraYapisi;
            int toplamSatirSayisi = secilenDerslik.BoyunaSiraSayisi;

            // Tüm grid pozisyonlarını oluştur
            for (int satir = 1; satir <= toplamSatirSayisi; satir++)
            {
                for (int sutun = 1; sutun <= toplamSutunSayisi; sutun++)
                {
                    // Bu pozisyonda oturan öğrenciyi bul
                    var ogrenciDetay = buDersliktekiOgrenciler
                        .FirstOrDefault(o => o.Satir == satir && o.Sutun == sutun);

                    derslikPlaniGorsel.Add(new OturmaPlaniOgrenciDetay
                    {
                        Ogrenci = ogrenciDetay?.Ogrenci,
                        Derslik = secilenDerslik,
                        Satir = satir,
                        Sutun = sutun
                    });
                }
            }

            return derslikPlaniGorsel;
        }

      
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}