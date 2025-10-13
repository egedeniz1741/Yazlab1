using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Yazlab1.Model;

namespace Yazlab1.Model
{
    public class OgretimUyesi
    {
        [Key]
        public int OgretimUyesiID { get; set; }

        public required string AdSoyad { get; set; }

        public virtual required ICollection<Ders> Dersler { get; set; }
    }
}