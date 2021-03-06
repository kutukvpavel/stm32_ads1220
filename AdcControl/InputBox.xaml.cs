﻿using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Media;
using AdcControl.Resources;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для InputBox.xaml
    /// </summary>
    public partial class InputBox : Window, INotifyPropertyChanged
    {
        public InputBox()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string InputText
        {
            get { return txtInput.Text; }
            set { txtInput.Text = value; }
        }

        public string PromptLabel
        {
            get { return (string)lblPrompt.Content; }
            set { lblPrompt.Content = value; }
        }

        private bool _PassedValidation = false;
        public bool PassedValidation
        {
            get { return _PassedValidation; }
            set
            {
                _PassedValidation = value;
                OnPropertyChanged();
            }
        }

        public Func<string, bool> ValidationFunction { get; set; }

        public char[] InvalidCharacters { get; set; }

        //Private

        private static readonly Brush ValidationFailedBrush = new SolidColorBrush(Color.FromArgb(64, 128, 0, 0));
        private static readonly Brush ValidationPassedBrush = new SolidColorBrush(Color.FromArgb(64, 0, 128, 0));

        private void OnPropertyChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void txtInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                e.Handled = !(e.Text.IndexOfAny(InvalidCharacters) < 0);
                if (e.Handled)
                {
                    SystemSounds.Beep.Play();
                }
            }
            catch (Exception ex)
            {
                e.Handled = true;
                App.Logger.Error(Default.msgInputBoxValidationError);
                App.Logger.Info(ex);
            }
        }

        private async void txtInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var t = txtInput.Text;
            PassedValidation = await Task.Run(() =>
            {
                try
                {
                    bool res = true;
                    if (InvalidCharacters != null)
                        res = t.IndexOfAny(InvalidCharacters) < 0;
                    if (res && (ValidationFunction != null))
                        res = ValidationFunction(t);
                    return res;
                }
                catch (Exception ex)
                {
                    App.Logger.Error(Default.msgInputBoxValidationError);
                    App.Logger.Info(ex);
                    return false;
                }
            });
            txtInput.Background = PassedValidation ? ValidationPassedBrush : ValidationFailedBrush;
        }

        private void this_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.Focus();
        }
    }
}
