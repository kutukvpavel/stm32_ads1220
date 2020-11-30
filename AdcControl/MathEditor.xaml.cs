using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using org.mariuszgromada.math.mxparser;

namespace AdcControl
{
    public partial class MathEditor : Window, INotifyPropertyChanged
    {
        public MathEditor()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool PassedValidation
        {
            get => _PassedValidation;
            set
            {
                _PassedValidation = value;
                OnPropertyChanged();
            }
        }

        //Private
        private bool _PassedValidation = false;

        private 

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }


    }
}
