using System;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        public InputBox()
        {
            InitializeComponent();
        }

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

        public Func<string, bool> ValidationFunction { get; set; }

        public char[] InvalidCharacters { get; set; }

        //Private

        private static readonly Brush ValidationFailedBrush = new SolidColorBrush(Color.FromArgb(64, 128, 0, 0));
        private static readonly Brush ValidationPassedBrush = new SolidColorBrush(Color.FromArgb(64, 0, 128, 0));

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

        private async void txtInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            bool res = false;
            await Task.Run(() =>
            {
                if (InvalidCharacters != null)
                    res = e.Text.IndexOfAny(InvalidCharacters) > -1;
                if ((!res) && (ValidationFunction != null))
                    res = ValidationFunction(e.Text);
            });
            e.Handled = res;
            txtInput.Background = res ? ValidationFailedBrush : ValidationPassedBrush;
        }
    }
}
