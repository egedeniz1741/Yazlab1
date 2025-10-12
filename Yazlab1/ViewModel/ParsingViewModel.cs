using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Models;
using ClosedXML.Excel;
using System.Configuration;
using MySql.Data.MySqlClient;

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
        }

        #region Ders Yükleme Komutu
        [RelayCommand]
        private async Task UploadDerslerExcel()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Dosyaları|*.xlsx;*.xls" };
            if (ofd.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Ders listesi işleniyor...";
                try
                {
                   
                    await Task.Run(() => ParseAndInsertDersListesi(ofd.FileName));
                    StatusMessage = "Ders Listesi verileri başarıyla eklendi!";
                }
                catch (Exception ex) { StatusMessage = $"Hata: {ex.Message}"; }
                finally { IsBusy = false; }
            }
        }
        #endregion

        #region Öğrenci Yükleme Komutu
        [RelayCommand]
        private async Task UploadOgrencilerExcel()
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Dosyaları|*.xlsx;*.xls" };
            if (ofd.ShowDialog() == true)
            {
                IsBusy = true;
                StatusMessage = "Öğrenci listesi işleniyor...";
                try
                {
                   
                    await Task.Run(() => ParseAndInsertOgrenciler(ofd.FileName));
                    StatusMessage = "Öğrenci verileri başarıyla eklendi!";
                }
                catch (Exception ex) { StatusMessage = $"Hata: {ex.Message}"; }
                finally { IsBusy = false; }
            }
        }
        #endregion

       
        #region Sizin Ders Listesi Metotlarınız
        private void ParseAndInsertDersListesi(string filePath)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    foreach (var row in rows.Skip(1))
                    {
                        string dersKodu = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(dersKodu) || dersKodu.Contains("DERS KODU") || dersKodu.Contains("DERS KOD") || dersKodu.Contains("Sınıf") || dersKodu.Contains("SINIF") || dersKodu.Contains("SEÇMELİ") || dersKodu.Contains("SEÇİMLİK")) { continue; }
                        string dersAdi = row.Cell(2).GetString().Trim();
                        if (dersAdi.Contains("DERSİN ADI") || (dersAdi.Contains("DERS") && dersAdi.Contains("ADI")) || dersAdi.Contains("SEÇMELİ") || dersAdi.Contains("SEÇİMLİK")) { continue; }
                        string ogretimUyesi = row.Cell(3).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(ogretimUyesi) || ogretimUyesi.Contains("ELEMANI") || ogretimUyesi.Contains("ÖĞR.") || ogretimUyesi.Contains("DERSİ VEREN") || ogretimUyesi.Contains("DERS VEREN") || ogretimUyesi.Length < 3) { continue; }

                        int ogretimUyesiID = GetOrInsertOgretimUyesi(conn, ogretimUyesi);
                        if (DersExists(conn, dersKodu, _aktifKullaniciBolumId)) { continue; } 

                        string insertDers = @"INSERT INTO Dersler (BolumID, OgretimUyesiID, DersKodu, DersAdi, Sinif, DersYapisi) VALUES (@BolumID, @OgretimUyesiID, @DersKodu, @DersAdi, @Sinif, @DersYapisi)"; 
                        using (var cmd = new MySqlCommand(insertDers, conn))
                        {
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId); 
                            cmd.Parameters.AddWithValue("@OgretimUyesiID", ogretimUyesiID);
                            cmd.Parameters.AddWithValue("@DersKodu", dersKodu);
                            cmd.Parameters.AddWithValue("@DersAdi", dersAdi);
                            cmd.Parameters.AddWithValue("@Sinif", DBNull.Value);
                            cmd.Parameters.AddWithValue("@DersYapisi", DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        private bool DersExists(MySqlConnection conn, string dersKodu, int bolumID)
        {
            string checkQuery = "SELECT COUNT(*) FROM Dersler WHERE DersKodu = @DersKodu AND BolumID = @BolumID";
            using (var cmd = new MySqlCommand(checkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@DersKodu", dersKodu);
                cmd.Parameters.AddWithValue("@BolumID", bolumID);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }
        private int GetOrInsertOgretimUyesi(MySqlConnection conn, string adSoyad)
        {
            string checkQuery = "SELECT OgretimUyesiID FROM OgretimUyeleri WHERE AdSoyad = @AdSoyad";
            using (var checkCmd = new MySqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@AdSoyad", adSoyad);
                object result = checkCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value) return Convert.ToInt32(result);
            }
            string insertQuery = "INSERT INTO OgretimUyeleri (AdSoyad) VALUES (@AdSoyad)";
            using (var insertCmd = new MySqlCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@AdSoyad", adSoyad);
                insertCmd.ExecuteNonQuery();
                return (int)insertCmd.LastInsertedId;
            }
        }
        #endregion

        #region 
        private void ParseAndInsertOgrenciler(string filePath)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    foreach (var row in rows.Skip(1))
                    {
                        string ogrenciNo = row.Cell(1).GetString().Trim();
                        string adSoyad = row.Cell(2).GetString().Trim();
                        string sinif = row.Cell(3).GetString().Trim();
                        string dersKodu = row.Cell(4).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(ogrenciNo) || string.IsNullOrWhiteSpace(adSoyad) || string.IsNullOrWhiteSpace(dersKodu)) { continue; }

                        int sinifNumara = ParseSinif(sinif);
                        int ogrenciID = GetOrInsertOgrenci(conn, ogrenciNo, adSoyad, sinifNumara);
                        int? dersID = GetDersID(conn, dersKodu);
                        if (dersID.HasValue)
                        {
                            InsertOgrenciDersKaydi(conn, ogrenciID, dersID.Value);
                        }
                    }
                }
            }
        }
        private int ParseSinif(string sinif)
        {
            if (string.IsNullOrWhiteSpace(sinif)) return 0;
            string[] parts = sinif.Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int sinifNo)) { return sinifNo; }
            return 0;
        }
        private int GetOrInsertOgrenci(MySqlConnection conn, string ogrenciNo, string adSoyad, int sinif)
        {
            string checkQuery = "SELECT OgrenciID FROM Ogrenciler WHERE OgrenciNo = @OgrenciNo";
            using (var checkCmd = new MySqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@OgrenciNo", ogrenciNo);
                object result = checkCmd.ExecuteScalar();
                if (result != null && result != DBNull.Value) return Convert.ToInt32(result);
            }
            string insertQuery = @"INSERT INTO Ogrenciler (OgrenciNo, AdSoyad, Sinif, BolumID) VALUES (@OgrenciNo, @AdSoyad, @Sinif, @BolumID)";
            using (var insertCmd = new MySqlCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@OgrenciNo", ogrenciNo);
                insertCmd.Parameters.AddWithValue("@AdSoyad", adSoyad);
                insertCmd.Parameters.AddWithValue("@Sinif", sinif);
                insertCmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId); 
                insertCmd.ExecuteNonQuery();
                return (int)insertCmd.LastInsertedId;
            }
        }
        private int? GetDersID(MySqlConnection conn, string dersKodu)
        {
            string query = "SELECT DersID FROM Dersler WHERE DersKodu = @DersKodu AND BolumID = @BolumID"; 
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DersKodu", dersKodu);
                cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId); 
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : (int?)null;
            }
        }
        private void InsertOgrenciDersKaydi(MySqlConnection conn, int ogrenciID, int dersID)
        {
            string checkQuery = @"SELECT COUNT(*) FROM OgrenciDersKayitlari WHERE OgrenciID = @OgrenciID AND DersID = @DersID";
            using (var checkCmd = new MySqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@OgrenciID", ogrenciID);
                checkCmd.Parameters.AddWithValue("@DersID", dersID);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0) return;
            }
            string insertQuery = @"INSERT INTO OgrenciDersKayitlari (OgrenciID, DersID) VALUES (@OgrenciID, @DersID)";
            using (var insertCmd = new MySqlCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@OgrenciID", ogrenciID);
                insertCmd.Parameters.AddWithValue("@DersID", dersID);
                insertCmd.ExecuteNonQuery();
            }
        }
        #endregion
    }
}