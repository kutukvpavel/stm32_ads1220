using System;
using System.Diagnostics;
using System.Drawing;
using org.mariuszgromada.math.mxparser;
using org.mariuszgromada.math.mxparser.mathcollection;

namespace DebugHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            //  -47872
            //  -16777011
            Console.WriteLine(Color.FromKnownColor(KnownColor.OrangeRed).ToArgb());
            Console.WriteLine(Color.FromKnownColor(KnownColor.MediumBlue).ToArgb());
            */

            Console.WriteLine("Calculating...");
            Argument y = new Argument("y", 0.01);
            Constant f = new Constant("F", 96500);
            Constant r = new Constant("R", 8.314);
            Constant t = new Constant("T", 800);
            Constant p = new Constant("p", 21000);
            Stopwatch w = new Stopwatch();
            w.Start();
            Expression e = new Expression("p*exp(-y*4*F/(R*T))", y, f, r, t, p);
            w.Stop();
            Console.WriteLine(w.ElapsedTicks);
            double d;
            w.Restart();
            d = e.calculate();
            w.Stop();
            Console.WriteLine(w.ElapsedTicks);
            Console.WriteLine(d);
            e.setArgumentValue("y", 0.02);
            w.Restart();
            d = e.calculate();
            w.Stop();
            Console.WriteLine(w.ElapsedTicks);
            Console.WriteLine(d);
            e.setArgumentValue("y", 0.03);
            w.Restart();
            d = e.calculate();
            w.Stop();
            Console.WriteLine(w.ElapsedTicks);
            Console.WriteLine(d);
            y.setArgumentValue(0.02);
            w.Restart();
            d = e.calculate();
            w.Stop();
            Console.WriteLine(w.ElapsedTicks);
            Console.WriteLine(d);
        }
    }
}
