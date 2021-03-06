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

namespace PhotoTracker
{
    /// <summary>
    /// Interaction logic for Progress.xaml
    /// </summary>
    public partial class Progress : Window
    {
        public float Percentage { set { ProgressBar.Value = ((ProgressBar.Maximum - ProgressBar.Minimum)/100)*value; }}
        
        public Progress(string operation)
        {
            InitializeComponent();

            Operation.Text = operation;
        }

        public void Connect(int connectionId, object target)
        {}
    }
}
