﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для ComplexRegisterEdit.xaml
    /// </summary>
    public partial class ComplexRegisterEdit : Window
    {
        public ComplexRegisterEdit()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
