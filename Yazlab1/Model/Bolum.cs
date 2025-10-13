using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Yazlab1.Model
{
    public class Bolum
    {
        [Key]
        public int BolumID { get; set; }

        public required string  BolumAdi { get; set; }

        public virtual required ICollection<Kullanici> Kullanicilar { get; set; }
    }
}