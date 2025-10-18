using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MySql.Data.MySqlClient;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Model;

using Yazlab1.Views;

namespace Yazlab1.ViewModel
{
    public partial class DerslikYonetimViewModel : ObservableObject
    {
        private readonly Kullanici _aktifKullanici;
        private readonly int _aktifKullaniciBolumId;
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        [ObservableProperty] private string _derslikKodu;
        [ObservableProperty] private string _derslikAdi;
        [ObservableProperty] private int _kapasite;
        [ObservableProperty] private int _enineSiraSayisi;
        [ObservableProperty] private int _boyunaSiraSayisi;
        [ObservableProperty] private int _siraYapisi;
        [ObservableProperty] private Derslik _selectedDerslik;

        public ObservableCollection<Derslik> Derslikler { get; set; }
        public ObservableCollection<object> SeatLayout { get; set; }

        public DerslikYonetimViewModel(Kullanici aktifKullanici)
        {
            _aktifKullanici = aktifKullanici;
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;

            if (_aktifKullaniciBolumId == 0)
            {
                _aktifKullaniciBolumId = 1; // kotu cozum ama baska turlu yapamadim valla 
            }


            Derslikler = new ObservableCollection<Derslik>();
            SeatLayout = new ObservableCollection<object>();
            DerslikleriYukle();
        }

        private void DerslikleriYukle()
        {
            Derslikler.Clear();
            var derslikListesi = new List<Derslik>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Derslikler WHERE BolumID = @BolumID";
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
            foreach (var derslik in derslikListesi)
            {
                Derslikler.Add(derslik);
            }
        }

        [RelayCommand]
        private void Ekle()
        {
            if (string.IsNullOrWhiteSpace(DerslikKodu) || string.IsNullOrWhiteSpace(DerslikAdi))
            {
                MessageBox.Show("Derslik Kodu ve Adı alanları boş bırakılamaz.");
                return;
            }

            bool isDuplicate = false;
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Derslikler WHERE DerslikKodu = @DerslikKodu AND BolumID = @BolumID";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DerslikKodu", DerslikKodu);
                    cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                    isDuplicate = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }

