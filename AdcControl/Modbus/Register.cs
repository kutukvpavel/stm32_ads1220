using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AdcControl.Modbus
{
    public class Register<T> : IRegister where T : IDeviceType, new()
    {
        public Register(ushort addr, string name)
        {
            if (!BitConverter.IsLittleEndian) throw new NotImplementedException("Non-LE archs not supported.");
            if ((TypedValue.Size > 1) && (TypedValue.Size % 2 != 0)) //32-bit alignment
                throw new ArgumentException($"Device data type definition is wrong: {TypedValue.GetType()}");
            Address = addr;
            Name = name;
            TypedValue.PropertyChanged += TypedValue_PropertyChanged;
        }

        private void TypedValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(TypedValue));
        }
        protected void OnPropertyChanged(string name = null)
        {
            Task.Run(() => {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }

        public Type Type => typeof(T);
        public IDeviceType Value => TypedValue;
        public T TypedValue { get; set; } = new T();
        public ushort Address { get; }
        public ushort Length => TypedValue.Size; //In modbus words
        public string Name { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ushort[] GetWords()
        {
            return TypedValue.GetWords();
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
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(TypedValue));
        }
    }
}
