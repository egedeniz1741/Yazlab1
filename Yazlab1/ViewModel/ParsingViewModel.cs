using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
    public partial class ParsingViewModel : ObservableObject
    {
        private readonly int _aktifKullaniciBolumId;
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [ObservableProperty] private bool _isBusy = false;
        [ObservableProperty] private string _statusMessage;

        public ParsingViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;

            if (_aktifKullaniciBolumId == 0)
            {
                _aktifKullaniciBolumId = 1;
            }
        }

        #region Ders Yükleme
        [RelayCommand]
        private async Task UploadDerslerExcel()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Dosyaları|*.xlsx;*.xls" };
            if (ofd.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "Bu işlem, mevcut tüm dersleri ve derslere bağlı öğrenci kayıtlarını silecek ve yerine bu Excel dosyasındakileri ekleyecektir. Devam etmek istediğinizden emin misiniz?",
                    "Uyarı: Veriler Silinecek",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No) return;

                IsBusy = true;
                StatusMessage = "Mevcut dersler siliniyor ve yenileri işleniyor...";
                try
                {
                    await Task.Run(() => ParseAndInsertDersListesi(ofd.FileName));
                    StatusMessage = "Ders listesi başarıyla güncellendi!";
                }
                catch (Exception ex) { StatusMessage = $"Hata: {ex.Message}"; }
                finally { IsBusy = false; }
            }
        }

        private void ParseAndInsertDersListesi(string filePath)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string deleteOdkQuery = "DELETE odk FROM OgrenciDersKayitlari odk JOIN Dersler d ON odk.DersID = d.DersID WHERE d.BolumID = @BolumID";
                using (var cmd = new MySqlCommand(deleteOdkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                    cmd.ExecuteNonQuery();
                }

                string deleteDerslerQuery = "DELETE FROM Dersler WHERE BolumID = @BolumID";
                using (var cmd = new MySqlCommand(deleteDerslerQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                    cmd.ExecuteNonQuery();
                }

                var mevcutOgretimUyeleri = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                string ogretimUyeleriQuery = "SELECT AdSoyad, OgretimUyesiID FROM OgretimUyeleri";
                using (var cmd = new MySqlCommand(ogretimUyeleriQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) mevcutOgretimUyeleri[reader.GetString("AdSoyad")] = reader.GetInt32("OgretimUyesiID");
                }

                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);
                    foreach (var row in rows)
                    {
                        string dersKodu = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(dersKodu) || dersKodu.Contains("DERS KOD")) continue;
                        string dersAdi = row.Cell(2).GetString().Trim();
                        if (dersAdi.Contains("DERSİN ADI")) continue;
                        string ogretimUyesi = row.Cell(3).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(ogretimUyesi) || ogretimUyesi.Contains("ELEMANI") || ogretimUyesi.Length < 3) continue;

                        int ogretimUyesiID = GetOrInsertOgretimUyesi(conn, ogretimUyesi, mevcutOgretimUyeleri);

                        string insertDers = "INSERT INTO Dersler (BolumID, OgretimUyesiID, DersKodu, DersAdi) VALUES (@BolumID, @OgretimUyesiID, @DersKodu, @DersAdi)";
                        using (var cmd = new MySqlCommand(insertDers, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            cmd.Parameters.AddWithValue("@OgretimUyesiID", ogretimUyesiID);
                            cmd.Parameters.AddWithValue("@DersKodu", dersKodu);
                            cmd.Parameters.AddWithValue("@DersAdi", dersAdi);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private int GetOrInsertOgretimUyesi(MySqlConnection conn, string adSoyad, Dictionary<string, int> mevcutOgretimUyeleri)
        {
            if (mevcutOgretimUyeleri.TryGetValue(adSoyad, out int id)) return id;

            string insertQuery = "INSERT INTO OgretimUyeleri (AdSoyad) VALUES (@AdSoyad)";
            using (var insertCmd = new MySqlCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@AdSoyad", adSoyad);
                insertCmd.ExecuteNonQuery();
                int newId = (int)insertCmd.LastInsertedId;
                mevcutOgretimUyeleri.Add(adSoyad, newId);
                return newId;
            }
        }
        #endregion

        #region Öğrenci Yükleme
        [RelayCommand]
        private async Task UploadOgrencilerExcel()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Dosyaları|*.xlsx;*.xls" };
            if (ofd.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "Bu işlem, bu bölüme ait mevcut tüm öğrencileri ve ders kayıtlarını silecek (Varsa) ve yerine bu Excel dosyasındakileri ekleyecektir. Devam etmek istediğinizden emin misiniz?",
                    "Uyarı: Veriler Silinecek",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No) return;

                IsBusy = true;
                StatusMessage = "Mevcut öğrenciler siliniyor ve yenileri işleniyor...";
                try
                {
                    await Task.Run(() => ParseAndInsertOgrenciler(ofd.FileName));
                    StatusMessage = "Öğrenci verileri başarıyla güncellendi!";
                }
                catch (Exception ex) { StatusMessage = $"Hata: {ex.Message}"; }
                finally { IsBusy = false; }
            }
        }

        private void ParseAndInsertOgrenciler(string filePath)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string deleteOdkQuery = "DELETE odk FROM OgrenciDersKayitlari odk JOIN Ogrenciler o ON odk.OgrenciID = o.OgrenciID WHERE o.BolumID = @BolumID";
                using (var cmd = new MySqlCommand(deleteOdkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                    cmd.ExecuteNonQuery();
                }
                string deleteOgrenciQuery = "DELETE FROM Ogrenciler WHERE BolumID = @BolumID";
                using (var cmd = new MySqlCommand(deleteOgrenciQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                    cmd.ExecuteNonQuery();
                }

                var mevcutDersler = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                string derslerQuery = "SELECT DersKodu, DersID FROM Dersler WHERE BolumID = @BolumID";
                using (var cmd = new MySqlCommand(derslerQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) mevcutDersler[reader.GetString("DersKodu")] = reader.GetInt32("DersID");
                    }
                }

                var ogrenciIdMap = new Dictionary<string, int>();

                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        string ogrenciNo = row.Cell(1).GetString().Trim();
                        string adSoyad = row.Cell(2).GetString().Trim();
                        string sinifStr = row.Cell(3).GetString().Trim();
                        string dersKodu = row.Cell(4).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(ogrenciNo) || string.IsNullOrWhiteSpace(adSoyad) || string.IsNullOrWhiteSpace(dersKodu)) continue;

                        if (!ogrenciIdMap.TryGetValue(ogrenciNo, out int ogrenciID))
                        {
                            string insertQuery = "INSERT INTO Ogrenciler (OgrenciNo, AdSoyad, Sinif, BolumID) VALUES (@OgrenciNo, @AdSoyad, @Sinif, @BolumID)";
                            using (var insertCmd = new MySqlCommand(insertQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@OgrenciNo", ogrenciNo);
                                insertCmd.Parameters.AddWithValue("@AdSoyad", adSoyad);
                                insertCmd.Parameters.AddWithValue("@Sinif", ParseSinif(sinifStr));
                                insertCmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                                insertCmd.ExecuteNonQuery();
                                ogrenciID = (int)insertCmd.LastInsertedId;
                                ogrenciIdMap.Add(ogrenciNo, ogrenciID);
                            }
                        }

                        if (mevcutDersler.TryGetValue(dersKodu, out int dersID))
                        {
                            string insertOdkQuery = "INSERT IGNORE INTO OgrenciDersKayitlari (OgrenciID, DersID) VALUES (@OgrenciID, @DersID)";
                            using (var insertCmd = new MySqlCommand(insertOdkQuery, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@OgrenciID", ogrenciID);
                                insertCmd.Parameters.AddWithValue("@DersID", dersID);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        private int ParseSinif(string sinif)
        {
            if (string.IsNullOrWhiteSpace(sinif)) return 0;
            string[] parts = sinif.Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int sinifNo)) return sinifNo;
            return 0;
        }
        #endregion

        #region Veri Temizleme Komutları
        [RelayCommand]
        private async Task TumDersleriSil()
        {
            var result = MessageBox.Show(
                "Bu işlem, bu bölüme ait mevcut tüm dersleri ve bu derslere bağlı tüm öğrenci kayıtlarını kalıcı olarak silecektir. Devam etmek istediğinizden emin misiniz?",
                "Uyarı: Tüm Dersler Silinecek",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            IsBusy = true;
            StatusMessage = "Mevcut dersler ve ilgili kayıtlar siliniyor...";
            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string deleteOdkQuery = "DELETE odk FROM OgrenciDersKayitlari odk JOIN Dersler d ON odk.DersID = d.DersID WHERE d.BolumID = @BolumID";
                        using (var cmd = new MySqlCommand(deleteOdkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            cmd.ExecuteNonQuery();
                        }
                        string deleteDerslerQuery = "DELETE FROM Dersler WHERE BolumID = @BolumID";
                        using (var cmd = new MySqlCommand(deleteDerslerQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
                StatusMessage = "Tüm dersler ve ilgili kayıtlar başarıyla silindi.";
            }
            catch (Exception ex) { StatusMessage = $"Hata: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task TumOgrencileriSil()
        {
            var result = MessageBox.Show(
                "Bu işlem, bu bölüme ait mevcut tüm öğrencileri ve bu öğrencilere bağlı tüm ders kayıtlarını kalıcı olarak silecektir. Devam etmek istediğinizden emin misiniz?",
                "Uyarı: Tüm Öğrenciler Silinecek",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            IsBusy = true;
            StatusMessage = "Mevcut öğrenciler ve ilgili kayıtlar siliniyor...";
            try
            {
                await Task.Run(() =>
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string deleteOdkQuery = "DELETE odk FROM OgrenciDersKayitlari odk JOIN Ogrenciler o ON odk.OgrenciID = o.OgrenciID WHERE o.BolumID = @BolumID";
                        using (var cmd = new MySqlCommand(deleteOdkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            cmd.ExecuteNonQuery();
                        }
                        string deleteOgrenciQuery = "DELETE FROM Ogrenciler WHERE BolumID = @BolumID";
                        using (var cmd = new MySqlCommand(deleteOgrenciQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                });
                StatusMessage = "Tüm öğrenciler ve ilgili kayıtlar başarıyla silindi.";
            }
            catch (Exception ex) { StatusMessage = $"Hata: {ex.Message}"; }
            finally { IsBusy = false; }
        }
        #endregion
    }
}