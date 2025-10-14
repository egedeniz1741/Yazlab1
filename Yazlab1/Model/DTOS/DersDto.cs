using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yazlab1.Model.DTOS
{
    public class DersDto
    {
        public string DersAdi { get; set; }
        public string DersKodu { get; set; }
        public string DersYapisi { get; set; }
        public int? Sinif { get; set; }
    }
}
