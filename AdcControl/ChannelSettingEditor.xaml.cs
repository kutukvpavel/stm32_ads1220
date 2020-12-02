using System;
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для CollectionSettingEditor.xaml
    /// </summary>
    public partial class ChannelSettingEditor : Window, INotifyPropertyChanged
    {
        public ChannelSettingEditor()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _HelpText = null;
        public string HelpText
        {
            get => _HelpText;
            set
            {
                _HelpText = value;
                OnPropertyChanged("HelpText");
                OnPropertyChanged("IsHelpTextPresent");
            }
        }

        public bool IsHelpTextPresent
        {
            get => _HelpText != null;
        }

        public Func<string, bool> ValueValidator { get; set; } = (x) => { return true; };

        public StringCollection ParsedInput { get; private set; }

        private bool _PassedValidation = false;
        public bool PassedValidation
        {
            get { return _PassedValidation; }
            private set
            {
                _PassedValidation = value;
                OnPropertyChanged("PassedValidation");
            }
        }

        public void SetDefaultInputValue(StringCollection collection)
        {
            foreach (var item in collection)
            {
                txtInput.AppendText(item + Environment.NewLine);
            }
        }

        //Private

        private static readonly Brush ValidationFailedBrush = new SolidColorBrush(Color.FromArgb(64, 128, 0, 0));
        private static readonly Brush ValidationPassedBrush = new SolidColorBrush(Color.FromArgb(64, 0, 128, 0));

        private void OnPropertyChanged(string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ReturnOK()
        {
            DialogResult = true;
            Close();
        }

        private async Task<bool> ParseInput(string data)
        {
            return await Task.Run(() =>
            {
                var c = new StringCollection();
                try
                {
                    c.AddRange(data.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
                    var res = DictionarySerializer.Parse(c, ValueValidator);
                    ParsedInput = c;
                    return res.Values.All(x => x);
                }
                catch (Exception)
                {
                    return false;
                }
            });
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            ReturnOK();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void txtInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    ReturnOK();
                }
            }
        }

        private async void txtInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            PassedValidation = await ParseInput(txtInput.Text);
            txtInput.Background = PassedValidation ? ValidationPassedBrush : ValidationFailedBrush;
        }

        private void this_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.Focus();
        }
    }
}
