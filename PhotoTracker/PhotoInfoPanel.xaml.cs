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
using System.Windows.Navigation;
using System.Windows.Shapes;
using PropertyChanged;

namespace PhotoTracker
{
    [ImplementPropertyChanged]
    public partial class PhotoInfoPanel : UserControl
    {
        public LogMarker Marker { get; set; }

        public PhotoInfoPanel()
        {
            InitializeComponent();
            
            DataContext = this;
        }

        public void Connect(int connectionId, object target)
        {}
    }
}
