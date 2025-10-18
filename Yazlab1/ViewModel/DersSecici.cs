using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Model;

namespace Yazlab1.ViewModel
{
    public partial class DersSecici : ObservableObject
    {
        public Ders Ders { get; }

        [ObservableProperty]
        public bool _isSelected; 

        public DersSecici(Ders ders)
        {
            Ders = ders;
            _isSelected = true;
        }

    }
}
