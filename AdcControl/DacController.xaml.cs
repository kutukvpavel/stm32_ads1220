using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        protected DacChannel Channel => (DacChannel)DataContext;

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Channel.WriteSetpoint();
            await Channel.ReadSetpoint();
        }

        private async void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    await Channel.ReadSetpoint();
                    break;
                case Key.Enter:
                    Channel.VoltageText = txtSetpoint.Text;
                    await Channel.WriteSetpoint();
                    await Channel.ReadSetpoint();
                    break;
                default: break;
            }
        }

        private async void txtDepoSetpoint_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    await Channel.ReadDepolarizationSetpoint();
                    break;
                case Key.Enter:
                    Channel.DepolarizationSetpointText = txtDepoSetpoint.Text;
                    await Channel.WriteDepolarizationSetpoint();
                    await Channel.ReadDepolarizationSetpoint();
                    break;
                default: break;
            }
        }

        private async void btnDepoSetpoint_Click(object sender, RoutedEventArgs e)
        {
            await Channel.WriteDepolarizationSetpoint();
            await Channel.ReadDepolarizationSetpoint();
        }

        private async void btnDepoPercent_Click(object sender, RoutedEventArgs e)
        {
            await Channel.WriteDepolarizationPercent();
            await Channel.ReadDepolarizationPercent();
        }

        private async void txtDepoPrecent_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    await Channel.ReadDepolarizationPercent();
                    break;
                case Key.Enter:
                    Channel.DepolarizationPercentText = txtDepoPrecent.Text;
                    await Channel.WriteDepolarizationPercent();
                    await Channel.ReadDepolarizationPercent();
                    break;
                default: break;
            }
        }

        private async void txtDepoInterval_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    await Channel.ReadDepolarizationInterval();
                    break;
                case Key.Enter:
                    Channel.DepolarizationIntervalText = txtDepoInterval.Text;
                    await Channel.WriteDepolarizationInterval();
                    await Channel.ReadDepolarizationInterval();
                    break;
                default: break;
            }
        }

        private async void btnDepoInterval_Click(object sender, RoutedEventArgs e)
        {
            await Channel.WriteDepolarizationInterval();
            await Channel.ReadDepolarizationInterval();
        }

        private async void btnCorrInterval_Click(object sender, RoutedEventArgs e)
        {
            await Channel.WriteCorrectionInterval();
            await Channel.ReadCorrectionInterval();
        }

        private async void txtCorrInterval_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    await Channel.ReadCorrectionInterval();
                    break;
                case Key.Enter:
                    Channel.CorrectionIntervalText = txtCorrInterval.Text;
                    await Channel.WriteCorrectionInterval();
                    await Channel.ReadCorrectionInterval();
                    break;
                default: break;
            }
        }
    }
}
