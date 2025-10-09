using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Models;

namespace Yazlab1.Model
{
    public class Derslik
    {
        [Key]
        public int DerslikID { get; set; }

        public int BolumID { get; set; }
        [ForeignKey("BolumID")]
        public virtual Bolum Bolum { get; set; }

        public string DerslikKodu { get; set; }

        public string DerslikAdi { get; set; }

        public int Kapasite { get; set; }

        public int EnineSiraSayisi { get; set; }

        public int BoyunaSiraSayisi { get; set; }

        public int SiraYapisi { get; set; }
    }
}