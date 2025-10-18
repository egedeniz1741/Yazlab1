using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Windows;
using Yazlab1.Model;
using Yazlab1.ViewModel;
namespace Yazlab1
{
    public partial class MainWindow : Window
    {
        public MainWindow(Kullanici aktifKullanici)
        {
            InitializeComponent();

            this.DataContext = new MainWindowViewModel(aktifKullanici);
        }

    }
}