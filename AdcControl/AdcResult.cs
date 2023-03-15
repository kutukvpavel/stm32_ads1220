using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl
{
    public class AdcResult
    {
        public float[] Voltages { get; set; }
        public float[] Currents { get; set; }
        public float[] CorrectedCurrents { get; set; }
    }
}
