using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl.Modbus
{
    public class DevFloat : IDeviceType
    {
        public float Value { get; private set; }

        public ushort Size => 2;
        public object Get()
        {
            return this;
        }

        public void Set(byte[] data)
        {
            Value = BitConverter.ToSingle(data);
        }

        public static explicit operator float(DevFloat v) => v.Value;
    }

    public class DevUshort : IDeviceType
    {
        public ushort Value { get; private set; }

        public ushort Size => 1;
        public object Get()
        {
            return this;
        }

        public void Set(byte[] data)
        {
            Value = BitConverter.ToUInt16(data);
        }

        public static explicit operator ushort(DevUshort v) => v.Value;
    }

    public class AdcChannelCal : IDeviceType
    {
        public float K { get; private set; }
        public float B { get; private set; }
        public ushort Invert { get; private set; }

        public ushort Size => 2 * 2 + 1;
        public object Get()
        {
            return this;
        }

        public void Set(byte[] data)
        {
            int startIndex = 0;
            K = BitConverter.ToSingle(data, startIndex);
            B = BitConverter.ToSingle(data, startIndex += sizeof(float));
            Invert = BitConverter.ToUInt16(data, startIndex += sizeof(float));
        }
    }

    public class DacCal : IDeviceType
    {
        public float K { get; private set; }
        public float B { get; private set; }
        public float Current_K { get; private set; }
        public float Current_B { get; private set; }

        public ushort Size => 2 * 4;
        public object Get()
        {
            return this;
        }

        public void Set(byte[] data)
        {
            int startIndex = 0;
            K = BitConverter.ToSingle(data, startIndex);
            B = BitConverter.ToSingle(data, startIndex += sizeof(float));
            Current_K = BitConverter.ToSingle(data, startIndex += sizeof(float));
            Current_B = BitConverter.ToSingle(data, startIndex += sizeof(float));
        }
    }

    public class AioCal : IDeviceType
    {
        public float K { get; private set; }
        public float B { get; private set; }

        public ushort Size => 2 * 2;
        public object Get()
        {
            return this;
        }

        public void Set(byte[] data)
        {
            K = BitConverter.ToSingle(data);
            B = BitConverter.ToSingle(data, sizeof(float));
        }
    }

    public class MotorParams : IDeviceType
    {
        public float RateToSpeed { get; private set; }
        public ushort Microsteps { get; private set; }
        public ushort Teeth { get; private set; }
        public ushort InvertEnable { get; private set; }
        public ushort InvertError { get; private set; }
        public ushort Direction { get; private set; }

        public ushort Size => 2 + 1 * 5;
        public object Get()
        {
            return this;
        }

        public void Set(byte[] data)
        {
            int startIndex = 0;
            RateToSpeed = BitConverter.ToSingle(data, startIndex);
            Microsteps = BitConverter.ToUInt16(data, startIndex += sizeof(float));
            Teeth = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            InvertEnable = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            InvertError = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            Direction = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
        }
    }
}
