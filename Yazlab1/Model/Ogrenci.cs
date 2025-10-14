using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yazlab1.Model
{
    public class Ogrenci
    {
        [Key]
        public int OgrenciID { get; set; }

        public required string  OgrenciNo { get; set; }

        public required string AdSoyad { get; set; }

        public int Sinif { get; set; }

        public int BolumID { get; set; }
        [ForeignKey("BolumID")]
        public virtual  Bolum Bolum { get; set; }

        public virtual  ICollection<OgrenciDersKayitlari> DersKayitlari { get; set; }
    }
}