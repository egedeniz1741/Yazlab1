using CommunityToolkit.Mvvm.ComponentModel; // Hata vermiyorsa kalabilir
using CommunityToolkit.Mvvm.Input; // Hata vermiyorsa kalabilir
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel; // Manuel INotifyPropertyChanged için
using System.Linq;
using System.Runtime.CompilerServices; // Manuel INotifyPropertyChanged için
using System.Windows;
using System.Windows.Input; // Manuel ICommand için
using Yazlab1.Model;

using Yazlab1.Views; // Yeni pencereyi açmak için

namespace Yazlab1.ViewModel
{
   
    public class SinavOturmaPlaniViewModel : INotifyPropertyChanged
    {
        // === Alanlar ===
        private AtanmisSinav _secilenSinav;
        private readonly string _sinavAdiBasligi; // Pencere başlığı için hala gerekli

        // === Özellikler ===
        public ObservableCollection<AtanmisSinav> SinavListesi { get; set; }

        public AtanmisSinav SecilenSinav
        {
            get => _secilenSinav;
            set
            {
                if (_secilenSinav != value)
                {
                    _secilenSinav = value;
                    OnPropertyChanged();
                    // Seçim değiştiğinde artık bir şey yapmaya gerek yok,
                    // sadece butonun aktifliğini kontrol edeceğiz.
                    ((RelayCommand)OturmaPlaninaGecCommand).NotifyCanExecuteChanged();
                }
            }
        }

        // === Komutlar ===
        public ICommand OturmaPlaninaGecCommand { get; }

        // === Constructor ===
        public SinavOturmaPlaniViewModel(List<AtanmisSinav> tumSinavlar, string sinavAdiBasligi)
        {
            _sinavAdiBasligi = sinavAdiBasligi;
            var siraliSinavlar = tumSinavlar != null
         ? tumSinavlar.OrderBy(s => s.Tarih).ThenBy(s => s.BaslangicSaati).ToList()
         : new List<AtanmisSinav>();

           
            SinavListesi = new ObservableCollection<AtanmisSinav>(siraliSinavlar);
            OturmaPlaninaGecCommand = new RelayCommand(OturmaPlaninaGec, CanOturmaPlaninaGec);
        }

        // === Metotlar ===

        // Yeni pencereyi açan metot
        private void OturmaPlaninaGec()
        {
            // CanExecute zaten kontrol ettiği için burada tekrar null kontrolüne gerek yok.
            OturmaPlaniGosterWindow gosterWindow = new OturmaPlaniGosterWindow(SecilenSinav, _sinavAdiBasligi);
            gosterWindow.Show();
        }

        // Butonun aktif olup olmayacağını belirleyen metot
        private bool CanOturmaPlaninaGec()
        {
            return SecilenSinav != null; // Sadece bir sınav seçiliyse aktif olsun
        }

        // PDF ile ilgili tüm metotlar kaldırıldı.
        // Oturma planı oluşturma mantığı da kaldırıldı (yeni ViewModel'de olacak).

        // === INotifyPropertyChanged Implementasyonu ===
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // === Basit RelayCommand Implementasyonu ===
        // Bu sınıfın burada veya ayrı bir dosyada olması gerekir.
        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;
            public event EventHandler CanExecuteChanged;
            public RelayCommand(Action execute, Func<bool> canExecute = null) { _execute = execute ?? throw new ArgumentNullException(nameof(execute)); _canExecute = canExecute; }
            public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
            public void Execute(object parameter) => _execute();
            public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}