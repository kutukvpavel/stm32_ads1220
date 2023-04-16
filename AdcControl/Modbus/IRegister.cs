﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl.Modbus
{
    public interface IRegister
    {
        public IDeviceType Value { get; }
        public Type Type { get; }

        public ushort Address { get; }
        public ushort Length { get; }
        public string Name { get; }

        public ushort[] GetWords();
        public void Set(params ushort[] regs);
    }
}
