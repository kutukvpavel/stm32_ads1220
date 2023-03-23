using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl.Modbus
{
    public struct DevFloat : IDeviceType
    {
        public float Value;

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

    public struct DevUshort : IDeviceType
    {
        public ushort Value;

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

    public struct AdcChannelCal : IDeviceType
    {
        public float K;
        public float B;
        public ushort Invert;

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

    public struct DacCal : IDeviceType
    {
        public float K;
        public float B;
        public float Current_K;
        public float Current_B;

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

    public struct AioCal : IDeviceType
    {
        public float K;
        public float B;

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

    public struct MotorParams : IDeviceType
    {
        public float RateToSpeed;
        public ushort Microsteps;
        public ushort Teeth;
        public ushort InvertEnable;
        public ushort InvertError;
        public ushort Direction;

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
