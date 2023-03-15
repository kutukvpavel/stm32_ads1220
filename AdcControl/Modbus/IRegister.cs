using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl.Modbus
{
    public interface IRegister
    {
        public object Value { get; }
        public Type Type { get; }

        public int Address { get; }
        public int Length { get; }
        public string Name { get; }

        public void Set(params ushort[] regs);
    }
}
