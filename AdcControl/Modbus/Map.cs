using System;
using System.Collections.Generic;
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

        public Dictionary<string, IRegister> HoldingRegisters { get; } = new Dictionary<string, IRegister>();
        public Dictionary<string, IRegister> InputRegisters { get; } = new Dictionary<string, IRegister>();

        public void AddHolding<T>(string name, int num) where T : unmanaged
        {
            Add<T>(HoldingRegisters, name, num);
        }
        public void AddInput<T>(string name, int num) where T : unmanaged
        {
            Add<T>(InputRegisters, name, num);
        }

        private void Add<T>(Dictionary<string, IRegister> to, string name, int num) where T : unmanaged
        {
            int lastAddr = to.Any() ? to.Max(x => x.Value.Address) : -1;
            string n = name;
            for (int i = 0; i < num; i++)
            {
                if (num > 1) n = name + i.ToString(); 
                to.Add(n, new Register<T>(++lastAddr, n));
            }
        }
    }
}
