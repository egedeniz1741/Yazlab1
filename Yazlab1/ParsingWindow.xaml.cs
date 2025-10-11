using ClosedXML.Excel;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Yazlab1.Models;
using Yazlab1.ViewModel;

namespace Yazlab1
{
    /// <summary>
    /// ParsingWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class ParsingWindow : Window
    {

        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ParsingWindow(Kullanici Aktifkullanici)
        {
            InitializeComponent();

            this.Title = "Derslik Yönetimi " + Aktifkullanici.Bolum.BolumAdi;
            this.DataContext = new DerslikYonetimViewModel(Aktifkullanici);
        }

        #region DersListesiExcelParse

        private void ExcelButton_Click(object sender, RoutedEventArgs e) //Ders Listesi
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Dosyaları|*.xlsx;*.xls";
            if (ofd.ShowDialog() == true)
            {
                string path = ofd.FileName;
                ParseAndInsertDersListesi(path);
                MessageBox.Show("Ders Listesi verileri başarıyla eklendi!");
            }
        }

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

                        if (string.IsNullOrWhiteSpace(dersKodu) ||      //bazi kelimeleri atliyozki sqle eklerken bazi satirlari eklemesin
                            dersKodu.Contains("DERS KODU") ||
                            dersKodu.Contains("DERS KOD") ||
                            dersKodu.Contains("Sınıf") ||
                            dersKodu.Contains("SINIF") ||
                            dersKodu.Contains("SEÇMELİ") ||
                            dersKodu.Contains("SEÇİMLİK"))
                        {
                            continue;
                        }

                        string dersAdi = row.Cell(2).GetString().Trim();

                        if (dersAdi.Contains("DERSİN ADI") ||                           //yine bazi kelimeleri atliyozki sqle eklerken bazi satirlari eklemesin
                            (dersAdi.Contains("DERS") && dersAdi.Contains("ADI")) ||
                            dersAdi.Contains("SEÇMELİ") ||
                            dersAdi.Contains("SEÇİMLİK"))
                        {
                            continue;
                        }

                        string ogretimUyesi = row.Cell(3).GetString().Trim();

                                                                                    // ogretim uyesi yanlissa satiri eklemiyoz 
                        if (string.IsNullOrWhiteSpace(ogretimUyesi) ||
                            ogretimUyesi.Contains("ELEMANI") ||
                            ogretimUyesi.Contains("ÖĞR.") ||
                            ogretimUyesi.Contains("DERSİ VEREN") ||
                            ogretimUyesi.Contains("DERS VEREN") ||
                            ogretimUyesi.Length < 3)
                        {
                            continue; 
                        }

                        int ogretimUyesiID = GetOrInsertOgretimUyesi(conn, ogretimUyesi);

                        if (DersExists(conn, dersKodu, 1))
                        {
                            continue;
                        }

                        string insertDers = @"INSERT INTO Dersler 
                            (BolumID, OgretimUyesiID, DersKodu, DersAdi, Sinif, DersYapisi)
                            VALUES (1, @OgretimUyesiID, @DersKodu, @DersAdi, @Sinif, @DersYapisi)";

                        using (var cmd = new MySqlCommand(insertDers, conn))
                        {
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
                if (result != null)
                    return Convert.ToInt32(result);
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

        #region OgrenciListesiExcelParse

        private void ExcelButton2_Click(object sender, RoutedEventArgs e) //Ogrenci Listesi
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Dosyaları|*.xlsx;*.xls";
            if (ofd.ShowDialog() == true)
            {
                string path = ofd.FileName;
                ParseAndInsertOgrenciler(path);
                MessageBox.Show("Öğrenci verileri başarıyla eklendi!");
            }
        }

        private void ParseAndInsertOgrenciler(string filePath)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    foreach (var row in rows.Skip(1)) // ilk satiri atla
                    {
                        string ogrenciNo = row.Cell(1).GetString().Trim();
                        string adSoyad = row.Cell(2).GetString().Trim();
                        string sinif = row.Cell(3).GetString().Trim();
                        string dersKodu = row.Cell(4).GetString().Trim();

                        // Boş kontrolleri
                        if (string.IsNullOrWhiteSpace(ogrenciNo) ||
                            string.IsNullOrWhiteSpace(adSoyad) ||
                            string.IsNullOrWhiteSpace(dersKodu))
                        {
                            continue;
                        }

                        // sinif parse
                        int sinifNumara = ParseSinif(sinif);

                        // oğrenciyi ekliyoruz veya IDsini aliyoruz
                        int ogrenciID = GetOrInsertOgrenci(conn, ogrenciNo, adSoyad, sinifNumara);

                        // Ders ID'si alma
                        int? dersID = GetDersID(conn, dersKodu);

                        if (dersID.HasValue)
                        {
                            // Oğrenci Ders kaydini ekle
                            InsertOgrenciDersKaydi(conn, ogrenciID, dersID.Value);
                        }
                    }
                }
            }
        }

        private int ParseSinif(string sinif)
        {
            // 5. Sınıf -> 5 olarak yaziliyor int tanimli
            if (string.IsNullOrWhiteSpace(sinif))
                return 0;

            string[] parts = sinif.Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int sinifNo))
            {
                return sinifNo;
            }
            return 0;
        }

        private int GetOrInsertOgrenci(MySqlConnection conn, string ogrenciNo, string adSoyad, int sinif)
        {
            // Ogrenci var mi diye kontrol ediyoruz
            string checkQuery = "SELECT OgrenciID FROM Ogrenciler WHERE OgrenciNo = @OgrenciNo";
            using (var checkCmd = new MySqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@OgrenciNo", ogrenciNo);
                object result = checkCmd.ExecuteScalar();
                if (result != null)
                    return Convert.ToInt32(result);
            }

            // Ogrenciyi bulamadiysak ekliyoruz
            string insertQuery = @"INSERT INTO Ogrenciler (OgrenciNo, AdSoyad, Sinif, BolumID) 
                          VALUES (@OgrenciNo, @AdSoyad, @Sinif, @BolumID)";
            using (var insertCmd = new MySqlCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@OgrenciNo", ogrenciNo);
                insertCmd.Parameters.AddWithValue("@AdSoyad", adSoyad);
                insertCmd.Parameters.AddWithValue("@Sinif", sinif);
                insertCmd.Parameters.AddWithValue("@BolumID", 1); 
                insertCmd.ExecuteNonQuery();
                return (int)insertCmd.LastInsertedId;
            }
        }

        private int? GetDersID(MySqlConnection conn, string dersKodu)
        {
            string query = "SELECT DersID FROM Dersler WHERE DersKodu = @DersKodu AND BolumID = 1";
            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DersKodu", dersKodu);
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }

        private void InsertOgrenciDersKaydi(MySqlConnection conn, int ogrenciID, int dersID)
        {
            // Önce kayıt var mı kontrol et
            string checkQuery = @"SELECT COUNT(*) FROM OgrenciDersKayitlari 
                         WHERE OgrenciID = @OgrenciID AND DersID = @DersID";
            using (var checkCmd = new MySqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@OgrenciID", ogrenciID);
                checkCmd.Parameters.AddWithValue("@DersID", dersID);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                    return; // Zaten kayıtlı
            }

            // Yoksa ekle
            string insertQuery = @"INSERT INTO OgrenciDersKayitlari (OgrenciID, DersID) 
                          VALUES (@OgrenciID, @DersID)";
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
