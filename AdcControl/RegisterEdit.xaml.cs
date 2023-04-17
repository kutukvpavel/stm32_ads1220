using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
            if (DataContext == null || Register == null) return;
            if (IsSimpleType) await SimpleRead();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            Register.PropertyChanged += Register_PropertyChanged;
        }

        private void Register_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Register.Value)) Dispatcher.InvokeAsync(() => ConvertToText());
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
        public bool IsNotReadonly => !IsReadonly;
        public string ValueText { get; set; } = "";
        public Modbus.IRegister Register => DataContext as Modbus.IRegister;

        public event PropertyChangedEventHandler PropertyChanged;

        private void ConvertToText()
        {
            Dispatcher.InvokeAsync(() =>
            {
                ValueText = Register.Value.ToString();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueText)));
            });
        }
        private async Task SimpleRead()
        {
            await App.Stm32Ads1220.ReadRegister(Register.Name);
            ConvertToText();
        }
        private async void btnRead_Click(object sender, RoutedEventArgs e)
        {
            await SimpleRead();
        }

        private async void btnWrite_Click(object sender, RoutedEventArgs e)
        {
            Register.Value.Set(ValueText);
            await App.Stm32Ads1220.WriteRegister(Register);
            await SimpleRead();
        }

        private async void btnOpenEditor_Click(object sender, RoutedEventArgs e)
        {
            await App.Stm32Ads1220.ReadRegister(Register.Name);
            var dialog = new ComplexRegisterEdit() { DataContext = Register.Value };
            if (dialog.ShowDialog() ?? false) await App.Stm32Ads1220.WriteRegister(Register);
        }
    }
}
