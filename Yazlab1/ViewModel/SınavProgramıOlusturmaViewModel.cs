using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
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
    #region Yardımcı Sınıflar
    public class SinavDetay
    {
        public Ders Ders { get; set; }
        public List<Ogrenci> Ogrenciler { get; set; } = new List<Ogrenci>();
        public int OgrenciSayisi => Ogrenciler.Count;
    }

    public class AtanmisSinav
    {
        public SinavDetay SinavDetay { get; set; }
        public DateTime Tarih { get; set; }
        public TimeSpan BaslangicSaati { get; set; }
        public List<Derslik> AtananDerslikler { get; set; } = new List<Derslik>();

        // YENİ EKLENEN ÖZELLİK: Derslik kodlarını birleştirilmiş olarak döndürür
        public string DerslikKodlariString => AtananDerslikler != null && AtananDerslikler.Any()
                                              ? string.Join("-", AtananDerslikler.Select(d => d.DerslikKodu))
                                              : string.Empty;
    }

    public class ExcelRaporSatiri
    {
        public string Tarih { get; set; }
        public string SinavSaati { get; set; }
        public string DersAdi { get; set; }
        public string OgretimElemani { get; set; }
        public string Derslik { get; set; }
    }
    #endregion

    public partial class SinavProgramiOlusturmaViewModel : ObservableObject
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private readonly int _aktifKullaniciBolumId;
        private readonly string _aktifKullaniciBolumAdi;

        [ObservableProperty] private DateTime _sinavBaslangicTarihi = DateTime.Today;
        [ObservableProperty] private DateTime _sinavBitisTarihi = DateTime.Today.AddDays(14);
        [ObservableProperty] private string _sinavTuru = "Vize";
        [ObservableProperty] private int _sinavBaslangicSaati = 9;
        [ObservableProperty] private int _sinavBitisSaati = 17;
        [ObservableProperty] private int _varsayilanSinavSuresi = 75;
        [ObservableProperty] private int _varsayilanBeklemeSuresi = 15;
        [ObservableProperty] private bool _isBusy = false;

        public ObservableCollection<DersSecici> DahilEdilecekDersler { get; set; }
        public ObservableCollection<bool> Gunler { get; set; }

        public SinavProgramiOlusturmaViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;
            _aktifKullaniciBolumAdi = aktifKullanici.Bolum.BolumAdi;

            if (_aktifKullaniciBolumId == 0)
            {
                _aktifKullaniciBolumId = 1;
            }

            DahilEdilecekDersler = new ObservableCollection<DersSecici>();
            Gunler = new ObservableCollection<bool> { true, true, true, true, true, false, false };
            DersleriYukle();
        }
        private async void DersleriYukle()
        {
            await Task.Run(() =>
            {
                var geciciDersListesi = new List<DersSecici>();
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DersID, DersKodu, DersAdi FROM Dersler WHERE BolumID = @BolumID ORDER BY DersKodu";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var ders = new Ders { DersID = reader.GetInt32("DersID"), DersKodu = reader.GetString("DersKodu"), DersAdi = reader.GetString("DersAdi") };
                                geciciDersListesi.Add(new DersSecici(ders));
                            }
                        }
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DahilEdilecekDersler.Clear();
                    foreach (var ders in geciciDersListesi)
                    {
                        DahilEdilecekDersler.Add(ders);
                    }
                });
            });
        }

        [RelayCommand]
        private async Task ProgramiOlustur()
        {
            IsBusy = true;
            try
            {
                // ... (Veri toplama kısmı aynı) ...
                var secilenDersler = DahilEdilecekDersler.Where(d => d.IsSelected).Select(d => d.Ders).ToList();
                if (!secilenDersler.Any())
                {
                    MessageBox.Show("Lütfen sınava dahil edilecek en az bir ders seçin.", "Uyarı");
                    return;
                }
                List<SinavDetay> sinavlar = await VerileriTopla(secilenDersler);
                List<Derslik> derslikler = await DerslikleriGetir();
                if (sinavlar == null || derslikler == null) return;

                var log = new StringBuilder();
                List<AtanmisSinav> takvim = await Task.Run(() => AlgoritmayiCalistir(sinavlar, derslikler, log));

                if (takvim != null && takvim.Any())
                {
                    MessageBox.Show($"Sınav programı başarıyla oluşturuldu! Şimdi Excel raporu oluşturulacak ve oturma planı gösterilecek.", "Başarılı");

                    // 1. ADIM: Excel Raporunu Oluştur (Geri Eklendi)
                    await ExcelRaporuOlustur(takvim);

                    // 2. ADIM: Oturma Planı Penceresini Aç
                    string sinavAdiBasligi = $"{_aktifKullaniciBolumAdi.ToUpper()} BÖLÜMÜ {_sinavTuru.ToUpper()} SINAV PROGRAMI";
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SinavOturmaPlaniWindow oturmaPlaniListesiWindow = new SinavOturmaPlaniWindow(takvim, sinavAdiBasligi);
                        oturmaPlaniListesiWindow.Show();
                    });
                }
                else
                {
                    MessageBox.Show(log.ToString(), "Algoritma Raporu");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        #region Excel Raporu Oluşturma
        private async Task ExcelRaporuOlustur(List<AtanmisSinav> takvim)
        {
            try
            {
                var raporVerisi = new List<ExcelRaporSatiri>();
                var ogretimElemanlari = await OgretimElemanlariniGetir();

                foreach (var sinav in takvim.OrderBy(s => s.Tarih).ThenBy(s => s.BaslangicSaati))
                {
                    raporVerisi.Add(new ExcelRaporSatiri
                    {
                        Tarih = sinav.Tarih.ToString("dd.MM.yyyy"),
                        SinavSaati = sinav.BaslangicSaati.ToString(@"hh\:mm"),
                        DersAdi = sinav.SinavDetay.Ders.DersAdi,
                        OgretimElemani = ogretimElemanlari.ContainsKey(sinav.SinavDetay.Ders.DersID) ? ogretimElemanlari[sinav.SinavDetay.Ders.DersID] : "N/A",
                        Derslik = string.Join("-", sinav.AtananDerslikler.Select(d => d.DerslikKodu))
                    });
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Sınav Programı");

                    // DEĞİŞİKLİK BURADA: Excel başlığına bölüm adı eklendi
                    worksheet.Cell("A1").Value = $"{_aktifKullaniciBolumAdi.ToUpper()} BÖLÜMÜ {_sinavTuru.ToUpper()} SINAV PROGRAMI";

                    worksheet.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    var basliklar = new List<string> { "Tarih", "Sınav Saati", "Ders Adı", "Öğretim Elemanı", "Derslik" };
                    worksheet.Cell("A3").InsertData(new[] { basliklar });
                    worksheet.Range("A3:E3").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

                    if (raporVerisi.Any())
                    {
                        worksheet.Cell("A4").InsertTable(raporVerisi, false);
                    }

                    worksheet.Columns().AdjustToContents();

                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Excel Workbook|*.xlsx",
                        Title = "Sınav Programını Kaydet",
                        FileName = $"Sinav_Programi_{DateTime.Now:yyyy-MM-dd}.xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        workbook.SaveAs(saveFileDialog.FileName);
                        MessageBox.Show($"Sınav programı başarıyla '{saveFileDialog.FileName}' olarak kaydedildi.", "Başarılı");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel raporu oluşturulurken bir hata oluştu: {ex.Message}", "Raporlama Hatası");
            }
        }
        #endregion

        #region Algoritma Mantığı
        private List<AtanmisSinav> AlgoritmayiCalistir(List<SinavDetay> sinavlar, List<Derslik> derslikler, StringBuilder log)
        {
            var takvim = new List<AtanmisSinav>();
            var ogrenciProgrami = new Dictionary<int, List<AtanmisSinav>>();
            var derslikProgrami = new Dictionary<DateTime, Dictionary<TimeSpan, List<Derslik>>>();
            var siraliSinavlar = sinavlar.OrderByDescending(s => s.OgrenciSayisi).ToList();

            if (siraliSinavlar.Any())
            {
                int enKalabalikSinav = siraliSinavlar.First().OgrenciSayisi;
                int toplamKapasite = derslikler.Sum(d => d.Kapasite);
                if (enKalabalikSinav > toplamKapasite)
                {
                    log.AppendLine("Algoritma Başarısız!");
                    log.AppendLine($"En kalabalık sınav ({siraliSinavlar.First().Ders.DersKodu}) {enKalabalikSinav} kişidir.");
                    log.AppendLine($"Ancak tüm dersliklerin toplam kapasitesi sadece {toplamKapasite}.");
                    log.AppendLine("Lütfen derslik ekleyin veya kapasiteleri artırın.");
                    return null;
                }
            }

            foreach (var sinav in siraliSinavlar)
            {
                bool yerlestirildi = false;
                for (var gun = _sinavBaslangicTarihi.Date; gun <= _sinavBitisTarihi.Date; gun = gun.AddDays(1))
                {
                    int dayIndex = (int)gun.DayOfWeek == 0 ? 6 : (int)gun.DayOfWeek - 1;
                    if (!Gunler[dayIndex]) continue;

                    for (var saat = new TimeSpan(SinavBaslangicSaati, 0, 0); saat <= new TimeSpan(SinavBitisSaati, 0, 0); saat = saat.Add(TimeSpan.FromMinutes(_varsayilanSinavSuresi + _varsayilanBeklemeSuresi)))
                    {
                        bool ogrenciCakismasiVar = sinav.Ogrenciler.Any(ogrenci =>
                            ogrenciProgrami.TryGetValue(ogrenci.OgrenciID, out var ogrencininSinavlari) &&
                            ogrencininSinavlari.Any(s => s.Tarih.Date == gun.Date && s.BaslangicSaati == saat));

                        if (ogrenciCakismasiVar) continue;

                        var doluDerslikler = derslikProgrami.ContainsKey(gun) && derslikProgrami[gun].ContainsKey(saat) ? derslikProgrami[gun][saat] : new List<Derslik>();
                        var doluDerslikIdleri = doluDerslikler.Select(d => d.DerslikID).ToHashSet();
                        var bosDerslikler = derslikler.Where(d => !doluDerslikIdleri.Contains(d.DerslikID)).OrderBy(d => d.Kapasite).ToList();

                        var uygunDerslikler = new List<Derslik>();
                        var kalanOgrenci = sinav.OgrenciSayisi;
                        foreach (var derslik in bosDerslikler)
                        {
                            if (kalanOgrenci > 0)
                            {
                                uygunDerslikler.Add(derslik);
                                kalanOgrenci -= derslik.Kapasite;
                            }
                        }

                        if (kalanOgrenci <= 0)
                        {
                            var atama = new AtanmisSinav { SinavDetay = sinav, Tarih = gun, BaslangicSaati = saat, AtananDerslikler = uygunDerslikler };
                            takvim.Add(atama);
                            foreach (var ogrenci in sinav.Ogrenciler)
                            {
                                if (!ogrenciProgrami.ContainsKey(ogrenci.OgrenciID)) ogrenciProgrami[ogrenci.OgrenciID] = new List<AtanmisSinav>();
                                ogrenciProgrami[ogrenci.OgrenciID].Add(atama);
                            }
                            if (!derslikProgrami.ContainsKey(gun)) derslikProgrami[gun] = new Dictionary<TimeSpan, List<Derslik>>();
                            if (!derslikProgrami[gun].ContainsKey(saat)) derslikProgrami[gun][saat] = new List<Derslik>();
                            derslikProgrami[gun][saat].AddRange(uygunDerslikler);
                            yerlestirildi = true;
                            break;
                        }
                    }
                    if (yerlestirildi) break;
                }

                if (!yerlestirildi)
                {
                    log.AppendLine($"'({sinav.Ders.DersKodu}) {sinav.Ders.DersAdi}' dersinin sınavı yerleştirilemedi!");
                    log.AppendLine($"Gereken Kapasite: {sinav.OgrenciSayisi}");
                    log.AppendLine("\nOlası Nedenler:");
                    log.AppendLine("- Tarih aralığı çok kısa veya seçili gün/saat sayısı yetersiz.");
                    log.AppendLine("- Bu derse giren öğrencilerin diğer sınavları tüm zaman dilimlerini doldurmuş olabilir (çakışma).");
                    log.AppendLine("- Herhangi bir zaman diliminde bu kadar öğrenciyi alacak yeterli sayıda boş derslik bulunamadı.");
                    return null;
                }
            }

            if (!takvim.Any())
            {
                log.AppendLine("Hiçbir sınav yerleştirilemedi. Lütfen kısıtlarınızı kontrol edin.");
            }

            return takvim;
        }
        #endregion

        #region Veri Toplama Metotları
        private async Task<List<SinavDetay>> VerileriTopla(List<Ders> secilenDersler)
        {
            var sinavListesi = new List<SinavDetay>();
            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        foreach (var ders in secilenDersler)
                        {
                            var sinavDetay = new SinavDetay { Ders = ders };
                            string query = @"SELECT o.OgrenciID, o.OgrenciNo, o.AdSoyad FROM Ogrenciler o JOIN OgrenciDersKayitlari odk ON o.OgrenciID = odk.OgrenciID WHERE odk.DersID = @DersID";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@DersID", ders.DersID);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        sinavDetay.Ogrenciler.Add(new Ogrenci { OgrenciID = reader.GetInt32("OgrenciID"), OgrenciNo = reader.GetString("OgrenciNo"), AdSoyad = reader.GetString("AdSoyad") });
                                    }
                                }
                            }
                            sinavListesi.Add(sinavDetay);
                        }
                    }
                });
                return sinavListesi;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sınav verileri toplanırken bir hata oluştu: {ex.Message}", "Veritabanı Hatası");
                return null;
            }
        }

        private async Task<List<Derslik>> DerslikleriGetir()
        {
            var derslikListesi = new List<Derslik>();
            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "SELECT DerslikID, DerslikKodu, DerslikAdi, Kapasite FROM Derslikler WHERE BolumID = @BolumID";
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    derslikListesi.Add(new Derslik { DerslikID = reader.GetInt32("DerslikID"), DerslikKodu = reader.GetString("DerslikKodu"), DerslikAdi = reader.GetString("DerslikAdi"), Kapasite = reader.GetInt32("Kapasite") });
                                }
                            }
                        }
                    }
                });
                return derslikListesi;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Derslikler getirilirken bir hata oluştu: {ex.Message}", "Veritabanı Hatası");
                return null;
            }
        }

        private async Task<Dictionary<int, string>> OgretimElemanlariniGetir()
        {
            var dict = new Dictionary<int, string>();
            await Task.Run(() =>
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT d.DersID, ou.AdSoyad FROM Dersler d LEFT JOIN OgretimUyeleri ou ON d.OgretimUyesiID = ou.OgretimUyesiID WHERE d.BolumID = @BolumID";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("AdSoyad")))
                                {
                                    dict[reader.GetInt32("DersID")] = reader.GetString("AdSoyad");
                                }
                            }
                        }
                    }
                }
            });
            return dict;
        }
        #endregion
    }
}