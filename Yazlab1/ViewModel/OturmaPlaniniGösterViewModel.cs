using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
    public class OturmaPlaniGosterViewModel : INotifyPropertyChanged
    {
        // === Alanlar ===
        private Derslik _secilenDerslik;
        private readonly AtanmisSinav _gosterilecekSinav;
        private List<OturmaPlaniOgrenciDetay> _tumYerlesimListesi; // Tüm öğrencilerin yerleşimini tutar

        // === Özellikler ===
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
                    SecilenDerslikDegisti(value); // Görsel planı güncelle
                }
            }
        }

        // === Constructor ===


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

            // Asenkron başlatma
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // 🔹 Asenkron hesaplamayı bekliyoruz
            await Task.Run(() => TumOturmaPlaniniHesapla());

            // 🔹 Artık liste kesin dolu — UI'ye güncelleme gönderiyoruz
            Application.Current.Dispatcher.Invoke(() =>
            {
                SecilenDerslik = DerslikListesi.FirstOrDefault();
            });
        }


        // === Metotlar ===

        // Tüm sınav için öğrencilerin hangi derslikte, hangi satır/sütunda olduğunu hesaplar
        private void TumOturmaPlaniniHesapla()
        {
            var hesaplananListe = new List<OturmaPlaniOgrenciDetay>();
            var ogrenciler = _gosterilecekSinav?.SinavDetay?.Ogrenciler?.OrderBy(o => o.OgrenciNo).ToList() ?? new List<Ogrenci>();
            var atananDerslikler = _gosterilecekSinav?.AtananDerslikler?.OrderBy(d => d.DerslikKodu).ToList() ?? new List<Derslik>();
            int ogrenciIndex = 0;

            foreach (var derslik in atananDerslikler)
            {
                if (derslik == null || derslik.BoyunaSiraSayisi <= 0 || derslik.EnineSiraSayisi <= 0)
                    continue;

                int derslikKapasitesi = derslik.Kapasite;
                int slotSayisi = derslik.BoyunaSiraSayisi * derslik.EnineSiraSayisi;
                int siraYapisi = derslik.SiraYapisi > 0 ? derslik.SiraYapisi : 1;
                int mevcutOgrenciSayisi = 0;

                for (int slotIndex = 0; slotIndex < slotSayisi; slotIndex++)
                {
                    for (int i = 0; i < siraYapisi; i++)
                    {
                        if (ogrenciIndex >= ogrenciler.Count)
                            break; // öğrenciler bitti

                        if (mevcutOgrenciSayisi >= derslikKapasitesi)
                            break; // derslik doldu

                        var ogrenci = ogrenciler[ogrenciIndex];
                        int satir = (slotIndex / derslik.EnineSiraSayisi) + 1;
                        int sutun = (slotIndex % derslik.EnineSiraSayisi) * siraYapisi + i + 1;

                        hesaplananListe.Add(new OturmaPlaniOgrenciDetay
                        {
                            Ogrenci = ogrenci,
                            Derslik = derslik,
                            Satir = satir,
                            Sutun = sutun
                        });

                        ogrenciIndex++;
                        mevcutOgrenciSayisi++;
                    }

                    // 🔹 İçteki döngü kırıldıysa, kontrol et
                    if (ogrenciIndex >= ogrenciler.Count || mevcutOgrenciSayisi >= derslikKapasitesi)
                        break; // sadece bu dersliği bırak, sonraki dersliğe geç
                }

                if (ogrenciIndex >= ogrenciler.Count)
                    break; // tüm öğrenciler yerleşti
            }

            _tumYerlesimListesi = hesaplananListe;
        }




        // Seçilen Derslik Değiştiğinde Çalışan Metot
        private async void SecilenDerslikDegisti(Derslik secilenDerslik)
        {
            Application.Current.Dispatcher.Invoke(() => GorselOturmaPlani.Clear());
            if (secilenDerslik == null || _tumYerlesimListesi == null) return;

            try
            {
                // DÜZELTME: Doğru metot adı 'OturmaPlaniOlusturGorsel' ve 'Derslik' parametresi alıyor
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

        // Seçilen Derslik İçin SADECE GÖRSEL Oturma Planını Hazırlayan Metot
        // DÜZELTME: Metot adı 'OturmaPlaniOlusturGorsel' ve 'Derslik' parametresi alıyor
        private ObservableCollection<OturmaPlaniOgrenciDetay> OturmaPlaniOlusturGorsel(Derslik secilenDerslik)
        {
            var derslikPlaniGorsel = new ObservableCollection<OturmaPlaniOgrenciDetay>();
            if (secilenDerslik == null || secilenDerslik.BoyunaSiraSayisi <= 0 || secilenDerslik.EnineSiraSayisi <= 0)
                return derslikPlaniGorsel;

            int slotSayisi = secilenDerslik.BoyunaSiraSayisi * secilenDerslik.EnineSiraSayisi;
            int siraYapisi = secilenDerslik.SiraYapisi > 0 ? secilenDerslik.SiraYapisi : 1;

            for (int slotIndex = 0; slotIndex < slotSayisi; slotIndex++)
            {
                int gorselSatir = (slotIndex / secilenDerslik.EnineSiraSayisi) + 1;
                int gorselSutun = (slotIndex % secilenDerslik.EnineSiraSayisi) + 1;

                var oturanIlkOgrenciDetay = _tumYerlesimListesi
                    .FirstOrDefault(o => o.Derslik.DerslikID == secilenDerslik.DerslikID &&
                                         o.Satir == gorselSatir &&
                                         o.Sutun == gorselSutun);

                var slotDetayGorsel = new OturmaPlaniOgrenciDetay
                {
                    Ogrenci = oturanIlkOgrenciDetay?.Ogrenci,
                    Satir = oturanIlkOgrenciDetay?.Satir ?? gorselSatir, // Bulunduysa öğrencinin satırını ata
                    Sutun = oturanIlkOgrenciDetay?.Sutun ?? gorselSutun // Bulunduysa öğrencinin sütununu ata
                };
                derslikPlaniGorsel.Add(slotDetayGorsel);
            }
           
            return derslikPlaniGorsel;
            
        }

        // === INotifyPropertyChanged Implementasyonu ===
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}