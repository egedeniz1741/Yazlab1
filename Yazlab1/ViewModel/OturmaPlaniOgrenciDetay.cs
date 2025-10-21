using Yazlab1.Model;


namespace Yazlab1.ViewModel 
{
    /// <summary>
    /// Görselleştirmede ve hesaplamalarda kullanılacak öğrenci oturma bilgisi.
    /// </summary>
    public class OturmaPlaniOgrenciDetay
    {
        public Ogrenci Ogrenci { get; set; }
        public Derslik Derslik { get; set; }

   
        public int Satir { get; set; }     
        public int Sutun { get; set; }     

    
        public string DisplayText => Ogrenci != null
            ? $"{Ogrenci.AdSoyad}\n({Ogrenci.OgrenciNo})\nSatır: {Satir} / Sütun: {Sutun}"
            : $"BOŞ";
    }
}