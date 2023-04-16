using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AdcControl
{
    public class StatusBit : INotifyPropertyChanged
    {
        public StatusBit(Controller owner, AdcConstants.Coils c)
        {
            Coil = c;
            Owner = owner;
        }
        public StatusBit(Controller owner, AdcConstants.Coils c, bool v) : this(owner, c)
        {
            _State = v;
        }

        public string Name => Enum.GetName(typeof(AdcConstants.Coils), Coil);
        public Controller Owner { get; }
        public AdcConstants.Coils Coil { get; }
        private bool _State = false;
        public bool State 
        { 
            get => _State;
            set
            {
                _State = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task Read()
        {
            State = (await Owner.ReadCoils())[(ushort)Coil];
        }
        public async Task Write()
        {
            await Owner.WriteCoil(Coil, State);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Task.Run(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}
