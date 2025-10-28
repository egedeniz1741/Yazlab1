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
        public int SinavSuresi { get; set; }
    }

    public class AtanmisSinav
    {
        public SinavDetay SinavDetay { get; set; }
        public DateTime Tarih { get; set; }
        public TimeSpan BaslangicSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }
        public List<Derslik> AtananDerslikler { get; set; } = new List<Derslik>();

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
        private List<AtanmisSinav> _sonOlusturulanTakvim;

        [ObservableProperty] private DateTime _sinavBaslangicTarihi = DateTime.Today;
        [ObservableProperty] private DateTime _sinavBitisTarihi = DateTime.Today.AddDays(14);
        [ObservableProperty] private string _sinavTuru = "Vize";
        [ObservableProperty] private int _sinavBaslangicSaati = 9;
        [ObservableProperty] private int _sinavBitisSaati = 17;
        [ObservableProperty] private int _varsayilanSinavSuresi = 75;
        [ObservableProperty] private int _varsayilanBeklemeSuresi = 15;
        [ObservableProperty] private bool _isBusy = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(OturmaPlanlamasinaGitCommand))]
        private bool _isTakvimOlusturuldu = false;

        public ObservableCollection<DersSecici> DahilEdilecekDersler { get; set; }
        public ObservableCollection<bool> Gunler { get; set; }

        public SinavProgramiOlusturmaViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici?.BolumID ?? throw new ArgumentNullException("Aktif kullanıcı BolumID'si boş olamaz.");
            _aktifKullaniciBolumAdi = aktifKullanici?.Bolum?.BolumAdi ?? "Bilinmeyen Bölüm";
            DahilEdilecekDersler = new ObservableCollection<DersSecici>();
            Gunler = new ObservableCollection<bool> { true, true, true, true, true, false, false };
            DersleriYukle();
        }

        private async void DersleriYukle()
        {
            await Task.Run(() =>
            {
                var geciciDersListesi = new List<DersSecici>();
                int varsayilanSure = 75;
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
                                    geciciDersListesi.Add(new DersSecici(ders, varsayilanSure));
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

        [RelayCommand]
        private async Task ProgramiOlustur()
        {
            IsBusy = true;
            IsTakvimOlusturuldu = false;
            _sonOlusturulanTakvim = null;

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
                    await ExcelRaporuOlustur(takvim);
                    _sonOlusturulanTakvim = takvim;
                    IsTakvimOlusturuldu = true;
                    MessageBox.Show($"Sınav programı başarıyla oluşturuldu ve Excel'e kaydedildi!\n\n{log}", "Başarılı");
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

        [RelayCommand(CanExecute = nameof(CanOturmaPlanlamasinaGit))]
        private void OturmaPlanlamasinaGit()
        {
            string sinavAdiBasligi = $"{_aktifKullaniciBolumAdi.ToUpper()} BÖLÜMÜ {_sinavTuru.ToUpper()} SINAV PROGRAMI";
            SinavOturmaPlaniWindow oturmaPlaniListesiWindow = new SinavOturmaPlaniWindow(_sonOlusturulanTakvim, sinavAdiBasligi);
            oturmaPlaniListesiWindow.Show();
        }

        private bool CanOturmaPlanlamasinaGit()
        {
            return IsTakvimOlusturuldu;
        }

        #region Algoritma Mantığı
        private List<AtanmisSinav> AlgoritmayiCalistir(List<SinavDetay> sinavlar, List<Derslik> derslikler, StringBuilder log)
        {
            var takvim = new List<AtanmisSinav>();
            var ogrenciProgrami = new Dictionary<int, List<AtanmisSinav>>();
            var derslikProgrami = new Dictionary<DateTime, Dictionary<TimeSpan, HashSet<int>>>();
            var derslikKullanımSayisi = new Dictionary<int, int>();
            var sinifGunlukSinav = new Dictionary<DateTime, Dictionary<int, int>>();

            foreach (var derslik in derslikler)
            {
                derslikKullanımSayisi[derslik.DerslikID] = 0;
            }

            var siraliSinavlar = sinavlar.OrderByDescending(s => s.OgrenciSayisi).ToList();

            foreach (var sinav in siraliSinavlar)
            {
                bool yerlestirildi = false;
                int dersSinifi = DersKodundanSinifGetir(sinav.Ders?.DersKodu);

                for (var gun = SinavBaslangicTarihi.Date; gun <= SinavBitisTarihi.Date; gun = gun.AddDays(1))
                {
                    int dayIndex = (int)gun.DayOfWeek == 0 ? 6 : (int)gun.DayOfWeek - 1;
                    if (!Gunler[dayIndex]) continue;

                
                    if (!SinifIcinGunlukSinavKontrolu(dersSinifi, gun, sinifGunlukSinav))
                        continue;
                    int slotAraligi = sinav.SinavSuresi + VarsayilanBeklemeSuresi;

                    for (var saat = new TimeSpan(SinavBaslangicSaati, 0, 0);
                         saat <= new TimeSpan(SinavBitisSaati - 1, 0, 0);
                         saat = saat.Add(TimeSpan.FromMinutes(slotAraligi)))
                    {
                        TimeSpan bitisSaati = saat.Add(TimeSpan.FromMinutes(sinav.SinavSuresi+VarsayilanBeklemeSuresi));
                        if (bitisSaati > new TimeSpan(SinavBitisSaati, 0, 0))
                            continue;

                        bool ogrenciCakismasiVar = sinav.Ogrenciler.Any(ogrenci =>
                            ogrenciProgrami.TryGetValue(ogrenci.OgrenciID, out var ogrencininSinavlari) &&
                            ogrencininSinavlari.Any(s => s.Tarih.Date == gun.Date &&
                                                       ((saat >= s.BaslangicSaati && saat < s.BitisSaati) ||
                                                        (bitisSaati > s.BaslangicSaati && bitisSaati <= s.BitisSaati) ||
                                                        (saat <= s.BaslangicSaati && bitisSaati >= s.BitisSaati))));
                        if (ogrenciCakismasiVar) continue;

                        bool beklemeSuresiUygun = true;
                        foreach (var ogrenci in sinav.Ogrenciler)
                        {
                            if (ogrenciProgrami.TryGetValue(ogrenci.OgrenciID, out var ogrencininSinavlari))
                            {
                                foreach (var oncekiSinav in ogrencininSinavlari)
                                {
                                    if (oncekiSinav.Tarih.Date == gun.Date)
                                    {
                                        TimeSpan fark = saat - oncekiSinav.BitisSaati;
                                        if (fark.TotalMinutes < VarsayilanBeklemeSuresi && fark.TotalMinutes > 0)
                                        {
                                            beklemeSuresiUygun = false;
                                            break;
                                        }
                                        TimeSpan sonrakiFark = oncekiSinav.BaslangicSaati - bitisSaati;
                                        if (sonrakiFark.TotalMinutes < VarsayilanBeklemeSuresi && sonrakiFark.TotalMinutes > 0)
                                        {
                                            beklemeSuresiUygun = false;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (!beklemeSuresiUygun) break;
                        }
                        if (!beklemeSuresiUygun) continue;

                        var uygunDerslikler = UygunDerslikleriBul(derslikler, derslikProgrami, gun, saat, bitisSaati, derslikKullanımSayisi, sinav.OgrenciSayisi);

                        if (uygunDerslikler.Any())
                        {
                            var atama = new AtanmisSinav
                            {
                                SinavDetay = sinav,
                                Tarih = gun,
                                BaslangicSaati = saat,
                                BitisSaati = bitisSaati,
                                AtananDerslikler = uygunDerslikler
                            };
                            takvim.Add(atama);

                            foreach (var ogrenci in sinav.Ogrenciler)
                            {
                                if (!ogrenciProgrami.ContainsKey(ogrenci.OgrenciID))
                                    ogrenciProgrami[ogrenci.OgrenciID] = new List<AtanmisSinav>();
                                ogrenciProgrami[ogrenci.OgrenciID].Add(atama);
                            }

                          
                            if (!sinifGunlukSinav.ContainsKey(gun.Date))
                                sinifGunlukSinav[gun.Date] = new Dictionary<int, int>();
                            if (!sinifGunlukSinav[gun.Date].ContainsKey(dersSinifi))
                                sinifGunlukSinav[gun.Date][dersSinifi] = 0;
                            sinifGunlukSinav[gun.Date][dersSinifi]++;

                            for (var derslikSaat = saat; derslikSaat < bitisSaati; derslikSaat = derslikSaat.Add(TimeSpan.FromMinutes(30)))
                            {
                                if (!derslikProgrami.ContainsKey(gun))
                                    derslikProgrami[gun] = new Dictionary<TimeSpan, HashSet<int>>();
                                if (!derslikProgrami[gun].ContainsKey(derslikSaat))
                                    derslikProgrami[gun][derslikSaat] = new HashSet<int>();

                                foreach (var d in uygunDerslikler)
                                {
                                    derslikProgrami[gun][derslikSaat].Add(d.DerslikID);
                                    derslikKullanımSayisi[d.DerslikID]++;
                                }
                            }

                            yerlestirildi = true;
                            break;
                        }
                    }
                    if (yerlestirildi) break;
                }

               
            }

           

        
          

            return takvim;
        }

        private bool SinifIcinGunlukSinavKontrolu(int sinif, DateTime tarih, Dictionary<DateTime, Dictionary<int, int>> sinifGunlukSinav)
        {
            if (!sinifGunlukSinav.ContainsKey(tarih.Date))
                sinifGunlukSinav[tarih.Date] = new Dictionary<int, int>();

            var gununSiniflari = sinifGunlukSinav[tarih.Date];

            // Sınıf için günlük maksimum sınav sayısı
            int maksimumSinav = 2;

            if (gununSiniflari.ContainsKey(sinif) && gununSiniflari[sinif] >= maksimumSinav)
                return false;

            return true;
        }

        private int DersKodundanSinifGetir(string dersKodu)
        {
            if (string.IsNullOrEmpty(dersKodu))
                return 0;

            var dersKoduTemiz = dersKodu.ToUpper().Trim();
            var sayisalKisim = new string(dersKoduTemiz.Where(char.IsDigit).ToArray());

            if (sayisalKisim.Length >= 3)
            {
                int dersKoduSayi = int.Parse(sayisalKisim);
                int sinif = dersKoduSayi / 100;

                if (sinif >= 1 && sinif <= 4)
                    return sinif;
            }

            return 0;
        }

        private List<Derslik> UygunDerslikleriBul(List<Derslik> derslikler,
                                                 Dictionary<DateTime, Dictionary<TimeSpan, HashSet<int>>> derslikProgrami,
                                                 DateTime gun, TimeSpan baslangic, TimeSpan bitis,
                                                 Dictionary<int, int> derslikKullanımSayisi, int gerekenOgrenciSayisi)
        {
            var uygunDerslikler = new List<Derslik>();
            var kalanOgrenci = gerekenOgrenciSayisi;

            var siraliDerslikler = derslikler.OrderBy(d => derslikKullanımSayisi[d.DerslikID])
                                            .ThenByDescending(d => d.Kapasite)
                                            .ToList();

            foreach (var derslik in siraliDerslikler)
            {
                bool derslikUygun = true;
                for (var kontrolSaati = baslangic; kontrolSaati < bitis; kontrolSaati = kontrolSaati.Add(TimeSpan.FromMinutes(30)))
                {
                    if (derslikProgrami.ContainsKey(gun) &&
                        derslikProgrami[gun].ContainsKey(kontrolSaati) &&
                        derslikProgrami[gun][kontrolSaati].Contains(derslik.DerslikID))
                    {
                        derslikUygun = false;
                        break;
                    }
                }

                if (derslikUygun && kalanOgrenci > 0)
                {
                    uygunDerslikler.Add(derslik);
                    kalanOgrenci -= derslik.Kapasite;
                }

                if (kalanOgrenci <= 0) break;
            }

            return kalanOgrenci <= 0 ? uygunDerslikler : new List<Derslik>();
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
                            var sinavSuresi = dersSecici.SinavSuresi;
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