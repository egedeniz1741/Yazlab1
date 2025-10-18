using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
    public partial class OturmaPlaniViewModel : ObservableObject
    {
        // Arayüzdeki TabControl'e bağlanacak ana veri yapısı
        // Key: Derslik, Value: O dersliğin oturma planı
        public Dictionary<Derslik, ObservableCollection<OturmaPlaniDetay>> YerlesimPlani { get; set; }

        private readonly List<AtanmisSinav> _atanmisSinavlar;
        private readonly string _sinavAdi;

        public OturmaPlaniViewModel(List<AtanmisSinav> atanmisSinavlar, string sinavAdi)
        {
            _atanmisSinavlar = atanmisSinavlar;
            _sinavAdi = sinavAdi; // "BİLGİSAYAR MÜHENDİSLİĞİ BÖLÜMÜ VİZE SINAV PROGRAMI" gibi
            YerlesimPlani = new Dictionary<Derslik, ObservableCollection<OturmaPlaniDetay>>();

            // Sınav programından oturma planını oluştur
            OturmaPlaniOlustur();
        }

        private void OturmaPlaniOlustur()
        {
            // Tüm öğrencileri tek bir listeye topla
            var tumOgrenciler = _atanmisSinavlar.SelectMany(s => s.SinavDetay.Ogrenciler).Distinct().ToList();

            // Tüm derslikleri ve koltuk sayılarını hesapla
            var tumDerslikler = _atanmisSinavlar.SelectMany(s => s.AtananDerslikler).Distinct().ToList();

            int ogrenciIndex = 0;

            foreach (var derslik in tumDerslikler)
            {
                var plan = new ObservableCollection<OturmaPlaniDetay>();
                int koltukSayisi = derslik.EnineSiraSayisi * derslik.BoyunaSiraSayisi * derslik.SiraYapisi;

                for (int i = 0; i < koltukSayisi; i++)
                {
                    if (ogrenciIndex < tumOgrenciler.Count)
                    {
                        plan.Add(new OturmaPlaniDetay { Ogrenci = tumOgrenciler[ogrenciIndex] });
                        ogrenciIndex++;
                    }
                    else
                    {
                        plan.Add(new OturmaPlaniDetay { Ogrenci = null }); // Kalan koltuklar boş
                    }
                }
                YerlesimPlani[derslik] = plan;
            }
        }

        [RelayCommand]
        private void ExcelAktar()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                Title = "Oturma Planını Kaydet",
                FileName = $"Oturma_Plani_{DateTime.Now:yyyy-MM-dd}.xlsx"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    foreach (var kvp in YerlesimPlani)
                    {
                        var derslik = kvp.Key;
                        var plan = kvp.Value;
                        var worksheet = workbook.Worksheets.Add(derslik.DerslikKodu);

                        worksheet.Cell("A1").Value = $"{_sinavAdi} - {derslik.DerslikAdi} ({derslik.DerslikKodu}) Oturma Planı";
                        worksheet.Range(1, 1, 1, derslik.EnineSiraSayisi * derslik.SiraYapisi).Merge();

                        int planIndex = 0;
                        for (int satir = 0; satir < derslik.BoyunaSiraSayisi; satir++)
                        {
                            for (int sutun = 0; sutun < derslik.EnineSiraSayisi * derslik.SiraYapisi; sutun++)
                            {
                                if (planIndex < plan.Count)
                                {
                                    var hucre = worksheet.Cell(satir + 3, sutun + 1);
                                    hucre.Value = plan[planIndex].GörüntüMetni;
                                    hucre.Style.Alignment.WrapText = true;
                                    planIndex++;
                                }
                            }
                        }
                        worksheet.Columns().AdjustToContents();
                        worksheet.Rows().AdjustToContents();
                    }
                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show("Oturma planı başarıyla Excel'e aktarıldı.", "Başarılı");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excel'e aktarırken bir hata oluştu: {ex.Message}", "Hata");
            }
        }
    }
}