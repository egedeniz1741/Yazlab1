using System;
using System.Windows;
using Microsoft.EntityFrameworkCore.SqlServer; 
using System.Configuration;
using Microsoft.Data.SqlClient;  

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

           
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                   
                    connection.Open();
                    MessageBox.Show("Veritabanı bağlantısı başarıyla kuruldu!");
                }
                catch (Exception ex)
                {
                   
                    MessageBox.Show("Bağlantı BAŞARISIZ OLDU.\n\nHata Mesajı: " + ex.Message);
                }
            }
        }
    }
}