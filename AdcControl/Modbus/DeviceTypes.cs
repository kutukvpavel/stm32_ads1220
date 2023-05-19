using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace AdcControl.Modbus
{
    public abstract class DevTypeBase : IDeviceType
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public abstract ushort Size { get; }

        public abstract object Get();
        public abstract ushort[] GetWords();
        public abstract void Set(byte[] data, int startIndex = 0);
        public abstract void Set(string data);

        protected void OnPropertyChanged(string name = null)
        {
            Task.Run(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)));
        }
    }

    public class DevFloat : DevTypeBase
    {
        public float Value { get; set; }

        public override ushort Size => 2;

        public override object Get()
        {
            return this;
        }
        public override ushort[] GetWords()
        {
            return IDeviceType.BytesToWords(BitConverter.GetBytes(Value), Size);
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            Value = BitConverter.ToSingle(data, startIndex);
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            Value = float.Parse(data, System.Globalization.NumberStyles.Float);
            OnPropertyChanged();
        }
        public override string ToString()
        {
            return Value.ToString(MathF.Abs(Value) < 0.001 ? "E6" : "F6");
        }

        public static explicit operator float(DevFloat v) => v.Value;
        public static explicit operator DevFloat(float v) => new DevFloat() { Value = v };
    }

    public class DevUShort : DevTypeBase
    {
        public ushort Value { get; set; }

        public override ushort Size => 1;

        public override object Get()
        {
            return this;
        }
        public override ushort[] GetWords()
        {
            return IDeviceType.BytesToWords(BitConverter.GetBytes(Value), Size);
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            Value = BitConverter.ToUInt16(data, startIndex);
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            Value = ushort.Parse(data);
            OnPropertyChanged();
        }
        public override string ToString()
        {
            return Value.ToString();
        }

        public static explicit operator ushort(DevUShort v) => v.Value;
        public static explicit operator DevUShort(ushort v) => new DevUShort() { Value = v };
    }

    public class DevULong : DevTypeBase
    {
        public uint Value { get; set; }

        public override ushort Size => 2;
        public override object Get()
        {
            return this;
        }
        public override ushort[] GetWords()
        {
            return IDeviceType.BytesToWords(BitConverter.GetBytes(Value), Size);
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            Value = BitConverter.ToUInt32(data, startIndex);
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            Value = uint.Parse(data);
            OnPropertyChanged();
        }
        public override string ToString()
        {
            return Value.ToString();
        }

        public static explicit operator uint(DevULong v) => v.Value;
        public static explicit operator DevULong(uint v) => new DevULong() { Value = v };
    }

    public class AdcChannelCal : DevTypeBase
    {
        public float K { get; set; }
        public float B { get; set; }
        public ushort Invert { get; set; }

        public override ushort Size => 2 * 2 + 1 + 1; //32-bit trailing alignment!
        public override object Get()
        {
            return this;
        }
        public override ushort[] GetWords()
        {
            throw new NotImplementedException();
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            K = BitConverter.ToSingle(data, startIndex);
            B = BitConverter.ToSingle(data, startIndex += sizeof(float));
            Invert = BitConverter.ToUInt16(data, startIndex += sizeof(float));
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            throw new NotImplementedException();
        }
    }

    public class DacCal : DevTypeBase
    {
        public float K { get; set; }
        public float B { get; set; }
        public float Current_K { get; set; }
        public float Current_B { get; set; }

        public override ushort Size => 2 * 4;
        public override object Get()
        {
            return this;
        }
        public override ushort[] GetWords()
        {
            throw new NotImplementedException();
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            K = BitConverter.ToSingle(data, startIndex);
            B = BitConverter.ToSingle(data, startIndex += sizeof(float));
            Current_K = BitConverter.ToSingle(data, startIndex += sizeof(float));
            Current_B = BitConverter.ToSingle(data, startIndex += sizeof(float));
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            throw new NotImplementedException();
        }
    }

    public class AioCal : DevTypeBase
    {
        public float K { get; set; }
        public float B { get; set; }

        public override ushort Size => 2 * 2;
        public override object Get()
        {
            return this;
        }

        public override ushort[] GetWords()
        {
            throw new NotImplementedException();
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            K = BitConverter.ToSingle(data);
            B = BitConverter.ToSingle(data, sizeof(float));
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            throw new NotImplementedException();
        }
    }

    public class MotorParams : DevTypeBase
    {
        public MotorParams()
        {
            Fields = new IDeviceType[]
            {
                RateToSpeed,
                Microsteps,
                Teeth,
                InvertEnable,
                InvertError,
                Direction
            };
        }

        protected IDeviceType[] Fields;

        public DevFloat RateToSpeed { get; set; } = new DevFloat();
        public DevUShort Microsteps { get; set; } = new DevUShort();
        public DevUShort Teeth { get; set; } = new DevUShort();
        public DevUShort InvertEnable { get; set; } = new DevUShort();
        public DevUShort InvertError { get; set; } = new DevUShort();
        public DevUShort Direction { get; set; } = new DevUShort();

        public override ushort Size => 2 + 1 * 5 + 1; //Trailing padding (32-bit alignment)!
        public override object Get()
        {
            return this;
        }
        public override ushort[] GetWords()
        {
            ushort[] buf = new ushort[Size];
            int i = 0;
            foreach (var item in Fields)
            {
                item.GetWords().CopyTo(buf, i);
                i += item.Size;
            }
            return buf;
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            foreach (var item in Fields)
            {
                item.Set(data, startIndex);
                startIndex += item.Size * sizeof(ushort);
            }
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            throw new NotImplementedException();
        }
    }

    public class RegulatorParams : DevTypeBase
    {
        public float kP { get; set; }
        public float kI { get; set; }
        public float kD { get; set; }
        public ushort LowConcMotor { get; set; }
        public ushort HighConcMotor { get; set; }
        public ushort SensingAdcChannel { get; set; }
        public ushort LowConcDacChannel { get; set; }
        public ushort HighConcDacChannel { get; set; }
        public ushort LowConcAdcChannel { get; set; }
        public ushort HighConcAdcChannel { get; set; }
        //public ushort Reserved1 { get; set; }
        public float TotalFlowrate { get; set; }

        public override ushort Size => 2 * 3 + 1 * 6 + 2 * 1;
        public override object Get()
        {
            return this;
        }
        public override ushort[] GetWords()
        {
            List<byte> buf = new List<byte>(Size * sizeof(ushort));
            buf.AddRange(BitConverter.GetBytes(kP));
            buf.AddRange(BitConverter.GetBytes(kI));
            buf.AddRange(BitConverter.GetBytes(kD));
            buf.AddRange(BitConverter.GetBytes(LowConcMotor));
            buf.AddRange(BitConverter.GetBytes(HighConcMotor));
            buf.AddRange(BitConverter.GetBytes(SensingAdcChannel));
            buf.AddRange(BitConverter.GetBytes(LowConcDacChannel));
            buf.AddRange(BitConverter.GetBytes(HighConcDacChannel));
            buf.AddRange(BitConverter.GetBytes(LowConcAdcChannel));
            buf.AddRange(BitConverter.GetBytes(HighConcAdcChannel));
            buf.AddRange(BitConverter.GetBytes((ushort)0)); //Reserved1
            buf.AddRange(BitConverter.GetBytes(TotalFlowrate));
            if (buf.Count != Size * sizeof(ushort))
                throw new InvalidOperationException("Device type write buffer size is not equal to defined type size!");
            return IDeviceType.BytesToWords(buf.ToArray(), Size);
        }
        public override void Set(byte[] data, int startIndex = 0)
        {
            kP = BitConverter.ToSingle(data, startIndex);
            kI = BitConverter.ToSingle(data, startIndex += sizeof(float));
            kD = BitConverter.ToSingle(data, startIndex += sizeof(float));
            LowConcMotor = BitConverter.ToUInt16(data, startIndex += sizeof(float));
            HighConcMotor = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            SensingAdcChannel = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            LowConcDacChannel = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            HighConcDacChannel = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            LowConcAdcChannel = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            HighConcAdcChannel = BitConverter.ToUInt16(data, startIndex += sizeof(ushort));
            startIndex += sizeof(ushort); //Reserved1
            TotalFlowrate = BitConverter.ToSingle(data, startIndex += sizeof(ushort));
            OnPropertyChanged();
        }
        public override void Set(string data)
        {
            throw new NotImplementedException();
        }
    }
}
