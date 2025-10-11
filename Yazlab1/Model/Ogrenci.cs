using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yazlab1.Models
{
    public class Ogrenci
    {
        [Key]
        public int OgrenciID { get; set; }

        public string OgrenciNo { get; set; }

        public string AdSoyad { get; set; }

        public int Sinif { get; set; }

        public int BolumID { get; set; }
        [ForeignKey("BolumID")]
        public virtual Bolum Bolum { get; set; }

        public virtual ICollection<OgrenciDersKayitlari> DersKayitlari { get; set; }
    }
}