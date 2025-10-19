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
            // Pencere başlığını da ViewModel'den almasını sağlayabiliriz (opsiyonel)
            // this.SetBinding(Window.TitleProperty, "PencereBasligi"); // Eğer ViewModel'de PencereBasligi property'si varsa
            // Veya direkt burada ayarlayabiliriz:
            this.Title = "Sınav Oturma Planları - " + sinavAdiBasligi;
        }
    }
}