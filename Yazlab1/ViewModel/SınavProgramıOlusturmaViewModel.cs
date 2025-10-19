using ClosedXML.Excel; // Excel için
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32; // SaveFileDialog için
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
// Models namespace'i
using Yazlab1.Views; // Views namespace'i (Pencere açmak için)

namespace Yazlab1.ViewModel
{
    #region Yardımcı Sınıflar
   
    public class SinavDetay
    {
        public Ders Ders { get; set; }
        public List<Ogrenci> Ogrenciler { get; set; } = new List<Ogrenci>();
        public int OgrenciSayisi => Ogrenciler.Count;
        public int SinavSuresi { get; set; } // Ders bazlı sınav süresi
    }

    /// <summary>
    /// Oluşturulan takvimdeki her bir sınavı temsil eder.
    /// </summary>
    public class AtanmisSinav
    {
        public SinavDetay SinavDetay { get; set; }
        public DateTime Tarih { get; set; }
        public TimeSpan BaslangicSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public List<Derslik> AtananDerslikler { get; set; } = new List<Derslik>();

        // Derslik kodlarını birleştirilmiş olarak döndüren özellik
        public string DerslikKodlariString => AtananDerslikler != null && AtananDerslikler.Any()
                                              ? string.Join("-", AtananDerslikler.Select(d => d.DerslikKodu))
                                              : string.Empty;
    }

    /// <summary>
    /// Excel raporundaki her bir satırı temsil eder.
    /// </summary>
    public class ExcelRaporSatiri
    {
        public string Tarih { get; set; }
        public string SinavSaati { get; set; }
        public string DersAdi { get; set; }
        public string OgretimElemani { get; set; }
        public string Derslik { get; set; }
    }
    #endregion

    /// <summary>
    /// Sınav Programı Oluşturma penceresinin tüm mantığını yöneten ViewModel.
    /// </summary>
    public partial class SinavProgramiOlusturmaViewModel : ObservableObject // partial olduğundan emin olun
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private readonly int _aktifKullaniciBolumId;
        private readonly string _aktifKullaniciBolumAdi;
        private List<AtanmisSinav> _sonOlusturulanTakvim; // Başarılı takvimi saklamak için

        // Arayüze bağlı kısıt özellikleri
        [ObservableProperty] private DateTime _sinavBaslangicTarihi = DateTime.Today;
        [ObservableProperty] private DateTime _sinavBitisTarihi = DateTime.Today.AddDays(14);
        [ObservableProperty] private string _sinavTuru = "Vize";
        [ObservableProperty] private int _sinavBaslangicSaati = 9;
        [ObservableProperty] private int _sinavBitisSaati = 17;
        [ObservableProperty] private int _varsayilanSinavSuresi = 75;
        [ObservableProperty] private int _varsayilanBeklemeSuresi = 15;
        [ObservableProperty] private bool _isBusy = false; // İşlem durumu

