﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Yazlab1.Models;

namespace Yazlab1
{
    /// <summary>
    /// OgrenciListesiMenu.xaml etkileşim mantığı
    /// </summary>
    public partial class OgrenciListesiMenu : Window
    {
        public OgrenciListesiMenu(Kullanici aktifKullanici)
        {
            InitializeComponent();
        }
    }
}
