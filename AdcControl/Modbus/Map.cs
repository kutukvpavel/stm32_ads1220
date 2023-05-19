using System.Collections.Generic;
using System.Collections.Specialized;

namespace AdcControl.Modbus
{
    public class Map
    {
        public Map()
        {
            
        }

        public OrderedDictionary HoldingRegisters { get; } = new OrderedDictionary();
        public OrderedDictionary InputRegisters { get; } = new OrderedDictionary();
        public List<string> ConfigRegisters { get; } = new List<string>();
        public List<string> PollRegisters { get; } = new List<string>();

        public void Clear()
        {
            HoldingRegisters.Clear();
            InputRegisters.Clear();
            ConfigRegisters.Clear();
            PollRegisters.Clear();
        }
        public IRegister GetHolding(string name, int index)
        {
            return HoldingRegisters[name + index.ToString()] as IRegister;
        }
        public IRegister GetInput(string name, int index)
        {
            return InputRegisters[name + index.ToString()] as IRegister;
        }
        public float GetInputFloat(string name, int index = -1)
        {
            if (index >= 0) name += index.ToString();
            return (InputRegisters[name] as Register<DevFloat>).TypedValue.Value;
        }
        public float GetHoldingFloat(string name, int index = -1)
        {
            if (index >= 0) name += index.ToString();
            return (HoldingRegisters[name] as Register<DevFloat>).TypedValue.Value;
        }
        public IRegister GetConfig(AdcConstants.ConfigurationRegisters reg)
        {
            return InputRegisters[AdcConstants.ConfigurationRegisterNames[reg]] as IRegister;
        }
        public ushort GetConfigValue(AdcConstants.ConfigurationRegisters reg)
        {
            return (ushort)(DevUShort)GetConfig(reg).Value;
        }
        public void AddHolding<T>(string name, int num, bool poll = false) where T : IDeviceType, new()
        {
            Add<T>(HoldingRegisters, name, num, poll: poll);
        }
        public void AddInput<T>(string name, int num, bool cfg = false, bool poll = false) where T : IDeviceType, new()
        {
            Add<T>(InputRegisters, name, num, cfg, poll);
        }

        private void Add<T>(OrderedDictionary to, string name, int num, bool cfg = false, bool poll = false) where T : IDeviceType, new()
        {
            int addr = 0;
            if (to.Count > 0)
            {
                var last = to[to.Count - 1] as IRegister; //Do not use ^1 with OrderedDictionary index-based access!! Will return NULL.
                addr = last.Address + last.Length;
            }
            string n = name;
            for (int i = 0; i < num; i++)
            {
                if (num > 1) n = name + i.ToString();
                var item = new Register<T>((ushort)addr, n);
                to.Add(n, item);
                if (cfg) ConfigRegisters.Add(n);
                if (poll) PollRegisters.Add(n);
                addr += item.Length;
            }
        }
    }
}
