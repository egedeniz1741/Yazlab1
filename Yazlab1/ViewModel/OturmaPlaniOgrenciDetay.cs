using Yazlab1.Model;


namespace Yazlab1.ViewModel // Namespace doğru olmalı
{
    /// <summary>
    /// Görselleştirmede ve hesaplamalarda kullanılacak öğrenci oturma bilgisi.
    /// </summary>
    public class OturmaPlaniOgrenciDetay
    {
        public Ogrenci Ogrenci { get; set; }
        public Derslik Derslik { get; set; } // Hangi derslikte olduğunu bilmek için

        // --- İSİMLER ESKİ HALİNE DÖNDÜ ---
        public int Satir { get; set; }      // Hesaplanan GERÇEK Satır (1'den başlar)
        public int Sutun { get; set; }      // Hesaplanan GERÇEK Sütun (1'den başlar)

        // DisplayText artık AdSoyad, No ve GERÇEK Satır/Sütun içeriyor
        public string DisplayText => Ogrenci != null
            ? $"{Ogrenci.AdSoyad}\n({Ogrenci.OgrenciNo})\nSatır: {Satir} / Sütun: {Sutun}"
            : $"BOŞ";
    }
}