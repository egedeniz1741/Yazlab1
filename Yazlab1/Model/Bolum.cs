using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Yazlab1.Model
{
    public class Bolum
    {
        [Key]
        public int BolumID { get; set; }

        public  string  BolumAdi { get; set; }

        public virtual  ICollection<Kullanici> Kullanicilar { get; set; }
    }
}