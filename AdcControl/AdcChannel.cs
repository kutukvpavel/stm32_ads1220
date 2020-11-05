using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace AdcControl
{
    public class AdcChannel
    {
        protected Queue<double> Buffer;
        protected object LockObject = new object();

        public AdcChannel(int capacity, int averaging, double start)
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
        }
        public AdcChannel(int rawCapacity, int averaging) : this(rawCapacity, averaging, DateTime.UtcNow.ToOADate())
        { }

        public event EventHandler Overflow;
        public event EventHandler ArrayChanged;

        public int RawCount { get; set; }
        public double[] RawX;
        public double[] RawY;
        public int CalculatedCount { get; private set; }
        public double[] CalculatedX;
        public double[] CalculatedY;
        public int Averaging { get; set; }
        public double StartTime { get; set; }

        public void AddPoint(double val)
        {
            AddPoint(val, DateTime.UtcNow.ToOADate());
        }

        public void AddPoint(double val, double time)
        {
            lock (LockObject)
            {
                if (RawCount == RawX.Length)
                {
                    var thread = new Thread(() =>
                    {
                        Overflow?.Invoke(this, new EventArgs());
                    });
                    return;
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
        }

        public async Task TrimExcess()
        {
            var task = new Task(() =>
            {
                lock (LockObject)
                {
                    if (RawCount != RawX.Length)
                    {
                        Array.Resize(ref RawX, RawCount);
                        Array.Resize(ref RawY, RawCount);
                    }
                    if (CalculatedCount != CalculatedX.Length)
                    {
                        Array.Resize(ref CalculatedX, CalculatedCount);
                        Array.Resize(ref CalculatedY, CalculatedCount);
                    }
                }
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
}
