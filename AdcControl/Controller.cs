using AdcControl.Resources;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NModbus;
using NModbus.SerialPortStream;
using NModbus.Extensions;
using NModbus.Utility;

using Timer = System.Timers.Timer;

namespace AdcControl
{
    public class Controller : INotifyPropertyChanged, IDisposable
    {
        #region Private

        /* Private */

        protected const char NewLine = '\n';
        protected readonly ParameterizedThreadStart DataErrorEventThreadStart;
        protected readonly ParameterizedThreadStart DeviceErrorThreadStart;
        protected bool _AcquisitionInProgress = false;
        protected bool _IsConnected = false;
        protected bool _Completed = true;
        protected object LockObject = new object();
        protected SerialPortStreamAdapter Adapter;
        protected IModbusMaster Master;
        protected readonly byte UnitAddress;
        protected readonly ModbusFactory Factory = new ModbusFactory();
        protected readonly Timer PollTimer;
#if TRACE
        protected static BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        protected static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }

        private void Log(Exception e, string m)
        {
            new Thread(() => { LogEvent?.Invoke(this, new LogEventArgs(e, m)); }).Start();
        }
        private void OnPropertyChanged()
        {
            new Thread(() => { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); }).Start();
        }
        private void OnUnexpectedDisconnect()
        {
            _IsConnected = false;
            _AcquisitionInProgress = false;
            _Completed = true;
            OnPropertyChanged();
            new Thread(() => { UnexpectedDisconnect?.Invoke(this, new EventArgs()); }).Start();
        }

