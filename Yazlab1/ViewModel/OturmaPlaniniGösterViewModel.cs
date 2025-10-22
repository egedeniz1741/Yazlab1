using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Yazlab1.Model;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Documents;

namespace Yazlab1.ViewModel
{
    public class SinifSatiri
    {
        public int SatirNumarasi { get; set; }
        public List<List<KoltukGorunum>> SirayaGoreKoltuklar { get; set; } = new List<List<KoltukGorunum>>();
    }

    public class KoltukGorunum
    {
        public OturmaPlaniOgrenciDetay OturmaDetay { get; set; }
        public int Satir { get; set; }
        public int Sira { get; set; }
        public int KoltukIndex { get; set; }

       
        public BitmapImage KoltukResim
        {
            get
            {
               
                    string resimAdi = OturmaDetay?.Ogrenci != null ? "dolu_koltuk.png" : "bos_koltuk.png";
                    return new BitmapImage(new Uri($"/images/{resimAdi}", UriKind.Relative));
               
            }
        }

        public string OgrenciAdSoyad => OturmaDetay?.Ogrenci?.AdSoyad ?? "Boş";
        public string OgrenciNo => OturmaDetay?.Ogrenci?.OgrenciNo ?? "";

     
        public string PozisyonText => $"S{Satir}-K{Sira}.{KoltukIndex}";

        public string ToolTipText => OturmaDetay?.Ogrenci != null ?
            $"{OturmaDetay.Ogrenci.AdSoyad}\n{OturmaDetay.Ogrenci.OgrenciNo}\nSıra: {Satir} - Sıra: {Sira} - Koltuk: {KoltukIndex}" :
            $"Boş koltuk\nSıra: {Satir} - Sıra: {Sira} - Koltuk: {KoltukIndex}";
    }
    public class OturmaPlaniGosterViewModel : INotifyPropertyChanged
    {
        private Derslik _secilenDerslik;
        private readonly AtanmisSinav _gosterilecekSinav;
        private List<OturmaPlaniOgrenciDetay> _tumYerlesimListesi;
        public ObservableCollection<SinifSatiri> SinifSatirlari { get; private set; }
        public string SinifDuzeniText => SecilenDerslik != null ?
            $"{SecilenDerslik.EnineSiraSayisi} satır × {SecilenDerslik.BoyunaSiraSayisi} sütun × {SecilenDerslik.SiraYapisi} kişilik" :
            "";
        public string PencereBasligi { get; }
        public ObservableCollection<Derslik> DerslikListesi { get; set; }
        public ObservableCollection<OturmaPlaniOgrenciDetay> GorselOturmaPlani { get; private set; }
        public ICommand PdfIndir { get; }

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
            SinifSatirlari = new ObservableCollection<SinifSatiri>();

            DerslikListesi = new ObservableCollection<Derslik>(siraliDerslikler);
            GorselOturmaPlani = new ObservableCollection<OturmaPlaniOgrenciDetay>();
            _tumYerlesimListesi = new List<OturmaPlaniOgrenciDetay>();

            PdfIndir = new RelayCommand(YazdirmayaGonder, CanPdfOlustur);

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

            int toplamSutunSayisi = derslik.EnineSiraSayisi;
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

        private ObservableCollection<SinifSatiri> GercekSinifDuzeniOlustur(Derslik secilenDerslik)
        {
            var sinifSatirlari = new ObservableCollection<SinifSatiri>();
            var buDersliktekiOgrenciler = _tumYerlesimListesi
                .Where(o => o.Derslik?.DerslikID == secilenDerslik.DerslikID)
                .OrderBy(o => o.Ogrenci?.OgrenciNo)
                .ToList();

            int ogrenciIndex = 0;
            int toplamOgrenci = buDersliktekiOgrenciler.Count;

            // DÜZELTME: EnineSiraSayisi = yatay sıra, BoyunaSiraSayisi = dikey sıra
            for (int yataySira = 1; yataySira <= secilenDerslik.EnineSiraSayisi; yataySira++)
            {
                var sinifSatiri = new SinifSatiri { SatirNumarasi = yataySira };

                for (int dikeySira = 1; dikeySira <= secilenDerslik.BoyunaSiraSayisi; dikeySira++)
                {
                    var siraKoltuklari = new List<KoltukGorunum>();

                    for (int koltuk = 1; koltuk <= secilenDerslik.SiraYapisi; koltuk++)
                    {
                        bool buKoltukDoluMu = KoltukDagilimKontrolu(secilenDerslik.SiraYapisi, koltuk, dikeySira, yataySira);
                        OturmaPlaniOgrenciDetay ogrenciDetay = null;

                        if (buKoltukDoluMu && ogrenciIndex < toplamOgrenci)
                        {
                            ogrenciDetay = buDersliktekiOgrenciler[ogrenciIndex];
                            ogrenciIndex++;
                        }

                        siraKoltuklari.Add(new KoltukGorunum
                        {
                            OturmaDetay = ogrenciDetay,
                            Satir = yataySira,        // Yatay sıra numarası
                            Sira = dikeySira,         // Dikey sıra numarası  
                            KoltukIndex = koltuk
                        });
                    }

                    sinifSatiri.SirayaGoreKoltuklar.Add(siraKoltuklari);
                }

                sinifSatirlari.Add(sinifSatiri);
            }

          
            return sinifSatirlari;
        }


