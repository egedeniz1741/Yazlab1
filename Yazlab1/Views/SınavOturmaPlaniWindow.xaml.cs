using System.Collections.Generic; // List<> kullanmak için eklendi
using System.Windows;
using Yazlab1.ViewModel; // SinavOturmaPlaniViewModel ve AtanmisSinav sınıflarını tanımak için

namespace Yazlab1.Views // Namespace'inizin bu olduğundan emin olun
{
    /// <summary>
    /// SinavOturmaPlaniWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class SinavOturmaPlaniWindow : Window
    {
        // Constructor artık List<AtanmisSinav> ve string parametrelerini alıyor
        public SinavOturmaPlaniWindow(List<AtanmisSinav> tumSinavlar, string sinavAdiBasligi)
        {
            InitializeComponent();
            // Gelen parametreleri kullanarak doğru ViewModel'i oluşturup DataContext'e atıyoruz
            this.DataContext = new SinavOturmaPlaniViewModel(tumSinavlar, sinavAdiBasligi);
        }
    }
}