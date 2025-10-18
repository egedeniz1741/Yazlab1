using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
   

    

   

    public partial class SinavOturmaPlaniViewModel : ObservableObject
    {
        public ObservableCollection<AtanmisSinav> SinavListesi { get; set; }

        [ObservableProperty]
        private AtanmisSinav _secilenSinav;

        // Bu özellik artık Observable değil, çünkü koleksiyonun kendisini değil, içeriğini değiştiriyoruz.
        public ObservableCollection<DerslikOturmaPlani> GorselOturmaPlaniListesi { get; set; }

        [ObservableProperty]
        private List<OturmaPlaniOgrenciDetay> _ogrenciYerlesimListesiPdf;

        private readonly string _sinavAdiBasligi;

        public SinavOturmaPlaniViewModel(List<AtanmisSinav> tumSinavlar, string sinavAdiBasligi)
        {
            _sinavAdiBasligi = sinavAdiBasligi;
            SinavListesi = new ObservableCollection<AtanmisSinav>(tumSinavlar.OrderBy(s => s.Tarih).ThenBy(s => s.BaslangicSaati));
            GorselOturmaPlaniListesi = new ObservableCollection<DerslikOturmaPlani>();
            OgrenciYerlesimListesiPdf = new List<OturmaPlaniOgrenciDetay>();
        }

        async partial void OnSecilenSinavChanged(AtanmisSinav value)
        {
            // Seçim değiştiğinde önceki planı temizle
            GorselOturmaPlaniListesi.Clear(); // Koleksiyonu yeniden oluşturmak yerine içini temizle
            OgrenciYerlesimListesiPdf.Clear();

            if (value != null)
            {
                // İşlemi arkaplanda yap ama UI güncellemesini Dispatcher ile yapacağımızdan emin ol
                await Task.Run(() => OturmaPlaniOlustur(value));
            }
            // PDF butonu durumunu güncellemek için bildirim gönder
            PdfAktarCommand.NotifyCanExecuteChanged();
        }

        private void OturmaPlaniOlustur(AtanmisSinav secilenSinav)
        {
            var pdfListesi = new List<OturmaPlaniOgrenciDetay>();
            var ogrenciler = secilenSinav.SinavDetay.Ogrenciler.OrderBy(o => o.OgrenciNo).ToList();
            int ogrenciIndex = 0;

            // Geçici bir liste oluşturalım, UI koleksiyonunu doğrudan arkaplanda değiştirmeyelim
            var geciciGorselPlanlar = new List<DerslikOturmaPlani>();

            foreach (var derslik in secilenSinav.AtananDerslikler.OrderBy(d => d.DerslikKodu))
            {
                var derslikPlaniGorsel = new ObservableCollection<OturmaPlaniOgrenciDetay>();
                int slotSayisi = derslik.BoyunaSiraSayisi * derslik.EnineSiraSayisi;

                for (int slotIndex = 0; slotIndex < slotSayisi; slotIndex++)
                {
                    var slotDetayGorsel = new OturmaPlaniOgrenciDetay { Derslik = derslik };
                    bool slotDolu = false;

                    for (int i = 0; i < derslik.SiraYapisi; i++)
                    {
                        if (ogrenciIndex < ogrenciler.Count)
                        {
                            var ogrenci = ogrenciler[ogrenciIndex];
                            int satir = (slotIndex / derslik.EnineSiraSayisi) + 1;
                            int sutun = (slotIndex % derslik.EnineSiraSayisi) * derslik.SiraYapisi + i + 1;

                            var pdfDetay = new OturmaPlaniOgrenciDetay { Ogrenci = ogrenci, Derslik = derslik, Satir = satir, Sutun = sutun };
                            pdfListesi.Add(pdfDetay);

                            if (!slotDolu) { slotDetayGorsel.Ogrenci = ogrenci; slotDolu = true; }
                            ogrenciIndex++;
                        }
                    }
                    derslikPlaniGorsel.Add(slotDetayGorsel);
                }
                geciciGorselPlanlar.Add(new DerslikOturmaPlani { Derslik = derslik, Plan = derslikPlaniGorsel });
            }

            // UI güncellemeleri ana thread'de topluca yapılmalı
            Application.Current.Dispatcher.Invoke(() =>
            {
                OgrenciYerlesimListesiPdf = pdfListesi;

                GorselOturmaPlaniListesi.Clear(); // Önce temizle
                foreach (var plan in geciciGorselPlanlar)
                {
                    GorselOturmaPlaniListesi.Add(plan); // Sonra tek tek ekle
                }

                // GorselOturmaPlaniListesi observable olmadığı için manuel bildirim GEREKMEZ,
                // çünkü koleksiyonun içeriği değiştiğinde TabControl otomatik güncellenir.
                // Sadece PDF listesi için bildirim gönderelim (CanExecute için).
                OnPropertyChanged(nameof(OgrenciYerlesimListesiPdf));
                PdfAktarCommand.NotifyCanExecuteChanged();
            });
        }

        // PDF Aktar metodu aynı kalıyor
        [RelayCommand(CanExecute = nameof(CanPdfAktar))]
        private void PdfAktar() { /* ...içerik aynı... */ }

        private bool CanPdfAktar()
        {
            return SecilenSinav != null && OgrenciYerlesimListesiPdf != null && OgrenciYerlesimListesiPdf.Any();
        }
    }
}