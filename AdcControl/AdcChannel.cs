using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AdcControl
{
    public class AdcChannel
    {
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
            CapacityStep = capacity;
        }
        public AdcChannel(int code, int rawCapacity, int averaging) : this(code, rawCapacity, averaging, DateTime.UtcNow.ToOADate())
        { }

        public double[] RawX;
        public double[] RawY;
        public double[] CalculatedX;
        public double[] CalculatedY;

        protected Queue<double> Buffer;
        protected object LockObject = new object();
#if TRACE
        protected static BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        #region Properties
        public int CapacityStep { get; set; }
        public int RawCount { get; set; }
        public int CalculatedCount { get; private set; }
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
                if (ContextMenuItem != null) ContextMenuItem.IsChecked = _IsVisible;
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
                if (_ContextMenuItem == null)
                {
                    _ContextMenuItem =
                        new AdcChannelContextMenuItem(ReturnCode, ReturnName) { IsChecked = IsVisible };
                    _ContextMenuItem.Click += ContextMenuItem_Click;
                }
                return _ContextMenuItem;
            }
        }

        #endregion

        #region Private Functions

        private static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(
                string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }

        private int ReturnCode()
        {
            return Code;
        }

        private string ReturnName()
        {
            return Name;
        }

        private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IsVisible = ContextMenuItem.IsChecked;
        }

        private void OnArrayChanged()
        {
            Plot.xs = CalculatedX;
            Plot.ys = CalculatedY;
        }

        #endregion

        #region Public Functions

        public void AddPoint(double val)
        {
            AddPoint(val, DateTime.UtcNow.ToOADate());
        }

        public void AddPoint(double val, double time)
        {
            bool arrayChanged = false;
            Trace("AddPoint waiting");
            lock (LockObject)
            {
                Trace("AddPoint entered");
                if (RawCount == RawX.Length)
                {
                    int newSize = RawX.Length + CapacityStep;
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
            if (arrayChanged) OnArrayChanged();
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
                lock (c.LockObject)
                {
                    double[] backupX = c.RawX;
                    double[] backupY = c.RawY;
                    int count = c.RawCount;
                    c.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        c.AddPoint(backupY[i], backupX[i]);
                    }
                }
            });
            await task;
        }

        #endregion
    }

    public class AdcChannelContextMenuItem : MenuItem
    {
        public AdcChannelContextMenuItem(Func<int> code, Func<string> name)
        {
            ChannelCode = code;
            ChannelName = name;
            base.Click += AdcChannelContextMenuItem_Click;
            base.Loaded += AdcChannelContextMenuItem_Loaded;
        }

        private void AdcChannelContextMenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            Header = ToString();
        }

        private void AdcChannelContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IsChecked = !IsChecked;
            Click?.Invoke(this, e);
        }

        private Func<int> ChannelCode { get; }
        private Func<string> ChannelName { get; }

        public new event RoutedEventHandler Click;

        public override string ToString()
        {
            return DictionarySaver.WriteMapping(ChannelCode(), ChannelName());
        }

        public int GetChannelCode()
        {
            return ChannelCode();
        }

        public string GetChannelName()
        {
            return ChannelName();
        }
    }
}
