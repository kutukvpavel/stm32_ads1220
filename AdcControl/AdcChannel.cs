using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdcControl
{
    public class AdcChannel
    {
        protected Queue<double> Buffer;
        protected object LockObject = new object();

        public AdcChannel(int code, int capacity, int averaging, double start)
        {
            RawX = new double[capacity];
            RawY = new double[capacity];
            RawCount = 0;
            CalculatedX = new double[capacity];
            CalculatedY = new double[capacity];
            CalculatedCount = 0;
            Averaging = averaging;
            Buffer = new Queue<double>(averaging);
            StartTime = start;
            Code = code;
        }
        public AdcChannel(int code, int rawCapacity, int averaging) : this(code, rawCapacity, averaging, DateTime.UtcNow.ToOADate())
        { }

        public event EventHandler ArrayChanged;
        public event EventHandler<LogEventArgs> DebugLogEvent;

        public int RawCount { get; set; }
        public double[] RawX;
        public double[] RawY;
        public int CalculatedCount { get; private set; }
        public double[] CalculatedX;
        public double[] CalculatedY;
        public int Averaging { get; set; }
        public double StartTime { get; set; }
        public int Code { get; }
        protected string _Name;
        public string Name
        {
            get { return _Name ?? Code.ToString("X"); }
            set
            {
                _Name = value;
                if (Plot != null) Plot.label = _Name;
            }
        }
        protected bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set
            {
                _IsVisible = value;
                if (Plot != null) Plot.visible = _IsVisible;
            }
        }
        protected System.Drawing.Color? _Color = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Black);
        public System.Drawing.Color? Color
        {
            get { return _Color; }
            set
            {
                _Color = value;
                if (Plot != null && _Color != null) Plot.color = (System.Drawing.Color)_Color;
            }
        }
        protected ScottPlot.PlottableScatter _Plot;
        public ScottPlot.PlottableScatter Plot
        {
            get { return _Plot; }
            set
            {
                _Plot = value;
                _Plot.label = _Name;
                _Plot.visible = _IsVisible;
                if (_Color != null) _Plot.color = (System.Drawing.Color)_Color;
            }
        }
        protected AdcChannelContextMenuItem _ContextMenuItem;
        public AdcChannelContextMenuItem ContextMenuItem
        {
            get 
            {
                if (_ContextMenuItem == null) _ContextMenuItem = 
                        new AdcChannelContextMenuItem(() => { return Code; }, () => { return Name; });
                return _ContextMenuItem;
            }
        }

        public void AddPoint(double val)
        {
            AddPoint(val, DateTime.UtcNow.ToOADate());
        }

        private void OnArrayChanged()
        {
            /*var thread = new Thread(() =>
            {
                ArrayChanged?.Invoke(this, new EventArgs());
            });
            thread.Start();*/
            ArrayChanged?.Invoke(this, new EventArgs());
        }

        protected void DebugTrace(string msg)
        {
            new Thread(() =>
            {
                DebugLogEvent?.Invoke(this, new LogEventArgs(new Exception(msg), "AdcChannel trace"));
            }).Start();
        }

        public void AddPoint(double val, double time)
        {
            bool arrayChanged = false;
            lock (LockObject)
            {
                if (RawCount == RawX.Length)
                {
                    int newSize = RawX.Length * 2;
                    Array.Resize(ref RawX, newSize);
                    Array.Resize(ref RawY, newSize);
                    Array.Resize(ref CalculatedX, newSize);
                    Array.Resize(ref CalculatedY, newSize);
                    arrayChanged = true;
                }
                RawX[RawCount] = time;
                RawY[RawCount++] = val;
                Buffer.Enqueue(val);
                CalculatedX[CalculatedCount] = time - StartTime;
                CalculatedY[CalculatedCount++] = Buffer.Average();
                while (Buffer.Count >= Averaging) //Lag-less buffering and dynamic window size support
                {
                    Buffer.Dequeue();
                }
            }
            if (arrayChanged)
            {
                OnArrayChanged();
                DebugTrace("AddPoint arrayChanged fired for: " + Code.ToString());
            }
        }

        public async Task TrimExcess()
        {
            var task = new Task(() =>
            {
                bool c = false;
                lock (LockObject)
                {
                    if (RawCount != RawX.Length)
                    {
                        Array.Resize(ref RawX, RawCount);
                        Array.Resize(ref RawY, RawCount);
                        c = true;
                    }
                    if (CalculatedCount != CalculatedX.Length)
                    {
                        Array.Resize(ref CalculatedX, CalculatedCount);
                        Array.Resize(ref CalculatedY, CalculatedCount);
                        c = true;
                    }
                }
                if (c) OnArrayChanged();
            });
            await task;
        }

        public void Clear()
        {
            lock (LockObject)
            {
                RawCount = 0;
                RawX = new double[RawX.Length];
                RawY = new double[RawY.Length];
                CalculatedCount = 0;
                CalculatedX = new double[CalculatedX.Length];
                CalculatedY = new double[CalculatedY.Length];
                Buffer.Clear();
            }
            OnArrayChanged();
        }

        public static async Task Recalculate(AdcChannel c)
        {
            var task = new Task(() =>
            {
                double[] backupX = c.RawX;
                double[] backupY = c.RawY;
                int count = c.RawCount;
                c.Clear();
                for (int i = 0; i < count; i++)
                {
                    c.AddPoint(backupY[i], backupX[i]);
                }
            });
            await task;
        }
    }

    public class AdcChannelContextMenuItem
    {
        public AdcChannelContextMenuItem(Func<int> code, Func<string> name)
        {
            Code = code;
            Name = name;
        }

        private Func<int> Code;
        private Func<string> Name;

        public override string ToString()
        {
            int c = Code();
            string n = Name();
            return DictionarySaver.WriteMapping(c, n ?? c.ToString("X"));
        }
    }
}
