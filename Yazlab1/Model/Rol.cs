using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Models;

namespace Yazlab1.Model
{
    public class Rol
    {
        [Key]
        int RolID { get; set; }

        string RolAdi { get; set; }

        public virtual ICollection<Kullanici> Kullanicilar { get; set; }

    }
}
