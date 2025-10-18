using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient; 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Data;
using Yazlab1.Model;
using Yazlab1.Model.DTOS;


namespace Yazlab1.ViewModel
{
    public partial class OgrenciListesiMenuViewModel : ObservableObject
    {
        private readonly SinavTakvimDbContext _dbContext = new SinavTakvimDbContext();
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private readonly int _aktifKullaniciBolumId;

        [ObservableProperty] private ObservableCollection<string> _ogrenciNumaralari;
        [ObservableProperty] private string _secilenOgrenciNumarasi;
        [ObservableProperty] private string _ogrenciBilgi = "Lütfen bir öğrenci seçin veya numarasını girip 'Getir' butonuna basın.";
        [ObservableProperty] private ObservableCollection<DersDto> _dersler;

        public OgrenciListesiMenuViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;

            if (_aktifKullaniciBolumId == 0)
            {
                _aktifKullaniciBolumId = 1; 
            }

            Dersler = new ObservableCollection<DersDto>();
            OgrenciNumaralariniYukle();
        }

        private async void OgrenciNumaralariniYukle()
        {
            try
            {
                var numaralar = await _dbContext.Ogrenciler
                    .Where(o => o.BolumID == _aktifKullaniciBolumId)
                    .Select(o => o.OgrenciNo)
                    .OrderBy(no => no)
                    .ToListAsync();
                OgrenciNumaralari = new ObservableCollection<string>(numaralar);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Öğrenciler yüklenirken hata oluştu: {ex.Message}", "Hata");
            }
        }

        [RelayCommand]
        private async Task OgrenciBilgileriniGetir()
        {
            OgrenciBilgi = "";
            Dersler.Clear();

            if (string.IsNullOrWhiteSpace(SecilenOgrenciNumarasi))
            {
                MessageBox.Show("Lütfen bir öğrenci numarası seçiniz veya giriniz!", "Uyarı");
                return;
            }

            try
            {
          
                await Task.Run(() =>
                {
                    using (var conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        Ogrenci ogrenci = null;

                  
                        string ogrenciQuery = "SELECT OgrenciID, AdSoyad, OgrenciNo, Sinif FROM Ogrenciler WHERE OgrenciNo = @OgrenciNo AND BolumID = @BolumID";
                        using (var cmd = new MySqlCommand(ogrenciQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@OgrenciNo", SecilenOgrenciNumarasi);
                            cmd.Parameters.AddWithValue("@BolumID", _aktifKullaniciBolumId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    ogrenci = new Ogrenci
                                    {
                                        OgrenciID = reader.GetInt32("OgrenciID"),
                                        AdSoyad = reader.GetString("AdSoyad"),
                                        OgrenciNo = reader.GetString("OgrenciNo"),
                                        Sinif = reader.GetInt32("Sinif")
                                    };
                                }
                            }
                        }

                      
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (ogrenci == null)
                            {
                                OgrenciBilgi = "Bu numaraya ait öğrenci bulunamadı.";
                                return;
                            }
                            OgrenciBilgi = $"İsim: {ogrenci.AdSoyad}  |  Numara: {ogrenci.OgrenciNo}  |  Sınıf: {ogrenci.Sinif}";
                        });

                        if (ogrenci == null) return;

                      
                        var dersDtoList = new List<DersDto>();
                        string dersQuery = @"SELECT d.DersAdi, d.DersKodu, d.DersYapisi, d.Sinif 
                                           FROM OgrenciDersKayitlari odk
                                           JOIN Dersler d ON odk.DersID = d.DersID
                                           WHERE odk.OgrenciID = @OgrenciID";
                        using (var cmd = new MySqlCommand(dersQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@OgrenciID", ogrenci.OgrenciID);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    dersDtoList.Add(new DersDto
                                    {
                                       
                                        DersAdi = reader.IsDBNull(reader.GetOrdinal("DersAdi")) ? null : reader.GetString("DersAdi"),
                                        DersKodu = reader.IsDBNull(reader.GetOrdinal("DersKodu")) ? null : reader.GetString("DersKodu"),
                                        DersYapisi = reader.IsDBNull(reader.GetOrdinal("DersYapisi")) ? null : reader.GetString("DersYapisi"),
                                        Sinif = reader.IsDBNull(reader.GetOrdinal("Sinif")) ? (int?)null : reader.GetInt32("Sinif")
                                    });
                                }
                            }
                        }

                     
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Dersler.Clear();
                            if (dersDtoList.Any())
                            {
                                foreach (var ders in dersDtoList)
                                {
                                    Dersler.Add(ders);
                                }
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata oluştu: {ex.Message}", "Hata");
            }
        }

        partial void OnSecilenOgrenciNumarasiChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && (OgrenciNumaralari?.Contains(value) ?? false))
            {
                OgrenciBilgileriniGetirCommand.Execute(null);
            }
        }
    }
}