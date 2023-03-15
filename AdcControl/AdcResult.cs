using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl
{
    public class AdcResult
    {
        public float[] Voltages { get; }
        public float[] Currents { get; }
        public float[] CorrectedCurrents { get; }

    }
}
