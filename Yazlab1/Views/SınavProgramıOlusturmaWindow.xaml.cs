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

namespace Yazlab1.Views
{
    public partial class SinavProgramiOlusturmaWindow : Window
    {
        public SinavProgramiOlusturmaWindow(Kullanici aktifKullanici)
        {
            InitializeComponent();
            this.DataContext = new SinavProgramiOlusturmaViewModel(aktifKullanici);
        }

      
    }

}
