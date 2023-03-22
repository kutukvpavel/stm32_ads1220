﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Linq;
using System.IO;

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
        public float GetInputFloat(string name)
        {
            return (InputRegisters[name] as Register<DevFloat>).TypedValue.Value;
        }
        public IRegister GetConfig(AdcConstants.ConfigurationRegisters reg)
        {
            return InputRegisters[AdcConstants.ConfigurationRegisterNames[reg]] as IRegister;
        }
        public ushort GetConfigValue(AdcConstants.ConfigurationRegisters reg)
        {
            return (ushort)GetConfig(reg).Value; //TODO: Add explicit cast operators to DevTypes
        }
        public void AddHolding<T>(string name, int num) where T : IDeviceType
        {
            Add<T>(HoldingRegisters, name, num);
        }
        public void AddInput<T>(string name, int num, bool cfg = false, bool poll = false) where T : IDeviceType
        {
            Add<T>(InputRegisters, name, num, cfg);
        }

        private void Add<T>(OrderedDictionary to, string name, int num, bool cfg = false, bool poll = false) where T : IDeviceType
        {
            int lastAddr = -1;
            if (to.Count > 0)
            {
                lastAddr = (to[to.Count - 1] as IRegister).Address;
            }
            string n = name;
            for (int i = 0; i < num; i++)
            {
                if (num > 1) n = name + i.ToString(); 
                to.Add(n, new Register<T>((ushort)++lastAddr, n));
                if (cfg) ConfigRegisters.Add(n);
                if (poll) PollRegisters.Add(n);
            }
        }
    }
}
