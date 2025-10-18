using CommunityToolkit.Mvvm.ComponentModel;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
    public partial class DersListesiMenuViewModel : ObservableObject
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        private readonly int _aktifKullaniciBolumId;

        public ObservableCollection<Ders> TumDersler { get; set; }
        public ObservableCollection<Ogrenci> DerstekiOgrenciler { get; set; }

        [ObservableProperty]
        private Ders _secilenDers;

        public DersListesiMenuViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;
            TumDersler = new ObservableCollection<Ders>();
            DerstekiOgrenciler = new ObservableCollection<Ogrenci>();

            if (_aktifKullaniciBolumId == 0)
            {
                _aktifKullaniciBolumId = 1; 
            }


            DersleriYukle();
        }

        private async void DersleriYukle()
        {
            await Task.Run(() =>
            {
                var derslerListesi = new List<Ders>();
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
                                derslerListesi.Add(new Ders
                                {
                                    DersID = reader.GetInt32("DersID"),
                                    DersKodu = reader.GetString("DersKodu"),
                                    DersAdi = reader.GetString("DersAdi")
                                });
                            }
                        }
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TumDersler.Clear();
                    foreach (var ders in derslerListesi)
                    {
                        TumDersler.Add(ders);
                    }
                });
            });
        }

    
        async partial void OnSecilenDersChanged(Ders value)
        {
          
            DerstekiOgrenciler.Clear();
            if (value == null) return;

         
            await Task.Run(() =>
            {
           
                var ogrencilerListesi = new List<Ogrenci>();
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT o.OgrenciNo, o.AdSoyad FROM Ogrenciler o 
                                   JOIN OgrenciDersKayitlari odk ON o.OgrenciID = odk.OgrenciID 
                                   WHERE odk.DersID = @DersID ORDER BY o.OgrenciNo";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DersID", value.DersID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ogrencilerListesi.Add(new Ogrenci
                                {
                                    OgrenciNo = reader.GetString("OgrenciNo"),
                                    AdSoyad = reader.GetString("AdSoyad")
                                });
                            }
                        }
                    }
                }

              
                Application.Current.Dispatcher.Invoke(() =>
                {
                   
                    DerstekiOgrenciler.Clear();
                    foreach (var ogrenci in ogrencilerListesi)
                    {
                        DerstekiOgrenciler.Add(ogrenci);
                    }
                });
            });
        }
    }
}