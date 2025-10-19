using CommunityToolkit.Mvvm.ComponentModel;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
    public partial class DersSecici : ObservableObject
    {
        public Ders Ders { get; }
        [ObservableProperty] public bool _isSelected = true;


        [ObservableProperty]
        public int _sinavSuresi;


        public DersSecici(Ders ders, int varsayilanSure)
        {
            Ders = ders ?? throw new ArgumentNullException(nameof(ders));
            _sinavSuresi = varsayilanSure;
        }
    }
}
