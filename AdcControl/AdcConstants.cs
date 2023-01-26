using System;
using System.Collections.Generic;
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

        public enum Coils : ushort
        {
            Acquire,
            CorrectDAC,
            EnableMotors,
            Ready,
            SaveEEPROM,
            HaveNewData,
            Reserved_1,
            Reserved_2,
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
            OUT7
        }
        public enum HoldingRegisters : ushort
        {
            AcquisitionDuration,
            DACSetpoint,
            DepolarizationPercent,
            DepolarizationSetpoint,
            Motors
        }
        public enum MotorParams : ushort
        {
            RateToSpeed_LW,
            RateToSpeed_HW,
            Microsteps,
            Teeth,
            InvertEnable,
            InvertError,
            Direction
        }
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
        public enum InputRegisters : ushort
        {
            ANALOG0,
            ANALOG1,
            ANALOG2,
            ANALOG3,
            ModulesADC,
            ModulesDAC = ModulesADC + InputModuleADC.LEN
        }
        public enum InputModuleADC : ushort
        {
            V0,
            V1,

            LEN
        }
        public enum InputModuleDAC : ushort
        {
            Current,
            Voltage,
            CorrectedVoltage
        }
    }
}
