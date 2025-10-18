using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Model;

namespace Yazlab1.ViewModel
{
    public class OturmaPlaniDetay
    {
       
        public Ogrenci Ogrenci { get; set; }

        public string GörüntüMetni => Ogrenci != null ? $"{Ogrenci.AdSoyad}\n({Ogrenci.OgrenciNo})" : "BOŞ";
    }
}
