using System.Windows;
using Yazlab1.ViewModel; 

namespace Yazlab1.Views
{
    public partial class OturmaPlaniGosterWindow : Window
    {
   
        public OturmaPlaniGosterWindow(AtanmisSinav secilenSinav, string sinavAdiBasligi)
        {
            InitializeComponent();
           
            this.DataContext = new OturmaPlaniGosterViewModel(secilenSinav, sinavAdiBasligi);
            
            this.SetBinding(Window.TitleProperty, "PencereBasligi");
        }
    }
}