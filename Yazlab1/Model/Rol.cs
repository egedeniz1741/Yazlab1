using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Model;

namespace Yazlab1.Model
{
    public class Rol
    {
        [Key]
        public int RolID { get; set; }

        public string RolAdi { get; set; }

        public virtual ICollection<Kullanici> Kullanicilar { get; set; }

    }
}
