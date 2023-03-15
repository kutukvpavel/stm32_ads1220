using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AdcControl.Modbus
{
    public class Register<T> : IRegister where T : unmanaged
    {
        static Dictionary<Type, int> SizeOfRegister = new Dictionary<Type, int>() //In modbus words
        {
            { typeof(ushort), 1 },
            { typeof(float), 2 }
        };

        public Register(int addr, string name)
        {
            if (!BitConverter.IsLittleEndian) throw new NotImplementedException("Non-LE archs not supported.");
            Address = addr;
            Name = name;
        }

        public Type Type => typeof(T);
        public object Value => TypedValue;
        public T TypedValue { get; private set; }
        public int Address { get; }
        public int Length => SizeOfRegister[typeof(T)];
        public string Name { get; }

        public void Set(params ushort[] regs)
        {
            byte[] buf = new byte[Length * sizeof(ushort)];
            for (int i = 0; i < Length; i++)
            {
                byte[] b = BitConverter.GetBytes(regs[i]);
                for (int j = 0; j < sizeof(ushort); j++)
                {
                    buf[i * sizeof(ushort) + j] = b[j];
                }
            }
            if (typeof(T) == typeof(float))
            {
                TypedValue = (T)(object)BitConverter.ToSingle(buf);
                return;
            }
            if (typeof(T) == typeof(ushort))
            {
                TypedValue = (T)(object)BitConverter.ToUInt16(buf);
                return;
            }
            if (typeof(T) == typeof(int))
            {
                TypedValue = (T)(object)BitConverter.ToInt32(buf);
                return;
            }
            throw new NotImplementedException("This register type is not supported.");
        }
    }
}