#endregion

        /* Public */

        public Controller(SerialPortStream port, byte addr = 0x01)
        {
            Port = port;
            DataErrorEventThreadStart = (object x) => { DataError?.Invoke(this, (DataErrorEventArgs)x); };
            DeviceErrorThreadStart = (object x) => { DeviceError?.Invoke(this, (TerminalEventArgs)x); };

            UnitAddress = addr;
            RegisterMap = new Modbus.Map();

            PollTimer = new Timer(1000)
            {
                AutoReset = true,
                Enabled = false
            };
            PollTimer.Elapsed += PollTimer_Elapsed;
        }

        private void PollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (LockObject)
            {
                try
                {
                    var task = Read();
                    task.Wait();
                    if (task.IsCompletedSuccessfully)
                    {
                        var value = task.Result;
                        new Thread(() => {
                            foreach (var item in value.Voltages.Select((x, i) => new AcquisitionEventArgs(i, x)))
                            {
                                AcquisitionDataReceived?.Invoke(this, item);
                            }
                            foreach (var item in value.Currents.Select((x, i) => new AcquisitionEventArgs(i + 0xD00, x)))
                            {
                                AcquisitionDataReceived?.Invoke(this, item);
                            }
                            foreach (var item in value.CorrectedCurrents.Select((x, i) => new AcquisitionEventArgs(i + 0xC00, x)))
                            {
                                AcquisitionDataReceived?.Invoke(this, item);
                            }
                        }).Start();
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public event EventHandler<AcquisitionEventArgs> AcquisitionDataReceived;
        public event EventHandler<DataErrorEventArgs> DataError;
        public event EventHandler<TerminalEventArgs> DeviceError;
        public event EventHandler<LogEventArgs> LogEvent;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler AcquisitionFinished;
        public event EventHandler UnexpectedDisconnect;

#region Properties

        public SerialPortStream Port { get; }
        public static int ConnectionTimeout { get; set; } = 3000; //mS
        public bool IsConnected
        {
            get { return _IsConnected; }
            private set
            {
                _IsConnected = value;
                OnPropertyChanged();
            }
        }
        public bool AcquisitionInProgress
        {
            get { return _AcquisitionInProgress; }
            private set
            {
                var b = _AcquisitionInProgress;
                _AcquisitionInProgress = value;
                OnPropertyChanged();
                if (!_AcquisitionInProgress && b)
                    new Thread(() => { AcquisitionFinished?.Invoke(this, new EventArgs()); }).Start();
            }
        }
        public bool ReadyForAcquisition
        {
            get { return _IsConnected && !_AcquisitionInProgress; }
        }
        public bool IsNotConnected
        {
            get { return !_IsConnected; }
        }
        public Modbus.Map RegisterMap { get; }

#endregion

#region Methods

        public async Task<AdcResult> Read()
        {
            try
            {
                foreach (var item in RegisterMap.PollRegisters)
                {
                    var reg = RegisterMap.InputRegisters[item] as Modbus.IRegister;
                    reg.Set(await Master.ReadInputRegistersAsync(UnitAddress, reg.Address, 1));
                }
                int adcPresent = RegisterMap.GetConfigValue(AdcConstants.ConfigurationRegisters.PRESENT_ADC_CHANNELS);
                int dacPresent = RegisterMap.GetConfigValue(AdcConstants.ConfigurationRegisters.PRESENT_DAC_MODULES);
                float[] adcBuffer = new float[adcPresent];
                for (int i = 0; i < adcPresent; i++)
                {
                    adcBuffer[i] = RegisterMap.GetInputFloat(AdcConstants.AdcVoltagesNameTemplate + i.ToString());
                }
                float[] dacBuffer = new float[dacPresent];
                float[] dacCorrBuffer = new float[dacPresent];
                for (int i = 0; i < dacPresent; i++)
                {
                    dacBuffer[i] = RegisterMap.GetInputFloat(AdcConstants.DacCurrentsNameTemplate + i.ToString());
                    dacCorrBuffer[i] = RegisterMap.GetInputFloat(AdcConstants.DacCorrectedNameTemplate + i.ToString());
                }
                return new AdcResult()
                {
                    Voltages = adcBuffer,
                    Currents = dacBuffer,
                    CorrectedCurrents = dacCorrBuffer
                };
            }
            catch (Exception ex)
            {
                Log(ex, "Failed to poll the device");
                throw;
            }
        }
        public async Task<bool> InitRegisterMap()
        {
            try
            {
                RegisterMap.Clear();
                //Add configuration registers by default, build configuration dependent layout later
                for (int i = 0; i < (int)AdcConstants.ConfigurationRegisters.LEN; i++)
                {
                    AdcConstants.ConfigurationRegisters reg = (AdcConstants.ConfigurationRegisters)i;
                    RegisterMap.AddInput<Modbus.DevUshort>(AdcConstants.ConfigurationRegisterNames[reg] as string, 1, true);
                }

                //Read configuration registers
                foreach (var item in RegisterMap.ConfigRegisters)
                {
                    var reg = RegisterMap.InputRegisters[item] as Modbus.IRegister;
                    reg.Set(await Master.ReadInputRegistersAsync(UnitAddress, reg.Address, 1));
                }
                //Build complete register map
                int adcTotal = RegisterMap.GetConfigValue(AdcConstants.ConfigurationRegisters.MAX_ADC_MODULES) *
                    RegisterMap.GetConfigValue(AdcConstants.ConfigurationRegisters.ADC_CHANNELS_PER_CHIP);
                int dacTotal = RegisterMap.GetConfigValue(AdcConstants.ConfigurationRegisters.MAX_DAC_MODULES);
                int aioTotal = RegisterMap.GetConfigValue(AdcConstants.ConfigurationRegisters.AIO_NUM);
                int motorTotal = RegisterMap.GetConfigValue(AdcConstants.ConfigurationRegisters.MOTORS_NUM);
                //Input
                RegisterMap.AddInput<Modbus.DevFloat>(AdcConstants.AdcVoltagesNameTemplate, adcTotal, poll: true);
                RegisterMap.AddInput<Modbus.DevFloat>(AdcConstants.DacCurrentsNameTemplate, dacTotal, poll: true);
                RegisterMap.AddInput<Modbus.DevFloat>(AdcConstants.DacCorrectedNameTemplate, dacTotal, poll: true);
                RegisterMap.AddInput<Modbus.DevFloat>("A_IN_", aioTotal);
                RegisterMap.AddInput<Modbus.DevFloat>("TEMP", 1);
                //Holding
                RegisterMap.AddInput<Modbus.DevFloat>("DAC_SETPOINT_", dacTotal);
                RegisterMap.AddInput<Modbus.AdcChannelCal>("ADC_CAL_", adcTotal);
                RegisterMap.AddInput<Modbus.DacCal>("DAC_CAL_", dacTotal);
                RegisterMap.AddInput<Modbus.AioCal>("AIO_CAL_", aioTotal);
                RegisterMap.AddInput<Modbus.AioCal>("TEMP_CAL", 1);
                RegisterMap.AddInput<Modbus.MotorParams>("MOTOR_PARAMS_", motorTotal);
                RegisterMap.AddInput<Modbus.DevFloat>("DEPO_PERCENT_", dacTotal);
                RegisterMap.AddInput<Modbus.DevFloat>("DEPO_SETPOINT_", dacTotal);
            }
            catch (Exception ex)
            {
                Log(ex, "Failed to initialize register map.");
                return false;
            }
            return true;
        }
        public async Task<bool> StartAcquisition()
        {
            if (!AcquisitionInProgress)
            {
                try
                {
                    await Master.WriteSingleCoilAsync(UnitAddress, (ushort)AdcConstants.Coils.Acquire, true);
                    PollTimer.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    Log(ex, "Failed to start acquisition");
                }
            }
            return false;
        }
        public async Task<bool> StopAcquisition()
        {
            if (AcquisitionInProgress)
            {
                try
                {
                    PollTimer.Stop();
                    await Master.WriteSingleCoilAsync(UnitAddress, (ushort)AdcConstants.Coils.Acquire, false);
                    return true;
                }
                catch (Exception ex)
                {
                    Log(ex, "Failed to stop acquisition");
                }
            }
            return false;
        }
        public async Task<bool> Connect(string portName = null)
        {
            if (Port.IsOpen) Port.Close();
            IsConnected = false;
            if (portName != null) Port.PortName = portName;
            Adapter = new SerialPortStreamAdapter(Port)
            {
                ReadTimeout = ConnectionTimeout,
                WriteTimeout = ConnectionTimeout
            };
            Master = Factory.CreateRtuMaster(Adapter);
            if ((await Master.ReadCoilsAsync(UnitAddress, (ushort)AdcConstants.Coils.Ready, 1))[0])
            {
                return await InitRegisterMap();
            }
            return false;
        }
        public bool Disconnect()
        {
            IsConnected = false;
            try
            {
                Port.Close();
            }
            catch (Exception e)
            {
                Log(e, Default.msgPortCloseProblem);
            }
            return !Port.IsOpen;
        }

        public void Dispose()
        {
            Disconnect();
            Master.Dispose();
        }

        #endregion
    }

#region EventArgs

    public class AcquisitionEventArgs : EventArgs
    {
        public float Value { get; }
        public int Channel { get; }

        public AcquisitionEventArgs(int channel, float value)
        {
            Channel = channel;
            Value = value;
        }
    }

    public class DataErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Data { get; }
        public DataErrorEventArgs(Exception e, string data)
        {
            Exception = e;
            Data = data;
        }
    }

    public class LogEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Message { get; }
        public LogEventArgs(Exception e, string msg)
        {
            Exception = e;
            Message = msg;
        }
    }

    public class TerminalEventArgs : EventArgs
    {
        public string Line { get; }
        public TerminalEventArgs(string line)
        {
            Line = line;
        }
    }

#endregion
}
