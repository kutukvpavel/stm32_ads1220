using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AdcControl.Modbus
{
    public class Register<T> : IRegister where T : IDeviceType
    {
        static Dictionary<Type, ushort> SizeOfRegister = new Dictionary<Type, ushort>() //In modbus words
        {
            { typeof(ushort), 1 },
            { typeof(float), 2 }
        };

        public Register(ushort addr, string name)
        {
            if (!BitConverter.IsLittleEndian) throw new NotImplementedException("Non-LE archs not supported.");
            Address = addr;
            Name = name;
        }

        public Type Type => typeof(T);
        public object Value => TypedValue;
        public T TypedValue { get; private set; }
        public ushort Address { get; }
        public ushort Length => SizeOfRegister[typeof(T)];
        public string Name { get; }

        public void Set(params ushort[] regs)
        {
            Set(0, regs);
        }
        public void Set(int startIndex, ushort[] regs)
        {
            ushort[] buf = new ushort[regs.Length - startIndex];
            Array.Copy(regs, startIndex, buf, 0, buf.Length);

            TypedValue.Set(buf);
        }
    }
}
