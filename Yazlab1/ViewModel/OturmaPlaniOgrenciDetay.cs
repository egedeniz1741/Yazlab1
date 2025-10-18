using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Model;

namespace Yazlab1.ViewModel
{
    public class OturmaPlaniOgrenciDetay
    {
        public Ogrenci Ogrenci { get; set; }
        public Derslik Derslik { get; set; }
        public int Satir { get; set; } // Oturduğu sıra (1'den başlar)
        public int Sutun { get; set; } // Oturduğu sütun (1'den başlar)

        // Görselleştirme için basit gösterim
        public string DisplayText => Ogrenci != null ? $"{Ogrenci.OgrenciNo}" : "BOŞ";
        // PDF için detaylı gösterim
        public string PdfDisplayText => Ogrenci != null ? $"{Ogrenci.AdSoyad} ({Ogrenci.OgrenciNo})" : "BOŞ";
    }
}
