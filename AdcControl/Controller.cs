using System;
using System.Collections.Generic;
using System.Text;
using RJCP.IO.Ports;
using System.Linq;
using System.Threading;
using AdcControl.Resources;
using System.Threading.Tasks;

namespace AdcControl
{
    public class Controller
    {
        /* Private */

        protected const char NewLine = '\n';
        protected const char Splitter = ':';
        protected const char ErrorDesignator = '!';
        protected const string ArrayHexCommandFormat = "{0}{1:2} {3:X}";
        protected const string ArrayFloatCommandFormat = "{0}{1:2} {3:6}";
        protected const string SimpleCommandFormat = "{0}{1}";
        protected const string AcquisitionSignature = "ACQ.";
        protected const string EndOfAcquisitionSignature = "END.";
        protected static readonly Dictionary<Commands, string> CommandFormat = new Dictionary<Commands, string>()
        {
            { Commands.SetChannel,  ArrayHexCommandFormat },
            { Commands.SetPgaGain, ArrayHexCommandFormat },
            { Commands.SetCalibrationOffset, ArrayFloatCommandFormat },
            { Commands.SetCalibrationCoefficient, ArrayFloatCommandFormat },
            { Commands.ToggleAcquisition, SimpleCommandFormat }
        };
        protected StringBuilder Buffer;
        protected readonly ParameterizedThreadStart DataReceiverEventThreadStart;
        protected readonly ParameterizedThreadStart DataErrorEventThreadStart;
        protected readonly ParameterizedThreadStart TerminalEventStart;
        protected readonly ParameterizedThreadStart DeviceErrorThreadStart;
        protected bool IsAcquiring = false;
        protected Task<bool> WaitForConnection;
        protected Task<bool> WaitForCompletion;
        protected bool _IsConnected = false;
        protected bool _Completed = false;
        protected object LockObject = new object(); 

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string read = Port.ReadExisting();
            int i = read.IndexOf(NewLine);
            if (i > -1)
            {
                string line = Buffer.Append(read, 0, i++).ToString(); //Skip the new line character here
                Buffer.Clear();
                Buffer.Append(read, i, read.Length - i);
                var thread = new Thread(TerminalEventStart);
                thread.Start(new TerminalEventArgs(line));
                if (line.EndsWith(ErrorDesignator))
                {
                    thread = new Thread(DeviceErrorThreadStart);
                    thread.Start(new TerminalEventArgs(line));
                }
                if (IsAcquiring)
                {
                    if (line.Length == EndOfAcquisitionSignature.Length)
                    {
                        if (line.SequenceEqual(EndOfAcquisitionSignature))
                        {
                            IsAcquiring = false;
                            return;
                        }
                    }
                    thread = new Thread(DataReceiverEventThreadStart);
                    try
                    {

                        string[] parsed = line.Split(Splitter);
                        thread.Start(new AcquisitionEventArgs(
                            byte.Parse(parsed[0], System.Globalization.NumberStyles.HexNumber),
                            float.Parse(parsed[1], System.Globalization.NumberStyles.AllowLeadingWhite)
                            ));
                    }
                    catch (Exception exc)
                    {
                        thread = new Thread(DataErrorEventThreadStart);
                        thread.Start(new DataErrorEventArgs(exc, line));
                    }
                }
                else
                {
                    if (line.Length == AcquisitionSignature.Length)
                    {
                        if (line.SequenceEqual(AcquisitionSignature))
                        {
                            IsAcquiring = true;
                            return;
                        }
                    }
                }
            }
            else
            {
                Buffer.Append(read);
            }
        }
        private void Log(Exception e, string m)
        {
            new Thread(() => { LogEvent?.Invoke(this, new LogEventArgs(e, m)); }).Start();
        }
        private bool Wait(ref bool flag, int timeout)
        {
            int i = 0;
            while (!flag)
            {
                Thread.Sleep(1);
                if (i++ > timeout) return false;
            }
            return true;
        }

        /* Public */

