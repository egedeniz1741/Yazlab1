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

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel Dosyaları|*.xlsx;*.xls";
            if (ofd.ShowDialog() == true)
            {
                string path = ofd.FileName;
                ParseAndInsertExcel(path);
                MessageBox.Show("Excel verileri başarıyla eklendi!");
            }
        }
        private void ExcelButton2_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ParseAndInsertExcel(string filePath)
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
    }
}
