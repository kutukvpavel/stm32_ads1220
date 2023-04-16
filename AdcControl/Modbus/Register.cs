using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AdcControl.Modbus
{
    public class Register<T> : IRegister where T : IDeviceType, new()
    {
        public Register(ushort addr, string name)
        {
            if (!BitConverter.IsLittleEndian) throw new NotImplementedException("Non-LE archs not supported.");
            Address = addr;
            Name = name;
        }

        public Type Type => typeof(T);
        public object Value => TypedValue;
        public T TypedValue { get; private set; } = new T();
        public ushort Address { get; }
        public ushort Length => TypedValue.Size; //In modbus words
        public string Name { get; }

        public ushort[] GetWords(object origin)
        {
            return TypedValue.GetWords(origin);
        }
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
