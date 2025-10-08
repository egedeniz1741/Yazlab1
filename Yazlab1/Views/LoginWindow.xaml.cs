using System;
using System.Linq;
using System.Windows;
using Yazlab1.Data;

namespace Yazlab1.Views 
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            
            InitializeComponent();
        }

        
        public void LoginButton_Click(object sender, RoutedEventArgs e)
        {
           
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

          
            ErrorMessageTextBlock.Text = "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ErrorMessageTextBlock.Text = "E-posta ve şifre alanları boş bırakılamaz.";
                return;
            }

            try
            {

                using var dbContext = new SinavTakvimDbContext();

                var user = dbContext.Kullanicilar.FirstOrDefault(u => u.Eposta == email);

                if (user != null)
                {

                    if (user.SifreHash == password) //sonra hashli sifreyle kontrol edeceğiz burayı
                    {

                        MainWindow mainWindow = new();
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        ErrorMessageTextBlock.Text = "Hatalı şifre girdiniz.";
                    }
                }
                else
                {
                    ErrorMessageTextBlock.Text = "Bu e-postaya sahip bir kullanıcı bulunamadı.";
                }
            }
            catch (Exception ex)
            {
            
                ErrorMessageTextBlock.Text = "Bir hata oluştu: " + ex.Message;
            }
        }
    }
}