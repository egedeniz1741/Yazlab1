using System.Collections.Generic; 
using System.Windows;
using Yazlab1.ViewModel;

namespace Yazlab1.Views 
{
    /// <summary>
    /// SinavOturmaPlaniWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class SinavOturmaPlaniWindow : Window
    {
      
        public SinavOturmaPlaniWindow(List<AtanmisSinav> tumSinavlar, string sinavAdiBasligi)
        {
            InitializeComponent();
          
            this.DataContext = new SinavOturmaPlaniViewModel(tumSinavlar, sinavAdiBasligi);
           
            this.Title = "Sınav Oturma Planları - " + sinavAdiBasligi;
        }
    }
}