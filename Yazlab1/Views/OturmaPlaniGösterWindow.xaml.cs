using System.Windows;
using Yazlab1.ViewModel; // Gerekli namespace

namespace Yazlab1.Views
{
    public partial class OturmaPlaniGosterWindow : Window
    {
        // Constructor, seçilen sınavı ve başlığı parametre olarak alır
        public OturmaPlaniGosterWindow(AtanmisSinav secilenSinav, string sinavAdiBasligi)
        {
            InitializeComponent();
            // Yeni ViewModel'i oluşturup DataContext'e atıyoruz
            this.DataContext = new OturmaPlaniGosterViewModel(secilenSinav, sinavAdiBasligi);
            // Pencere başlığını ViewModel'den alıyoruz (opsiyonel, constructor'da da set edilebilir)
            this.SetBinding(Window.TitleProperty, "PencereBasligi");
        }
    }
}