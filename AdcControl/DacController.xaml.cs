using System;
using System.Collections.Generic;
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

namespace AdcControl
{
    /// <summary>
    /// Логика взаимодействия для DacController.xaml
    /// </summary>
    public partial class DacController : UserControl
    {
        public DacController()
        {
            InitializeComponent();
        }

        private async Task Write()
        {
            var ch = (DacChannel)DataContext;
            ch.VoltageText = txtValue.Text;
            await ch.Write();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Write();
        }

        private async void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            await Write();
        }

        private async void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.F5) return;
            var ch = (DacChannel)DataContext;
            
            await ch.Read();
        }
    }
}
