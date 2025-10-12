using System.Windows;
using Yazlab1.Models;
using Yazlab1.ViewModel;

namespace Yazlab1.Views 
{
    public partial class ParsingWindow : Window
    {
        public ParsingWindow(Kullanici aktifKullanici)
        {
            InitializeComponent();
            this.DataContext = new ParsingViewModel(aktifKullanici);
        }

        private void GeriButton_Click(object sender, RoutedEventArgs e)
        {
          

            this.Close(); 
        }
    }
}