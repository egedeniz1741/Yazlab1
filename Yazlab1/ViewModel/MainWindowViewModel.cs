using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient; 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
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
        private readonly Kullanici _aktifAdmin; 
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

      
        [ObservableProperty] private string _newUserEmail;
        [ObservableProperty] private Bolum _selectedBolum;
        [ObservableProperty]
        private ObservableCollection<Kullanici> _kullaniciListesi;

        [ObservableProperty]
        private Kullanici selectedKullanici;


        public ObservableCollection<Bolum> BolumlerListesi { get; set; }

      
        public MainWindowViewModel(Kullanici aktifKullanici)
        {
            _aktifAdmin = aktifKullanici;
            BolumlerListesi = new ObservableCollection<Bolum>();

            
            LoadBolumler();
        }
 

        public void LoadAllUsers()
        {
            try
            {
                KullaniciListesi = new ObservableCollection<Kullanici>();

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                    SELECT k.KullaniciID, k.Eposta, k.AdSoyad, k.RolID, k.BolumID, 
                           r.RolAdi, b.BolumAdi
                    FROM Kullanicilar k
                    LEFT JOIN Roller r ON k.RolID = r.RolID
                    LEFT JOIN Bolumler b ON k.BolumID = b.BolumID
                    ORDER BY k.AdSoyad";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            KullaniciListesi.Add(new Kullanici
                            {
                                KullaniciID = reader.GetInt32("KullaniciID"),
                                Eposta = reader.GetString("Eposta"),
                                AdSoyad = reader.GetString("AdSoyad"),
                                RolID = reader.IsDBNull("RolID") ? 0 : reader.GetInt32("RolID"),
                                BolumID = reader.IsDBNull("BolumID") ? 0 : reader.GetInt32("BolumID"),
                                Rol = new Rol { RolAdi = reader.IsDBNull("RolAdi") ? "Belirsiz" : reader.GetString("RolAdi") },
                                Bolum = new Bolum { BolumAdi = reader.IsDBNull("BolumAdi") ? "Belirsiz" : reader.GetString("BolumAdi") }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcılar yüklenirken hata oluştu: {ex.Message}");
            }
        }

    
        public void DeleteUser(Kullanici kullanici)
        {
            if (kullanici == null)
            {
                MessageBox.Show("Lütfen silmek için bir kullanıcı seçin.", "Uyarı");
                return;
            }

           
            if (kullanici.KullaniciID == _aktifAdmin.KullaniciID)
            {
                MessageBox.Show("Kendi hesabınızı silemezsiniz!", "Uyarı");
                return;
            }

            var result = MessageBox.Show(
                $"{kullanici.AdSoyad} kullanıcısını silmek istediğinizden emin misiniz?",
                "Kullanıcı Silme",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "DELETE FROM Kullanicilar WHERE KullaniciID = @KullaniciID";

                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@KullaniciID", kullanici.KullaniciID);
                            int affectedRows = cmd.ExecuteNonQuery();

                            if (affectedRows > 0)
                            {
                                MessageBox.Show("Kullanıcı başarıyla silindi.");
                                KullaniciListesi.Remove(kullanici);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kullanıcı silinirken hata oluştu: {ex.Message}");
                }
            }
        }

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

     
        [RelayCommand]
        public  void NavigateToBolum(string bolumAdi)
        {
            if (string.IsNullOrEmpty(bolumAdi)) return;

            var targetBolum = BolumlerListesi.FirstOrDefault(b => b.BolumAdi == bolumAdi);

            if (targetBolum == null)
            {
                MessageBox.Show($"{bolumAdi} bulunamadı.", "Hata");
                return;
            }

          
            var fakeCoordinator = new Kullanici
            {
                KullaniciID = _aktifAdmin.KullaniciID,
                AdSoyad = _aktifAdmin.AdSoyad,
                RolID = _aktifAdmin.RolID,
                Rol = _aktifAdmin.Rol, 
                BolumID = targetBolum.BolumID,
                Bolum = targetBolum 
            };

            DerslikYonetimWindow derslikWindow = new DerslikYonetimWindow(fakeCoordinator);
            derslikWindow.Show();
        }

      
        public void AddNewCoordinator(string email, string password,  Bolum bolum)
        {
            
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || bolum == null)
            {
                MessageBox.Show("Tüm alanlar doldurulmalıdır.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                
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

                    string adSoyad = $"{bolum.BolumAdi} Koordinatörü";

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
                if (ex.Number == 1062) 
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