            if (isDuplicate)
            {
                MessageBox.Show("Bu derslik kodu zaten mevcut. Lütfen farklı bir kod girin.");
                return;
            }

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "INSERT INTO Derslikler (BolumID, DerslikKodu, DerslikAdi, Kapasite, EnineSiraSayisi, BoyunaSiraSayisi, SiraYapisi) VALUES (@BolumID, @DerslikKodu, @DerslikAdi, @Kapasite, @EnineSira, @BoyunaSira, @SiraYapisi)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                    cmd.Parameters.AddWithValue("@DerslikKodu", DerslikKodu);
                    cmd.Parameters.AddWithValue("@DerslikAdi", DerslikAdi);
                    cmd.Parameters.AddWithValue("@Kapasite", Kapasite);
                    cmd.Parameters.AddWithValue("@EnineSira", EnineSiraSayisi);
                    cmd.Parameters.AddWithValue("@BoyunaSira", BoyunaSiraSayisi);
                    cmd.Parameters.AddWithValue("@SiraYapisi", SiraYapisi);
                    cmd.ExecuteNonQuery();
                }
            }
            DerslikleriYukle();
            MessageBox.Show("Derslik başarıyla eklendi.");
        }

        [RelayCommand]
        private void Guncelle()
        {
            if (SelectedDerslik == null)
            {
                MessageBox.Show("Lütfen güncellemek için bir derslik seçin.");
                return;
            }

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Derslikler SET DerslikKodu = @DerslikKodu, DerslikAdi = @DerslikAdi, Kapasite = @Kapasite, EnineSiraSayisi = @EnineSira, BoyunaSiraSayisi = @BoyunaSira, SiraYapisi = @SiraYapisi WHERE DerslikID = @DerslikID";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DerslikKodu", DerslikKodu);
                    cmd.Parameters.AddWithValue("@DerslikAdi", DerslikAdi);
                    cmd.Parameters.AddWithValue("@Kapasite", Kapasite);
                    cmd.Parameters.AddWithValue("@EnineSira", EnineSiraSayisi);
                    cmd.Parameters.AddWithValue("@BoyunaSira", BoyunaSiraSayisi);
                    cmd.Parameters.AddWithValue("@SiraYapisi", SiraYapisi);
                    cmd.Parameters.AddWithValue("@DerslikID", SelectedDerslik.DerslikID);
                    cmd.ExecuteNonQuery();
                }
            }
            DerslikleriYukle();
            MessageBox.Show("Derslik başarıyla güncellendi.");
        }

        [RelayCommand]
        private void Sil()
        {
            if (SelectedDerslik == null)
            {
                MessageBox.Show("Lütfen silmek için bir derslik seçin.");
                return;
            }

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Derslikler WHERE DerslikID = @DerslikID";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DerslikID", SelectedDerslik.DerslikID);
                    cmd.ExecuteNonQuery();
                }
            }
            DerslikleriYukle();
            MessageBox.Show("Derslik başarıyla silindi.");
        }

        [RelayCommand]
        private void ExcelEkraninaGit()
        {
            ParsingWindow parsingWindow = new ParsingWindow(_aktifKullanici);
            parsingWindow.Show();
        }

        [RelayCommand]
        private void OgrenciMenuEkraninaGit()
        {
            OgrenciListesiMenu ogrenciMenu = new OgrenciListesiMenu(_aktifKullanici);
            ogrenciMenu.Show();
        }

        [RelayCommand]
        private void DersMenuEkraninaGit()
        {
            DersListesiMenu dersMenu = new DersListesiMenu(_aktifKullanici);
            dersMenu.Show();
        }

        [RelayCommand]
        private async Task SinavProgramiEkraninaGit()
        {
            int dersSayisi = 0;
            int ogrenciSayisi = 0;
            await Task.Run(() =>
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string dersQuery = "SELECT COUNT(*) FROM Dersler WHERE BolumID = @BolumID";
                    using (var cmd = new MySqlCommand(dersQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                        dersSayisi = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    string ogrenciQuery = "SELECT COUNT(*) FROM Ogrenciler WHERE BolumID = @BolumID";
                    using (var cmd = new MySqlCommand(ogrenciQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                        ogrenciSayisi = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            });
            if (dersSayisi == 0 || ogrenciSayisi == 0)
            {
                MessageBox.Show("Sınav programı oluşturmak için sistemde kayıtlı ders ve öğrenci bulunmalıdır. Lütfen önce 'Excel Veri Yükle' ekranından verileri yükleyin.", "Eksik Veri Uyarısı", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                SinavProgramiOlusturmaWindow sinavMenu = new SinavProgramiOlusturmaWindow(_aktifKullanici);
                sinavMenu.Show();
            }
        }

        partial void OnSelectedDerslikChanged(Derslik value)
        {
            if (value != null)
            {
                DerslikKodu = value.DerslikKodu;
                DerslikAdi = value.DerslikAdi;
                Kapasite = value.Kapasite;
                EnineSiraSayisi = value.EnineSiraSayisi;
                BoyunaSiraSayisi = value.BoyunaSiraSayisi;
                SiraYapisi = value.SiraYapisi;
                GenerateSeatLayout();
            }
        }

        private void GenerateSeatLayout()
        {
            SeatLayout.Clear();
            if (SelectedDerslik == null || SelectedDerslik.BoyunaSiraSayisi <= 0 || SelectedDerslik.EnineSiraSayisi <= 0)
            {
                return;
            }
            int totalSlots = SelectedDerslik.BoyunaSiraSayisi * SelectedDerslik.EnineSiraSayisi;
            for (int i = 0; i < totalSlots; i++)
            {
                SeatLayout.Add(new object());
            }
        }
    }
}