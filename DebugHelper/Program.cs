using System;
using System.Drawing;

namespace DebugHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            //  -47872
            //  -16777011
            Console.WriteLine(Color.FromKnownColor(KnownColor.OrangeRed).ToArgb());
            Console.WriteLine(Color.FromKnownColor(KnownColor.MediumBlue).ToArgb());
            Console.ReadKey();
        }
    }
}
