using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Yazlab1.Model
{
    public class OgrenciDersKayitlari
    {
        
        public int OgrenciID { get; set; }
        [ForeignKey("OgrenciID")]
        public virtual required Ogrenci Ogrenci { get; set; }

        public int DersID { get; set; }
        [ForeignKey("DersID")]
        public virtual required Ders Ders { get; set; }
    }
}