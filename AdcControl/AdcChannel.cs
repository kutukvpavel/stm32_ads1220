using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Drawing;

namespace AdcControl
{
    public class AdcChannel
    {
        public AdcChannel(int code, int capacity, int averaging, double sampleRate, double start)
        {
            _RawX = new double[capacity];
            _RawY = new double[capacity];
            RawCount = 0;
            _CalculatedX = new double[capacity];
            _CalculatedY = new double[capacity];
            CalculatedCount = 0;
            MovingAveraging = averaging;
            Buffer = new Queue<double>(averaging);
            StartTime = start;
            Code = code;
            CapacityStep = capacity;
            SampleRate = sampleRate;
        }
        public AdcChannel(int code, int rawCapacity, int averaging, double sampleRate) 
            : this(code, rawCapacity, averaging, sampleRate, DateTime.UtcNow.ToOADate())
        { }

        public event EventHandler ArrayChanged;

        protected Queue<double> Buffer;
        protected object LockObject = new object();
#if TRACE
        protected static BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        #region Properties

        //Data for plotting
        private double[] _RawX;
        private double[] _RawY;
        private double[] _CalculatedX;
        private double[] _CalculatedY;
        public double[] RawX { get => _RawX; }
        public double[] RawY { get => _RawY; }
        public double[] CalculatedX { get => _CalculatedX; }
        public double[] CalculatedY { get => _CalculatedY; }

        //Other
        public string ColumnXSuffix { get; set; } = " X";
        public string ColumnYSuffix { get; set; } = " Y";
        public int CapacityStep { get; set; }
        public int RawCount { get; private set; }
        public int CalculatedCount { get; private set; }
        public int MovingAveraging { get; set; }
        public double StartTime { get; set; }
        public int Code { get; }
        public int DropPoints { get; set; }

        protected double _SampleRate = 4;
        public double SampleRate
        {
            get { return _SampleRate; }
            set
            {
                _SampleRate = value;
                if (_Plot != null)
                {
                    _Plot.sampleRate = _SampleRate;
                    _Plot.samplePeriod = 1 / _SampleRate;
                }
            }
        }
        protected string _Name;
        public string Name
        {
            get { return _Name ?? Code.ToString("X"); }
            set
            {
                _Name = value;
                if (_Plot != null) _Plot.label = _Name;
                if (_ContextMenuItem != null) _ContextMenuItem.ChannelName = _Name;
                if (_ColumnX != null) _ColumnX.Header = _Name + ColumnXSuffix;
                if (_ColumnY != null) _ColumnY.Header = _Name + ColumnYSuffix;
            }
        }
        protected bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set
            {
                _IsVisible = value;
                if (_Plot != null) _Plot.visible = _IsVisible;
                if (_ContextMenuItem != null) _ContextMenuItem.IsChecked = _IsVisible;
            }
        }
        protected Color? _Color = System.Drawing.Color.FromKnownColor(KnownColor.Black);
        public Color? Color
        {
            get { return _Color; }
            set
            {
                _Color = value;
                if (_Plot != null && _Color != null)
                {
                    _Plot.color = (Color)_Color;
                    _Plot.brush = new SolidBrush((Color)_Color);
                }
            }
        }
        protected ScottPlot.PlottableSignal _Plot;
        public ScottPlot.PlottableSignal Plot
        {
            get { return _Plot; }
            set
            {
                _Plot = value;
                _Plot.label = _Name;
                _Plot.visible = _IsVisible;
                _Plot.sampleRate = _SampleRate;
                _Plot.samplePeriod = 1 / _SampleRate;
                if (_Color != null)
                {
                    _Plot.color = (Color)_Color;
                    _Plot.brush = new SolidBrush((Color)_Color);
                }
                _Plot.maxRenderIndex = CalculatedCount;
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
                        new AdcChannelContextMenuItem(Code, Name) { IsChecked = IsVisible };
                    _ContextMenuItem.Click += ContextMenuItem_Click;
                }
                return _ContextMenuItem;
            }
        }
        protected ColumnItemsControl _ColumnX;
        public ColumnItemsControl CalculatedXColumn
        {
            get
            {
                if (_ColumnX == null)
                {
                    _ColumnX = new ColumnItemsControl() { ItemStringFormat = "F2", Header = _Name + ColumnXSuffix };
                }
                return _ColumnX;
            }
        }
        protected ColumnItemsControl _ColumnY;
        public ColumnItemsControl CalculatedYColumn
        {
            get
            {
                if (_ColumnY == null)
                {
                    _ColumnY = new ColumnItemsControl() { ItemStringFormat = "F5", Header = _Name + ColumnYSuffix };
                }
                return _ColumnY;
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

        private void ContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IsVisible = ContextMenuItem.IsChecked;
        }

        private void OnArrayChanged()
        {
            new Thread(() =>
            {
                ArrayChanged?.Invoke(this, new EventArgs());
            }).Start();
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
                    Array.Resize(ref _RawX, newSize);
                    Array.Resize(ref _RawY, newSize);
                    Array.Resize(ref _CalculatedX, newSize);
                    Array.Resize(ref _CalculatedY, newSize);
                    arrayChanged = true;
                }
                RawX[RawCount] = time;
                RawY[RawCount] = val;
                RawCount++;
                Buffer.Enqueue(val);
                if (DropPoints <= 1 || ((RawCount % DropPoints) != 0))
                {
                    var x = time - StartTime;
                    var y = Buffer.Average();
                    _CalculatedX[CalculatedCount] = x;
                    _CalculatedY[CalculatedCount] = y;
                    if (_Plot != null && !arrayChanged) _Plot.maxRenderIndex = CalculatedCount;
                    if (_ColumnX != null)
                    {
                        _ColumnX.Dispatcher.BeginInvoke(() =>
                        {
                            _ColumnX.AddItem(CsvExporter.OADateToSeconds(x));
                        });
                    }
                    if (_ColumnY != null)
                    {
                        _ColumnY.Dispatcher.BeginInvoke(() =>
                        {
                            _ColumnY.AddItem(y);
                        });
                    }
                    CalculatedCount++;
                }
                while (Buffer.Count >= MovingAveraging) //Lag-less buffering and dynamic window size support
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
                        Array.Resize(ref _RawX, RawCount);
                        Array.Resize(ref _RawY, RawCount);
                        c = true;
                    }
                    if (CalculatedCount != CalculatedX.Length)
                    {
                        Array.Resize(ref _CalculatedX, CalculatedCount);
                        Array.Resize(ref _CalculatedY, CalculatedCount);
                        c = true;
                    }
                }
                if (c) OnArrayChanged();
            });
            await task;
        }

        public void Clear(int capacity = -1)
        {
            if (capacity < 0) capacity = CapacityStep;
            lock (LockObject)
            {
                RawCount = 0;
                _RawX = new double[capacity];
                _RawY = new double[capacity];
                CalculatedCount = 0;
                _CalculatedX = new double[capacity];
                _CalculatedY = new double[capacity];
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
                    c.Clear(count);
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
        public AdcChannelContextMenuItem(int code, string name)
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

        public int ChannelCode { get; set; }
        public string ChannelName { get; set; }

        public new event RoutedEventHandler Click;

        public override string ToString()
        {
            return ChannelName;
        }
    }
}
