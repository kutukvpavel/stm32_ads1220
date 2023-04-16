using System;
using System.Collections.Specialized;
using System.Text;

namespace AdcControl
{
    public static class AdcConstants
    {
        /*public enum Channels : byte
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
        }*/

        public const int DacCurrentsBase = 0xD00;
        public const int DacCorrectedVoltagesBase = 0xC00;

        public enum Coils : ushort
        {
            Acquire,
            CorrectDAC,
            EnableMotors,
            Ready,
            SaveEEPROM,
            HaveNewData,
            Regulate,
            Depolarize,
            ADC_RES,
            ADC_EN,
            DAC_RES,
            DAC_EN,
            MOTOR_EN,
            MOTOR_DIR0,
            MOTOR_DIR1,
            MOTOR_DIR2,
            OUT0,
            OUT1,
            OUT2,
            OUT3,
            OUT4,
            OUT5,
            OUT6,
            OUT7,

            LEN
        }
        public static readonly Coils[] ConfigurationCoils =
        {
            Coils.CorrectDAC,
            Coils.Depolarize,
            Coils.EnableMotors,
            Coils.Regulate
        };
        public enum DiscreteInputs : ushort
        {
            MOTOR_ERR0,
            MOTOR_ERR1,
            MOTOR_ERR2,
            IN3,
            IN4,
            IN5,
            IN6,
            IN7
        }
        public enum ConfigurationRegisters : ushort
        {
            MOTORS_NUM,
            MAX_ADC_MODULES,
            ADC_CHANNELS_PER_CHIP,
            PRESENT_ADC_CHANNELS,
            MAX_DAC_MODULES,
            PRESENT_DAC_MODULES,
            AIO_NUM,
            RESERVED1,

            LEN
        }
        public static readonly OrderedDictionary ConfigurationRegisterNames 
            = new OrderedDictionary()
        {
                { ConfigurationRegisters.MOTORS_NUM, "MOTORS_NUM" },
                { ConfigurationRegisters.MAX_ADC_MODULES, "MAX_ADC_MODULES" },
                { ConfigurationRegisters.ADC_CHANNELS_PER_CHIP, "ADC_CHANNELS_PER_CHIP" },
                { ConfigurationRegisters.PRESENT_ADC_CHANNELS, "PRESENT_ADC_CHANNELS" },
                { ConfigurationRegisters.MAX_DAC_MODULES, "MAX_DAC_MODULES" },
                { ConfigurationRegisters.PRESENT_DAC_MODULES, "PRESENT_DAC_MODULES" },
                { ConfigurationRegisters.AIO_NUM, "AIO_NUM" },
                { ConfigurationRegisters.RESERVED1, "RESERVED_1" }
        };
        public static readonly string AdcVoltagesNameTemplate = "ADC_VOLTAGE_";
        public static readonly string DacCurrentsNameTemplate = "DAC_CURRENT_";
        public static readonly string DacCorrectedNameTemplate = "DAC_CORR_";
        public static readonly string DacCorrectionIntervalNameTemplate = "DAC_CORR_INTERVAL_";
        public static readonly string DacSetpointNameTemplate = "DAC_SETPOINT_";
        public static readonly string DacDepoPercentNameTemplate = "DEPO_PERCENT_";
        public static readonly string DacDepoIntervalNameTemplate = "DEPO_INVERAL_";
        public static readonly string DacDepoSetpointNameTemplate = "DEPO_SETPOINT_";
    }
}
