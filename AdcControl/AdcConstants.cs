using System;
using System.Collections.Generic;
using System.Text;

namespace AdcControl
{
    public static class AdcConstants
    {
        public enum Channels : byte
        {
            AIN0_AIN1 = 0x00,
            AIN2_AIN3 = 0x80
        }
        public enum PgaGain : byte
        {
            x0 = 0x00
        }
        public enum SampleRate : byte
        {
            SPS_20 = 0x00
        }
    }
}
