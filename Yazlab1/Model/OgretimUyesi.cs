using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Yazlab1.Model;

namespace Yazlab1.Models
{
    public class OgretimUyesi
    {
        [Key]
        public int OgretimUyesiID { get; set; }

        public string AdSoyad { get; set; }

        public virtual ICollection<Ders> Dersler { get; set; }
    }
}