using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace AdcControl
{
    public struct Point<TX, TY>
    {
        public TX X { get; }
        public TY Y { get; }
        public Point(TX x, TY y)
        {
            X = x;
            Y = y;
        }
    }

    public class AdcChannel
    {
        protected Queue<float> Buffer;

        public AdcChannel(int rawCapacity, int averaging, long start)
        {
            Points = new List<Point<double,float>>(rawCapacity);
            CalculatedPoints = new List<Point<double, float>>(rawCapacity);
            Averaging = averaging;
            Buffer = new Queue<float>(averaging);
            StartTime = start;
        }
        public AdcChannel(int rawCapacity, int averaging) : this(rawCapacity, averaging, DateTime.UtcNow.Ticks)
        { }

        public List<Point<double, float>> Points { get; set; }
        public List<Point<double, float>> CalculatedPoints { get; private set; }
        public int Averaging { get; set; }
        public double StartTime { get; set; }

        public void AddPoint(float val)
        {
            AddPoint(val, DateTime.UtcNow.ToOADate());
        }

        public void AddPoint(float val, double time)
        {
            Points.Add(new Point<double, float>(time, val));
            Buffer.Enqueue(val);
            CalculatedPoints.Add(new Point<double, float>(time - StartTime, Buffer.Average()));
            while (Buffer.Count >= Averaging) //Lag-less buffering and dynamic window size support
            {
                Buffer.Dequeue();
            }
        }

        public void TrimExcess()
        {
            Points.TrimExcess();
            CalculatedPoints.TrimExcess();
        }

        public static async Task Recalculate(AdcChannel c)
        {
            var task = new Task(() =>
            {
                Point<double, float>[] backup = c.Points.ToArray();
                c.Points.Clear();
                c.CalculatedPoints.Clear();
                c.Points.Capacity = backup.Length;
                c.CalculatedPoints.Capacity = backup.Length;
                for (int i = 0; i < backup.Length; i++)
                {
                    c.AddPoint(backup[i].Y, backup[i].X);
                }
            });
            await task;
        }
    }
}
