using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Yazlab1.Model;
using Yazlab1.ViewModel;

namespace Yazlab1
{
    /// <summary>
    /// DersListesiMenu.xaml etkileşim mantığı
    /// </summary>
    public partial class DersListesiMenu : Window
    {
        public DersListesiMenu(Kullanici aktifKullanici)
        {
            InitializeComponent();

            this.Title = "Ders Listesi Menusu " + aktifKullanici.Bolum.BolumAdi;
            this.DataContext = new DersListesiMenuViewModel(aktifKullanici);
        }
    }
}