        public Controller(SerialPortStream port)
        {
            Buffer = new StringBuilder(16);
            Port = port;
            Port.DataReceived += Port_DataReceived;
            DataReceiverEventThreadStart = (object x) => { AcquisitionDataReceived?.Invoke(this, (AcquisitionEventArgs)x); };
            DataErrorEventThreadStart = (object x) => { DataError?.Invoke(this, (DataErrorEventArgs)x); };
            TerminalEventStart = (object x) => { TerminalEvent?.Invoke(this, (TerminalEventArgs)x); };
            DeviceErrorThreadStart = (object x) => { DeviceError?.Invoke(this, (TerminalEventArgs)x); };
            WaitForConnection = new Task<bool>(() => 
            {
                return Wait(ref _IsConnected, ConnectionTimeout);
            });
            WaitForCompletion = new Task<bool>(() =>
            {
                return Wait(ref _Completed, CompletionTimeout);
            });
        }

        public enum Commands : byte
        {
            ToggleAcquisition = (byte)'A',
            Reset = (byte)'R',
            FactoryReset = (byte)'F',
            Info = (byte)'I',
            SetCalibrationCoefficient = (byte)'C',
            SetCalibrationOffset = (byte)'O',
            SetPgaGain = (byte)'G',
            SetChannel = (byte)'H'
        }

        public event EventHandler<AcquisitionEventArgs> AcquisitionDataReceived;
        public event EventHandler<DataErrorEventArgs> DataError;
        public event EventHandler<TerminalEventArgs> DeviceError;
        public event EventHandler<LogEventArgs> LogEvent;
        public event EventHandler<TerminalEventArgs> TerminalEvent;

        public SerialPortStream Port { get; }
        public int ConnectionTimeout { get; set; } = 5000; //mS
        public int CompletionTimeout { get; set; } = 3000; //mS
        public bool IsConnected
        {
            get { return _IsConnected; }
            private set
            {
                _IsConnected = value;
            }
        }
        public bool CommandExecutionCompleted
        {
            get { return _Completed; }
            set
            {
                _Completed = value;
            }
        }
        public bool AcquisitionInProgress
        {
            get { return IsAcquiring; }
        }
        public bool ReadyForAcquisition
        {
            get { return _IsConnected && !IsAcquiring; }
        }
        public bool ReadyForConnection
        {
            get { return Port.IsOpen && !_IsConnected; }
        }

        public async Task<bool> SendCommand(Commands cmd, params object[] args)
        {
            if (args == null) args = new object[0];
            if (CommandFormat.ContainsKey(cmd) && args.Length > 0)
            {
                args = args.Prepend((char)(byte)cmd).ToArray();
                return await SendCustom(string.Format(CommandFormat[cmd], args));
            }
            else
            {
                return await SendCustom(((char)(byte)cmd).ToString());
            }
        }
        public async Task<bool> SendCustom(string cmd)
        {
            await WaitForCompletion;
            CommandExecutionCompleted = false;
            try
            {
                Port.WriteLine(cmd);
            }
            catch (Exception e)
            {
                Log(e, string.Format("{0} Info: {1}", Default.msgPortWriteError, cmd));
                return false;
            }
            return await WaitForCompletion;
        }
        public async Task<bool> StartAcquisition(int duration = 0)
        {
            if (!IsAcquiring)
                return await SendCommand(Commands.ToggleAcquisition, duration > 0 ? duration.ToString() : null);
            return false;
        }
        public async Task<bool> StopAcquisition()
        {
            if (IsAcquiring) return await SendCommand(Commands.ToggleAcquisition);
            return false;
        }
        public async Task<bool> Connect(string portName = null)
        {
            IsConnected = false;
            if (Port.IsOpen)
            {
                Log(null, Default.msgPortAlreadyOpen);
                Disconnect();
            }
            if (portName != null) Port.PortName = portName;
            try
            {
                Port.Open();
            }
            catch (Exception e)
            {
                Log(e, Default.msgPortOpenProblem);
            }
            return await WaitForConnection;
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
    }

    public class AcquisitionEventArgs : EventArgs
    {
        public float Value { get; }
        public byte Channel { get; }

        public AcquisitionEventArgs(byte channel, float value)
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
}
