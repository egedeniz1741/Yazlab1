using System;
using System.Windows;

using System.Configuration;
using MySql.Data.MySqlClient; 
namespace Yazlab1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
        
            string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    // Bağlantıyı açmayı dene
                    connection.Open();
                    MessageBox.Show("MySQL veritabanı bağlantısı başarıyla kuruldu!");
                }
                catch (Exception ex)
                {
                    // Hata olursa, hatayı göster
                    MessageBox.Show("Bağlantı BAŞARISIZ OLDU.\n\nHata Mesajı: " + ex.Message);
                }
            }
        }
    }
}