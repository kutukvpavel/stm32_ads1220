using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;
using System.Media;

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
            e.Handled = !(e.Text.IndexOfAny(InvalidCharacters) < 0);
            if (e.Handled)
            {
                SystemSounds.Beep.Play();
            }
        }

        private async void txtInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            PassedValidation = await Task.Run(() =>
            {
                bool res = true;
                if (InvalidCharacters != null)
                    res = txtInput.Text.IndexOfAny(InvalidCharacters) < 0;
                if (res && (ValidationFunction != null))
                    res = ValidationFunction(txtInput.Text);
                return res;
            });
            txtInput.Background = PassedValidation ? ValidationPassedBrush : ValidationFailedBrush;
        }
    }
}
