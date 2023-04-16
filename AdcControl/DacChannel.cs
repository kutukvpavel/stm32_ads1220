using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AdcControl.Properties;
using System.ComponentModel;

namespace AdcControl
{
    public class DacChannel : INotifyPropertyChanged
    {
        public DacChannel(Controller owner, int index)
        {
            Owner = owner;
            Index = index;
        }

        public Controller Owner { get; }
        public int Index { get; }
        public float Setpoint { get; private set; } = 0;
        public string ChannelLabel => $"DAC ch. #{Index}";
        public string VoltageText { get; set; } = "0";

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task Read()
        {
            await Owner.ReadDacSetpoint(Index);
            Setpoint = ((Modbus.DevFloat)
                (Owner.RegisterMap.HoldingRegisters[AdcConstants.DacSetpointNameTemplate + Index.ToString()] as Modbus.IRegister)
                .Value).Value;
            VoltageText = Setpoint.ToString(Settings.ViewSettings.CalculatedYNumberFormat);
            await Task.Run(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VoltageText))));
        }
        public async Task Write()
        {
            if (float.TryParse(VoltageText, out float f))
            {
                await Owner.WriteDacSetpoint(Index, f);
            }
            await Read();
        }
    }
}
