using System;
using System.ComponentModel;

namespace AdcControl.Modbus
{
    public interface IDeviceType : INotifyPropertyChanged
    {
        public static Type[] SimpleTypes { get; } =
        {
            typeof(DevFloat),
            typeof(DevULong),
            typeof(DevUShort)
        };

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
        protected static ushort[] BytesToWords(byte[] data, ushort size)
        {
            ushort[] buf = new ushort[size];
            for (int i = 0; i < size; i++)
            {
                buf[i] = BitConverter.ToUInt16(data, i * sizeof(ushort));
            }
            return buf;
        }

        public ushort Size { get; } //in modbus words
        public object Get();
        public ushort[] GetWords();
        public void Set(byte[] data);
        public void Set(ushort[] data)
        {
            Set(GetBytes(data, Size));
        }
        public void Set(string data);
    }
}
