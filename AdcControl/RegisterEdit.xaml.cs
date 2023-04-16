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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для RegisterEdit.xaml
    /// </summary>
    public partial class RegisterEdit : UserControl, INotifyPropertyChanged
    {
        public RegisterEdit()
        {
            InitializeComponent();
            DataContextChanged += RegisterEdit_DataContextChanged;
        }

        private async void RegisterEdit_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsSimpleType) await SimpleRead();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

        public bool IsSimpleType { 
            get
            {
                if (Register == null) return false;
                return Modbus.IDeviceType.SimpleTypes.Contains(Register.Type);
            }
        }
        public Visibility SimpleEditorVisibility => IsSimpleType ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ComplexEditorVisibility => IsSimpleType ? Visibility.Collapsed : Visibility.Visible;
        public bool IsReadonly { get; set; } = false;
        public string ValueText { get; set; } = "";
        public Modbus.IRegister Register => DataContext as Modbus.IRegister;

        public event PropertyChangedEventHandler PropertyChanged;

        private async Task SimpleRead()
        {
            ValueText = (await App.Stm32Ads1220.ReadRegister(Register.Name)).ToString();
            await Task.Run(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueText))));
        }
        private async void btnRead_Click(object sender, RoutedEventArgs e)
        {
            await SimpleRead();
        }

        private async void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            Register.Value.Set(ValueText);
            await App.Stm32Ads1220.WriteRegister(Register);
            await Task.Run(() => btnRead_Click(this, e));
        }

        private async void btnOpenEditor_Click(object sender, RoutedEventArgs e)
        {
            await App.Stm32Ads1220.ReadRegister(Register.Name);
            var dialog = new ComplexRegisterEdit() { DataContext = Register.Value };
            if (dialog.ShowDialog() ?? false) await App.Stm32Ads1220.WriteRegister(Register);
        }
    }
}