        // Oturma planı butonunun aktifliğini kontrol eden özellik
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OturmaPlanlamasinaGitCommand))] // Bu komutun CanExecute'ini güncelle
        private bool _isTakvimOlusturuldu = false;

        // Arayüzdeki ders listesi DataGrid'ine bağlı koleksiyon
        public ObservableCollection<DersSecici> DahilEdilecekDersler { get; set; }
        // Arayüzdeki gün CheckBox'larına bağlı koleksiyon
        public ObservableCollection<bool> Gunler { get; set; }

        public SinavProgramiOlusturmaViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici?.BolumID ?? throw new ArgumentNullException("Aktif kullanıcı BolumID'si boş olamaz.");
            _aktifKullaniciBolumAdi = aktifKullanici?.Bolum?.BolumAdi ?? "Bilinmeyen Bölüm";
            DahilEdilecekDersler = new ObservableCollection<DersSecici>();
            Gunler = new ObservableCollection<bool> { true, true, true, true, true, false, false }; // Pzt-Cuma seçili
            DersleriYukle();
        }

        /// <summary>
        /// Veritabanından bölümdeki tüm dersleri çeker ve arayüzdeki listeyi günceller.
        /// </summary>
        private async void DersleriYukle()
        {
            await Task.Run(() =>
            {
                var geciciDersListesi = new List<DersSecici>();
                int varsayilanSure = 75; // Varsayılan değer
                Application.Current.Dispatcher.Invoke(() => { varsayilanSure = VarsayilanSinavSuresi; });

                try
                {
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
                                    geciciDersListesi.Add(new DersSecici(ders, varsayilanSure)); // Varsayılan süre ile oluştur
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Dersler yüklenirken hata oluştu: {ex.Message}", "Hata"));
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DahilEdilecekDersler.Clear();
                    foreach (var ders in geciciDersListesi) DahilEdilecekDersler.Add(ders);
                });
            });
        }

        /// <summary>
        /// Sınav programı oluşturma algoritmasını başlatan komut.
        /// </summary>
        [RelayCommand]
        private async Task ProgramiOlustur()
        {
            IsBusy = true;
            IsTakvimOlusturuldu = false; // Başlangıçta butonu pasif yap
            _sonOlusturulanTakvim = null; // Önceki takvimi temizle
            try
            {
                var secilenDersSeciciler = DahilEdilecekDersler.Where(d => d.IsSelected).ToList();
                if (!secilenDersSeciciler.Any())
                {
                    MessageBox.Show("Lütfen sınava dahil edilecek en az bir ders seçin.", "Uyarı"); return;
                }
                if (SinavBaslangicSaati < 0 || SinavBaslangicSaati > 23 || SinavBitisSaati < 0 || SinavBitisSaati > 23 || SinavBaslangicSaati >= SinavBitisSaati)
                {
                    MessageBox.Show("Lütfen geçerli bir sınav başlangıç ve bitiş saati girin (0-23 arası, başlangıç < bitiş).", "Geçersiz Saat"); return;
                }

                List<SinavDetay> sinavlar = await VerileriTopla(secilenDersSeciciler);
                List<Derslik> derslikler = await DerslikleriGetir();
                if (sinavlar == null || derslikler == null) return;

                var log = new StringBuilder();
                List<AtanmisSinav> takvim = await Task.Run(() => AlgoritmayiCalistir(sinavlar, derslikler, log));

                if (takvim != null && takvim.Any())
                {
                    await ExcelRaporuOlustur(takvim); // Excel'i oluştur
                    _sonOlusturulanTakvim = takvim; // Takvimi sakla
                    IsTakvimOlusturuldu = true; // SADECE BAŞARILI OLUNCA true yap (Buton aktif olur)
                    MessageBox.Show($"Sınav programı başarıyla oluşturuldu ve Excel'e kaydedildi! Şimdi 'Oturma Planlaması' butonunu kullanabilirsiniz.", "Başarılı");
                }
                else
                {
                    MessageBox.Show(log.Length > 0 ? log.ToString() : "Uygun bir takvim bulunamadı. Kısıtları kontrol edin.", "Algoritma Raporu");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Program oluşturulurken beklenmedik hata: {ex.Message}", "Kritik Hata");
                IsTakvimOlusturuldu = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Oturma Planı penceresini açan komut.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanOturmaPlanlamasinaGit))]
        private void OturmaPlanlamasinaGit()
        {
            string sinavAdiBasligi = $"{_aktifKullaniciBolumAdi.ToUpper()} BÖLÜMÜ {_sinavTuru.ToUpper()} SINAV PROGRAMI";
            // SinavOturmaPlaniWindow'u aç (sınav listesi ekranı)
            SinavOturmaPlaniWindow oturmaPlaniListesiWindow = new SinavOturmaPlaniWindow(_sonOlusturulanTakvim, sinavAdiBasligi);
            oturmaPlaniListesiWindow.Show();
        }

        /// <summary>
        /// OturmaPlanlamasinaGit komutunun çalışıp çalışamayacağını belirler.
        /// </summary>
        private bool CanOturmaPlanlamasinaGit()
        {
            // IsTakvimOlusturuldu true ise buton aktif olur
            return IsTakvimOlusturuldu;
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
                        DersAdi = sinav.SinavDetay?.Ders?.DersAdi ?? "Bilinmiyor",
                        OgretimElemani = ogretimElemanlari.ContainsKey(sinav.SinavDetay?.Ders?.DersID ?? -1) ? ogretimElemanlari[sinav.SinavDetay.Ders.DersID] : "N/A",
                        Derslik = sinav.DerslikKodlariString
                    });
                }
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Sınav Programı");
                    worksheet.Cell("A1").Value = $"{_aktifKullaniciBolumAdi.ToUpper()} BÖLÜMÜ {_sinavTuru.ToUpper()} SINAV PROGRAMI";
                    worksheet.Range("A1:E1").Merge().Style.Font.SetBold().Font.SetFontSize(16).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    var basliklar = new List<string> { "Tarih", "Sınav Saati", "Ders Adı", "Öğretim Elemanı", "Derslik" };
                    worksheet.Cell("A3").InsertData(new[] { basliklar });
                    worksheet.Range("A3:E3").Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
                    if (raporVerisi.Any()) worksheet.Cell("A4").InsertTable(raporVerisi, false);
                    worksheet.Columns().AdjustToContents();
                    SaveFileDialog saveFileDialog = new SaveFileDialog
                    {
                        Filter = "Excel Workbook|*.xlsx",
                        Title = "Sınav Programını Kaydet",
                        FileName = $"Sinav_Programi_{_aktifKullaniciBolumAdi}_{_sinavTuru}_{DateTime.Now:yyyyMMdd}.xlsx"
                    };
                    bool? result = Application.Current.Dispatcher.Invoke(() => saveFileDialog.ShowDialog());
                    if (result == true)
                    {
                        workbook.SaveAs(saveFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex) { Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Excel raporu oluşturulurken hata: {ex.Message}", "Hata")); }
        }
        #endregion

        #region Algoritma Mantığı
        private List<AtanmisSinav> AlgoritmayiCalistir(List<SinavDetay> sinavlar, List<Derslik> derslikler, StringBuilder log)
        {
            var takvim = new List<AtanmisSinav>();
            var ogrenciProgrami = new Dictionary<int, List<AtanmisSinav>>();
            var derslikProgrami = new Dictionary<DateTime, Dictionary<TimeSpan, HashSet<int>>>();
            var siraliSinavlar = sinavlar.OrderByDescending(s => s.OgrenciSayisi).ToList();

            if (siraliSinavlar.Any())
            {
                int enKalabalikSinav = siraliSinavlar.First().OgrenciSayisi;
                int toplamKapasite = derslikler.Sum(d => d.Kapasite);
                if (enKalabalikSinav > toplamKapasite)
                {
                    log.AppendLine("Algoritma Başarısız!");
                    log.AppendLine($"En kalabalık sınav ({siraliSinavlar.First().Ders?.DersKodu ?? "N/A"}) {enKalabalikSinav} kişidir.");
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

                    int slotSuresi = _varsayilanSinavSuresi + _varsayilanBeklemeSuresi;

                    for (var saat = new TimeSpan(SinavBaslangicSaati, 0, 0); saat < new TimeSpan(SinavBitisSaati, 0, 0); saat = saat.Add(TimeSpan.FromMinutes(slotSuresi)))
                    {
                        bool ogrenciCakismasiVar = sinav.Ogrenciler.Any(ogrenci =>
                            ogrenciProgrami.TryGetValue(ogrenci.OgrenciID, out var ogrencininSinavlari) &&
                            ogrencininSinavlari.Any(s => s.Tarih.Date == gun.Date && s.BaslangicSaati == saat));
                        if (ogrenciCakismasiVar) continue;

                        var doluDerslikIdleri = derslikProgrami.ContainsKey(gun) && derslikProgrami[gun].ContainsKey(saat)
                                               ? derslikProgrami[gun][saat]
                                               : new HashSet<int>();
                        var bosDerslikler = derslikler.Where(d => !doluDerslikIdleri.Contains(d.DerslikID))
                                                      .OrderByDescending(d => d.Kapasite) // Büyükten küçüğe
                                                      .ToList();

                        var uygunDerslikler = new List<Derslik>();
                        var kalanOgrenci = sinav.OgrenciSayisi;
                        foreach (var derslik in bosDerslikler)
                        {
                            if (kalanOgrenci > 0)
                            {
                                uygunDerslikler.Add(derslik);
                                kalanOgrenci -= derslik.Kapasite;
                            }
                            else break;
                        }

                        if (kalanOgrenci <= 0 && uygunDerslikler.Any())
                        {
                            TimeSpan bitisSaati = saat.Add(TimeSpan.FromMinutes(sinav.SinavSuresi));
                            TimeSpan sonrakiSlotBaslangici = saat.Add(TimeSpan.FromMinutes(slotSuresi));
                            if (bitisSaati > sonrakiSlotBaslangici)
                            {
                                // Sınav bu slota sığmıyor
                                continue;
                            }

                            var atama = new AtanmisSinav { SinavDetay = sinav, Tarih = gun, BaslangicSaati = saat, BitisSaati = bitisSaati, AtananDerslikler = uygunDerslikler };
                            takvim.Add(atama);
                            foreach (var ogrenci in sinav.Ogrenciler)
                            {
                                if (!ogrenciProgrami.ContainsKey(ogrenci.OgrenciID)) ogrenciProgrami[ogrenci.OgrenciID] = new List<AtanmisSinav>();
                                ogrenciProgrami[ogrenci.OgrenciID].Add(atama);
                            }
                            if (!derslikProgrami.ContainsKey(gun)) derslikProgrami[gun] = new Dictionary<TimeSpan, HashSet<int>>();
                            if (!derslikProgrami[gun].ContainsKey(saat)) derslikProgrami[gun][saat] = new HashSet<int>();
                            foreach (var d in uygunDerslikler) derslikProgrami[gun][saat].Add(d.DerslikID);
                            yerlestirildi = true;
                            break;
                        }
                    }
                    if (yerlestirildi) break;
                }
                if (!yerlestirildi)
                {
                    log.AppendLine($"'({sinav.Ders?.DersKodu ?? "N/A"}) {sinav.Ders?.DersAdi ?? "Bilinmeyen"}' dersinin sınavı yerleştirilemedi!");
                    log.AppendLine($"Gereken Kapasite: {sinav.OgrenciSayisi}");
                    log.AppendLine("\nOlası Nedenler:");
                    log.AppendLine("- Tarih aralığı, seçili gün/saat sayısı veya derslik kapasitesi yetersiz.");
                    log.AppendLine("- Öğrenci çakışmaları nedeniyle uygun zaman dilimi bulunamadı.");
                    return null;
                }
            }
            if (!takvim.Any()) log.AppendLine("Hiçbir sınav yerleştirilemedi. Lütfen kısıtlarınızı kontrol edin.");
            return takvim;
        }
        #endregion

        #region Veri Toplama Metotları
        private async Task<List<SinavDetay>> VerileriTopla(List<DersSecici> secilenDersSeciciler)
        {
            var sinavListesi = new List<SinavDetay>();
            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        foreach (var dersSecici in secilenDersSeciciler)
                        {
                            var ders = dersSecici.Ders;
                            var sinavSuresi = dersSecici._sinavSuresi;
                            var sinavDetay = new SinavDetay { Ders = ders, SinavSuresi = sinavSuresi };
                            string query = @"SELECT o.OgrenciID, o.OgrenciNo, o.AdSoyad FROM Ogrenciler o JOIN OgrenciDersKayitlari odk ON o.OgrenciID = odk.OgrenciID WHERE odk.DersID = @DersID";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@DersID", ders?.DersID ?? -1);
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
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Sınav verileri toplanırken bir hata oluştu: {ex.Message}", "Veritabanı Hatası"));
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
                        string query = "SELECT DerslikID, DerslikKodu, DerslikAdi, Kapasite, EnineSiraSayisi, BoyunaSiraSayisi, SiraYapisi FROM Derslikler WHERE BolumID = @BolumID";
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    derslikListesi.Add(new Derslik
                                    {
                                        DerslikID = reader.GetInt32("DerslikID"),
                                        DerslikKodu = reader.GetString("DerslikKodu"),
                                        DerslikAdi = reader.GetString("DerslikAdi"),
                                        Kapasite = reader.GetInt32("Kapasite"),
                                        EnineSiraSayisi = reader.GetInt32("EnineSiraSayisi"),
                                        BoyunaSiraSayisi = reader.GetInt32("BoyunaSiraSayisi"),
                                        SiraYapisi = reader.GetInt32("SiraYapisi")
                                    });
                                }
                            }
                        }
                    }
                });
                return derslikListesi;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Derslikler getirilirken bir hata oluştu: {ex.Message}", "Veritabanı Hatası"));
                return null;
            }
        }

        private async Task<Dictionary<int, string>> OgretimElemanlariniGetir()
        {
            var dict = new Dictionary<int, string>();
            try
            {
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
                                    dict[reader.GetInt32("DersID")] = reader.IsDBNull(reader.GetOrdinal("AdSoyad")) ? "Atanmamış" : reader.GetString("AdSoyad");
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() => MessageBox.Show($"Öğretim elemanları getirilirken hata: {ex.Message}", "Veritabanı Hatası"));
            }
            return dict;
        }
        #endregion
    }
}