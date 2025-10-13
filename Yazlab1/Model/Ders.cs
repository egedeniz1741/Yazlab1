using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yazlab1.Model
{
    public class Ders
    {
        [Key]
        public int DersID { get; set; }

        public int BolumID { get; set; }
        [ForeignKey("BolumID")]
        public virtual required Bolum Bolum { get; set; }

        public int? OgretimUyesiID { get; set; }
        [ForeignKey("OgretimUyesiID")]
        public virtual required OgretimUyesi OgretimUyesi { get; set; }

        public required string DersKodu { get; set; }

        public required string DersAdi { get; set; }

        public int? Sinif { get; set; }

        public required string  DersYapisi { get; set; } 
    }
}