using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient; // SQL için eklendi
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
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly Kullanici _aktifAdmin; // Admin bilgilerini saklamak için
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        // === Özellikler (Properties) ===
        // Kullanıcı ekleme formu için
        [ObservableProperty] private string _newUserEmail;
        [ObservableProperty] private string _newUserAdSoyad;
        [ObservableProperty] private Bolum _selectedBolum; // ComboBox'tan seçilen bölüm

        // Bölüm listesi (ComboBox için)
        public ObservableCollection<Bolum> BolumlerListesi { get; set; }

        // === Constructor ===
        public MainWindowViewModel(Kullanici aktifKullanici)
        {
            _aktifAdmin = aktifKullanici;
            BolumlerListesi = new ObservableCollection<Bolum>();

            // Veritabanından bölümleri çekip ComboBox'ı doldur
            LoadBolumler();
        }

        /// <summary>
        /// ComboBox'ı doldurmak için veritabanından bölümleri çeker.
        /// </summary>
        private void LoadBolumler()
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT BolumID, BolumAdi FROM Bolumler";
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            BolumlerListesi.Add(new Bolum
                            {
                                BolumID = reader.GetInt32("BolumID"),
                                BolumAdi = reader.GetString("BolumAdi")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bölümler yüklenirken hata oluştu: {ex.Message}");
            }
        }

        /// <summary>
        /// Adminin bir bölümün koordinatör paneline gitmesini sağlar.
        /// </summary>
        [RelayCommand]
        public  void NavigateToBolum(string bolumAdi)
        {
            if (string.IsNullOrEmpty(bolumAdi)) return;

            // Hafızaya yüklediğimiz BolumlerListesi'nden hedef bölümü bul
            var targetBolum = BolumlerListesi.FirstOrDefault(b => b.BolumAdi == bolumAdi);

            if (targetBolum == null)
            {
                MessageBox.Show($"{bolumAdi} bulunamadı.", "Hata");
                return;
            }

            // ÖNEMLİ: Adminin yetkileriyle, seçilen bölümün kimliğine bürünüyoruz.
            // Geçici bir "fake" kullanıcı nesnesi oluşturup DerslikYonetimWindow'a gönderiyoruz.
            var fakeCoordinator = new Kullanici
            {
                KullaniciID = _aktifAdmin.KullaniciID,
                AdSoyad = _aktifAdmin.AdSoyad,
                RolID = _aktifAdmin.RolID,
                Rol = _aktifAdmin.Rol, // Admin rolünü korur
                BolumID = targetBolum.BolumID, // Ama hedef bölümün ID'sini alır
                Bolum = targetBolum // Ve hedef bölümün nesnesini alır
            };

            DerslikYonetimWindow derslikWindow = new DerslikYonetimWindow(fakeCoordinator);
            derslikWindow.Show();
        }

        /// <summary>
        /// Veritabanına yeni bir Bölüm Koordinatörü ekler.
        /// </summary>
        public void AddNewCoordinator(string email, string password, string adSoyad, Bolum bolum)
        {
            // Basit doğrulama
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(adSoyad) || bolum == null)
            {
                MessageBox.Show("Tüm alanlar doldurulmalıdır.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. Bölüm Koordinatörü rolünün ID'sini bul
                    string rolQuery = "SELECT RolID FROM Roller WHERE RolAdi = 'Bölüm Koordinatörü'";
                    int koordinatorRolID;
                    using (var cmdRol = new MySqlCommand(rolQuery, conn))
                    {
                        var result = cmdRol.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("Rol ('Bölüm Koordinatörü') bulunamadı. Lütfen veritabanını kontrol edin.", "Hata");
                            return;
                        }
                        koordinatorRolID = Convert.ToInt32(result);
                    }

                    // 2. Yeni kullanıcıyı ekle
                    // TODO: Şifreyi burada hash'lemek en doğrusudur.
                    // Örn: string hashedPassword = HashPassword(password);
                    string insertQuery = @"INSERT INTO Kullanicilar (Eposta, SifreHash, AdSoyad, RolID, BolumID) 
                                         VALUES (@Eposta, @Sifre, @AdSoyad, @RolID, @BolumID)";
                    using (var cmdInsert = new MySqlCommand(insertQuery, conn))
                    {
                        cmdInsert.Parameters.AddWithValue("@Eposta", email);
                        cmdInsert.Parameters.AddWithValue("@Sifre", password); 
                      
                        cmdInsert.Parameters.AddWithValue("@AdSoyad", adSoyad);
                    
                        cmdInsert.Parameters.AddWithValue("@RolID", koordinatorRolID);
                        cmdInsert.Parameters.AddWithValue("@BolumID", bolum.BolumID);

                        cmdInsert.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Yeni bölüm koordinatörü başarıyla eklendi.");
               
               
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Duplicate entry
                    MessageBox.Show("Bu e-posta adresi zaten kullanılıyor.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show($"Veritabanı hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bilinmeyen bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}