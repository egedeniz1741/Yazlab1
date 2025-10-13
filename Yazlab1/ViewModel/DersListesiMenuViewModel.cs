using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yazlab1.Data;
using Yazlab1.Model;


namespace Yazlab1.ViewModel
{
    public partial class DersListesiMenuViewModel : ObservableObject
    {
        private readonly SinavTakvimDbContext _dbContext = new SinavTakvimDbContext();
        private readonly int _aktifKullaniciBolumId;


        public DersListesiMenuViewModel(Kullanici aktifKullanici)
        {
            _aktifKullaniciBolumId = aktifKullanici.BolumID.Value;
        }
    }
}
