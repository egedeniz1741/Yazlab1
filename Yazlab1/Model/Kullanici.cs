using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Yazlab1.Model;

namespace Yazlab1.Model
{
    public class Kullanici
    {
        [Key]
        public int KullaniciID { get; set; }

        public string ?Eposta { get; set; }

        public string ?SifreHash { get; set; }

        public string ?AdSoyad { get; set; }   
        public int RolID { get; set; }
        [ForeignKey("RolID")]
        public virtual  Rol? Rol { get; set; }

        public int? BolumID { get; set; } 
        [ForeignKey("BolumID")]
        public virtual  Bolum Bolum { get; set; }
    }
}