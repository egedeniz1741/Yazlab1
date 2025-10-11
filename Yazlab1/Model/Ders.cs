using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yazlab1.Models
{
    public class Ders
    {
        [Key]
        public int DersID { get; set; }

        public int BolumID { get; set; }
        [ForeignKey("BolumID")]
        public virtual Bolum Bolum { get; set; }

        public int? OgretimUyesiID { get; set; }
        [ForeignKey("OgretimUyesiID")]
        public virtual OgretimUyesi OgretimUyesi { get; set; }

        public string DersKodu { get; set; }

        public string DersAdi { get; set; }

        public int? Sinif { get; set; }

        public string DersYapisi { get; set; } 
    }
}