        private bool KoltukDagilimKontrolu(int siraYapisi, int koltukIndex, int sutunNumarasi, int satirumarasi)
        {

            if (siraYapisi == 1)
                return true;

            if (siraYapisi == 2)
            {
                // Çapraz dağılım: her satırda farklı koltuk dolu
                return (sutunNumarasi % 2 == 1) ? (koltukIndex == 1) : (koltukIndex == 2);
            }

            if (siraYapisi == 3)
            {
                // Kenarlar dolu, ortası boş - satırlara göre değişken
                return (sutunNumarasi % 2 == 1) ? (koltukIndex != 2) : (koltukIndex == 2);
            }

            if (siraYapisi == 4)
            {
               
                    return koltukIndex == 1 || koltukIndex == 4;
               
            }

            // 5+ için genel çözüm
            return (koltukIndex % 2 == 1) && (sutunNumarasi % 2 == 1) ||
                   (koltukIndex % 2 == 0) && (sutunNumarasi % 2 == 0);
        }


        private async void SecilenDerslikDegisti(Derslik secilenDerslik)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                GorselOturmaPlani.Clear();
                SinifSatirlari.Clear();
            });

            if (secilenDerslik == null || _tumYerlesimListesi == null) return;

            try
            {
                var sinifDuzeni = await Task.Run(() => GercekSinifDuzeniOlustur(secilenDerslik));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SinifSatirlari.Clear();
                    foreach (var satir in sinifDuzeni)
                        SinifSatirlari.Add(satir);

                    OnPropertyChanged(nameof(SinifDuzeniText));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sınıf düzeni oluşturulurken hata: {ex.Message}", "Hata");
            }
        }

        

        private bool CanPdfOlustur()
        {
            return _tumYerlesimListesi != null && _tumYerlesimListesi.Any();
        }

        private void YazdirmayaGonder()
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // Tüm derslikler için belge oluştur
                    var flowDoc = TumDersliklerIcinDokumanOlustur();

                    // Sayfa ayarları
                    flowDoc.PageHeight = printDialog.PrintableAreaHeight;
                    flowDoc.PageWidth = printDialog.PrintableAreaWidth;

                    // Yazdır veya PDF'e kaydet
                    IDocumentPaginatorSource idpSource = flowDoc;
                    printDialog.PrintDocument(idpSource.DocumentPaginator, "Oturma Planı");

                    MessageBox.Show("Oturma planı yazdırılıyor/kaydediliyor!\n\nNot: Microsoft Print to PDF seçeneğini kullanarak PDF olarak kaydedebilirsiniz.",
                        "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazdırma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument TumDersliklerIcinDokumanOlustur()
        {
            var flowDoc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                PagePadding = new Thickness(40),
                ColumnWidth = double.PositiveInfinity
            };

            // Başlık
            var baslik = new Paragraph(new Run("SINAV OTURMA PLANI"))
            {
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            flowDoc.Blocks.Add(baslik);

            var dersAdi = new Paragraph(new Run(_gosterilecekSinav.SinavDetay?.Ders?.DersAdi ?? ""))
            {
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            };
            flowDoc.Blocks.Add(dersAdi);

            var tarih = new Paragraph(new Run($"Tarih: {_gosterilecekSinav.Tarih:dd.MM.yyyy} | Saat: {_gosterilecekSinav.BaslangicSaati}"))
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            };
            flowDoc.Blocks.Add(tarih);

            // Her derslik için
            var derslikler = _gosterilecekSinav.AtananDerslikler?.OrderBy(d => d.DerslikKodu).ToList() ?? new List<Derslik>();

            foreach (var derslik in derslikler)
            {
                if (derslik == null) continue;

                // Derslik başlığı
                var derslikBaslik = new Paragraph(new Run($"📍 {derslik.DerslikAdi}"))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 15, 0, 10),
                    Foreground = new SolidColorBrush(Color.FromRgb(43, 76, 126))
                };
                flowDoc.Blocks.Add(derslikBaslik);

                // Tablo oluştur
                var table = DerslikTablosuOlustur(derslik);
                flowDoc.Blocks.Add(table);

                // Son derslik değilse sayfa ayırıcı
                if (derslik != derslikler.Last())
                {
                    var ayirici = new Paragraph(new Run(""))
                    {
                        BreakPageBefore = true
                    };
                    flowDoc.Blocks.Add(ayirici);
                }
            }

            return flowDoc;
        }

        private System.Windows.Documents.Table DerslikTablosuOlustur(Derslik derslik)
        {
            // Gerçek sınıf düzenini oluştur
            var sinifDuzeni = GercekSinifDuzeniOlustur(derslik);

            var table = new System.Windows.Documents.Table
            {
                CellSpacing = 2,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1)
            };

            // Sütun sayısı: SiraYapisi + 1 (başlık sütunu için)
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(80) }); // Başlık
            for (int i = 0; i < derslik.SiraYapisi; i++)
            {
                table.Columns.Add(new System.Windows.Documents.TableColumn());
            }

            var rowGroup = new System.Windows.Documents.TableRowGroup();

            // Her yatay sıra için
            foreach (var sinifSatiri in sinifDuzeni)
            {
                // Her dikey sıra için AYRI BİR TABLO SATIRI oluştur
                for (int dikeySiraIndex = 0; dikeySiraIndex < sinifSatiri.SirayaGoreKoltuklar.Count; dikeySiraIndex++)
                {
                    var tableRow = new System.Windows.Documents.TableRow();
                    var siraKoltuklari = sinifSatiri.SirayaGoreKoltuklar[dikeySiraIndex];

                    // İlk sütun: Sıra bilgisi
                    var baslikCell = new System.Windows.Documents.TableCell(
                        new Paragraph(new Run($"Sıra {sinifSatiri.SatirNumarasi}-{dikeySiraIndex + 1}"))
                        {
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(2)
                        })
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(43, 76, 126)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(5),
                        Background = new SolidColorBrush(Color.FromRgb(220, 230, 245))
                    };
                    tableRow.Cells.Add(baslikCell);

                    // Koltuklar
                    foreach (var koltuk in siraKoltuklari)
                    {
                        string icerik = "";
                        Brush arkaplan = Brushes.White;
                        Brush kenarlık = new SolidColorBrush(Color.FromRgb(197, 210, 224));

                        if (koltuk.OturmaDetay?.Ogrenci != null)
                        {
                            icerik = $"{koltuk.OturmaDetay.Ogrenci.AdSoyad}\n({koltuk.OturmaDetay.Ogrenci.OgrenciNo})";
                            arkaplan = new SolidColorBrush(Color.FromRgb(245, 248, 255));
                            kenarlık = new SolidColorBrush(Color.FromRgb(100, 150, 200));
                        }
                        else
                        {
                            icerik = "BOŞ";
                            arkaplan = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                        }

                        var cellPara = new Paragraph(new Run(icerik))
                        {
                            FontSize = 9,
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(2),
                            LineHeight = 1
                        };

                        var cell = new System.Windows.Documents.TableCell(cellPara)
                        {
                            BorderBrush = kenarlık,
                            BorderThickness = new Thickness(1),
                            Padding = new Thickness(4),
                            Background = arkaplan
                        };

                        tableRow.Cells.Add(cell);
                    }

                    rowGroup.Rows.Add(tableRow);
                }

                // Her yatay sıra sonrası boşluk satırı (opsiyonel - görsel ayırım için)
                if (sinifSatiri != sinifDuzeni.Last())
                {
                    var ayiriciRow = new System.Windows.Documents.TableRow { Background = Brushes.LightGray };
                    var ayiriciCell = new System.Windows.Documents.TableCell(
                        new Paragraph(new Run("")) { Margin = new Thickness(0), FontSize = 4 })
                    {
                        ColumnSpan = derslik.SiraYapisi + 1,
                        Padding = new Thickness(0)
                    };
                    ayiriciRow.Cells.Add(ayiriciCell);
                    rowGroup.Rows.Add(ayiriciRow);
                }
            }

            table.RowGroups.Add(rowGroup);
            return table;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;
            public event EventHandler CanExecuteChanged;

            public RelayCommand(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
            public void Execute(object parameter) => _execute();
            public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}