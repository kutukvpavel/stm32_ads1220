using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl.Modbus
{
    public interface IDeviceType
    {
        protected static byte[] GetBytes(ushort[] data, ushort size)
        {
            byte[] buf = new byte[size * sizeof(ushort)];
            for (int i = 0; i < size; i++)
            {
                byte[] b = BitConverter.GetBytes(data[i]);
                for (int j = 0; j < sizeof(ushort); j++)
                {
                    buf[i * sizeof(ushort) + j] = b[j];
                }
            }
            return buf;
        }

        public ushort Size { get; } //in modbus words
        public object Get();
        public void Set(byte[] data);
        public void Set(ushort[] data)
        {
            Set(GetBytes(data, Size));
        }
    }
}
