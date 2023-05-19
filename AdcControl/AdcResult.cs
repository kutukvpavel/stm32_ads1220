﻿using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl
{
    public class AdcResult
    {
        public DateTime Timestamp { get; set; }
        public float[] Voltages { get; set; }
        public float[] Currents { get; set; }
        public float[] CorrectedVoltages { get; set; }
        public float[] PumpSpeeds { get; set; }
    }
}
