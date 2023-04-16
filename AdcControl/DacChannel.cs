using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AdcControl.Properties;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        private string _VoltageText = "0";
        public string VoltageText
        {
            get => _VoltageText; 
            set
            {
                _VoltageText = value;
                OnPropertyChanged();
            }
        }
        private string _DepolarizationPercentText = "0";
        public string DepolarizationPercentText
        {
            get => _DepolarizationPercentText; 
            set
            {
                _DepolarizationPercentText = value;
                OnPropertyChanged();
            }
        }
        public float DepolarizationPercent { get; private set; } = 0;
        public float DepolarizationInterval { get; private set; } = 1;
        private string _DepolarizationIntervalText = "5";
        public string DepolarizationIntervalText
        {
            get => _DepolarizationIntervalText;
            set
            {
                _DepolarizationIntervalText = value;
                OnPropertyChanged();
            }
        }
        public float DepolarizationSetpoint { get; private set; }
        private string _DepolarizationSetpointText = "0";
        public string DepolarizationSetpointText
        {
            get => _DepolarizationSetpointText; 
            set
            {
                _DepolarizationSetpointText = value;
                OnPropertyChanged();
            }
        }
        public float CorrectionInterval { get; private set; } = 1;
        private string _CorrectionIntervalText = "1";
        public string CorrectionIntervalText
        {
            get => _CorrectionIntervalText;
            set
            {
                _CorrectionIntervalText = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task ReadSetpoint()
        {
            Setpoint = await ReadFloatHoldingRegister(AdcConstants.DacSetpointNameTemplate);
            VoltageText = Setpoint.ToString(Settings.ViewSettings.CalculatedYNumberFormat);
        }
        public async Task WriteSetpoint()
        {
            if (float.TryParse(VoltageText, out float f))
            {
                await WriteFloatHoldingRegister(AdcConstants.DacSetpointNameTemplate, f);
            }
            await ReadSetpoint();
        }
        public async Task ReadDepolarizationPercent()
        {
            DepolarizationPercent = await ReadFloatHoldingRegister(AdcConstants.DacDepoPercentNameTemplate);
            DepolarizationPercentText = DepolarizationPercent.ToString("F1");
        }
        public async Task WriteDepolarizationPercent()
        {
            if (float.TryParse(DepolarizationPercentText, out float f))
            {
                await WriteFloatHoldingRegister(AdcConstants.DacDepoPercentNameTemplate, f);
            }
            await ReadDepolarizationPercent();
        }
        public async Task ReadDepolarizationInterval()
        {
            DepolarizationInterval = await ReadMsHoldingRegister(AdcConstants.DacDepoIntervalNameTemplate);
            DepolarizationIntervalText = DepolarizationInterval.ToString("F2");
        }
        public async Task WriteDepolarizationInterval()
        {
            if (float.TryParse(DepolarizationIntervalText, out float f))
            {
                await WriteMsHoldingRegister(AdcConstants.DacDepoIntervalNameTemplate, f);
            }
            await ReadDepolarizationInterval();
        }
        public async Task ReadDepolarizationSetpoint()
        {
            DepolarizationSetpoint = await ReadFloatHoldingRegister(AdcConstants.DacDepoSetpointNameTemplate);
            DepolarizationSetpointText = DepolarizationSetpoint.ToString(Settings.ViewSettings.CalculatedYNumberFormat);
        }
        public async Task WriteDepolarizationSetpoint()
        {
            if (float.TryParse(DepolarizationSetpointText, out float f))
            {
                await WriteFloatHoldingRegister(AdcConstants.DacDepoSetpointNameTemplate, f);
            }
            await ReadDepolarizationSetpoint();
        }
        public async Task ReadCorrectionInterval()
        {
            CorrectionInterval = await ReadMsHoldingRegister(AdcConstants.DacCorrectionIntervalNameTemplate);
            CorrectionIntervalText = CorrectionInterval.ToString("F2");
        }
        public async Task WriteCorrectionInterval()
        {
            if (float.TryParse(CorrectionIntervalText, out float f))
            {
                await WriteMsHoldingRegister(AdcConstants.DacCorrectionIntervalNameTemplate, f);
            }
            await ReadCorrectionInterval();
        }

        //Private
        private async Task WriteFloatHoldingRegister(string nameTemplate, float value)
        {
            var reg = Owner.RegisterMap.GetHolding(nameTemplate, Index);
            await Owner.WriteRegister(reg.Name, (Modbus.DevFloat)value);
        }
        private async Task<float> ReadFloatHoldingRegister(string nameTemplate)
        {
            var reg = Owner.RegisterMap.GetHolding(nameTemplate, Index);
            return (float)await Owner.ReadRegister<Modbus.DevFloat>(reg.Name);
        }
        private async Task WriteMsHoldingRegister(string nameTemplate, float value)
        {
            var reg = Owner.RegisterMap.GetHolding(nameTemplate, Index);
            uint ms = (uint)MathF.Round(value * 1000.0f); //Instrument units = mS
            await Owner.WriteRegister(reg.Name, (Modbus.DevULong)ms);
        }
        private async Task<float> ReadMsHoldingRegister(string nameTemplate)
        {
            var reg = Owner.RegisterMap.GetHolding(nameTemplate, Index);
            return ((uint)await Owner.ReadRegister<Modbus.DevULong>(reg.Name)) / 1000.0f;
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Task.Run(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
