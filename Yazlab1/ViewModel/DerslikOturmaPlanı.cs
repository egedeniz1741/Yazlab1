using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Model;

namespace Yazlab1.ViewModel
{
    public partial class DerslikOturmaPlani 
    {
        public Derslik Derslik { get; set; }
        public ObservableCollection<OturmaPlaniOgrenciDetay> Plan { get; set; }
    }
}
