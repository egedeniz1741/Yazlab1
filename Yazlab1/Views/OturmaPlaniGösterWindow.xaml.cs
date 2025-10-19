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
           
            this.DataContext = new OturmaPlaniGosterViewModel(secilenSinav, sinavAdiBasligi);
            
            this.SetBinding(Window.TitleProperty, "PencereBasligi");
        }
    }
}