using CommunityToolkit.Mvvm.ComponentModel; 
using CommunityToolkit.Mvvm.Input; 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel; 
using System.Linq;
using System.Runtime.CompilerServices; 
using System.Windows;
using System.Windows.Input; 
using Yazlab1.Model;

using Yazlab1.Views; 

namespace Yazlab1.ViewModel
{
   
    public class SinavOturmaPlaniViewModel : INotifyPropertyChanged
    {
        
        private AtanmisSinav _secilenSinav;
        private readonly string _sinavAdiBasligi; 

      
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
                  
                    ((RelayCommand)OturmaPlaninaGecCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public ICommand OturmaPlaninaGecCommand { get; }

       
        public SinavOturmaPlaniViewModel(List<AtanmisSinav> tumSinavlar, string sinavAdiBasligi)
        {
            _sinavAdiBasligi = sinavAdiBasligi;
            var siraliSinavlar = tumSinavlar != null
         ? tumSinavlar.OrderBy(s => s.Tarih).ThenBy(s => s.BaslangicSaati).ToList()
         : new List<AtanmisSinav>();

           
            SinavListesi = new ObservableCollection<AtanmisSinav>(siraliSinavlar);
            OturmaPlaninaGecCommand = new RelayCommand(OturmaPlaninaGec, CanOturmaPlaninaGec);
        }

      

       
        private void OturmaPlaninaGec()
        {
            
            OturmaPlaniGosterWindow gosterWindow = new OturmaPlaniGosterWindow(SecilenSinav, _sinavAdiBasligi);
            gosterWindow.Show();
        }

       
        private bool CanOturmaPlaninaGec()
        {
            return SecilenSinav != null;
        }

        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
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