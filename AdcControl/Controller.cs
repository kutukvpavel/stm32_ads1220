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

namespace AdcControl
{
    public class Controller : INotifyPropertyChanged
    {
        #region Private

        /* Private */

        protected const char NewLine = '\n';
        protected static readonly char[] ToTrim = { '\r', ' ', '\0' };
        protected const char Splitter = ':';
        protected const char ErrorDesignator = '!';
        protected const string ArrayHexCommandFormat = "{0}{1:2} {3:X}";
        protected const string ArrayFloatCommandFormat = "{0}{1:2} {3:6}";
        protected const string SimpleCommandFormat = "{0}{1}";
        protected const string AcquisitionSignature = "ACQ.";
        protected const string EndOfAcquisitionSignature = "END.";
        protected const string ConnectionSignature = "READY...";
        protected const string CompletionSignature = "PARSED.";
        protected static readonly Dictionary<Commands, string> CommandFormat = new Dictionary<Commands, string>()
        {
            { Commands.SetChannel,  ArrayHexCommandFormat },
            { Commands.SetPgaGain, ArrayHexCommandFormat },
            { Commands.SetCalibrationOffset, ArrayFloatCommandFormat },
            { Commands.SetCalibrationCoefficient, ArrayFloatCommandFormat },
            { Commands.ToggleAcquisition, SimpleCommandFormat }
        };
        protected StringBuilder Buffer;
        protected readonly ParameterizedThreadStart DataErrorEventThreadStart;
        protected readonly ParameterizedThreadStart DeviceErrorThreadStart;
        protected bool _AcquisitionInProgress = false;
        protected bool _IsConnected = false;
        protected bool _Completed = true;
        protected object LockObject = new object();
        protected BlockingCollectionQueue TerminalQueue;
        protected BlockingCollectionQueue DataQueue;
#if TRACE
        protected static BlockingCollectionQueue TraceQueue = new BlockingCollectionQueue();
#endif

        protected static void Trace(string s)
        {
#if TRACE
            TraceQueue.Enqueue(() => { System.Diagnostics.Trace.WriteLine(string.Format("{0:mm.ss.ff} {1}", DateTime.UtcNow, s)); });
#endif
        }

#region Parser

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Trace("Data received");
            Buffer.Append(Port.ReadExisting());
            string read = Buffer.ToString();
            int i = read.IndexOf(NewLine);
            if (i > -1)
            {
                Buffer.Clear();
                while (i > -1)
                {
                    string line = read.Substring(0, i++).Trim(ToTrim); //Skip the new line character here
                    ParseLine(line);
                    read = read.Remove(0, i);
                    i = read.IndexOf(NewLine);
                }
                Buffer.Append(read);
            }
        }
        private void ParseLine(string line)
        {
            Trace("Parser invoked");
            TerminalQueue.Enqueue(() => { TerminalEvent?.Invoke(this, new TerminalEventArgs(line)); });
            if (line.EndsWith(ErrorDesignator))
            {
                new Thread(DeviceErrorThreadStart).Start(new TerminalEventArgs(line));
                return;
            }
            if (IsConnected)
            {
                if (line.Length == ConnectionSignature.Length)
                {
                    if (line.SequenceEqual(ConnectionSignature))
                    {
                        OnUnexpectedDisconnect();
                        return;
                    }
                }
                if (!CommandExecutionCompleted)
                {
                    if (line.Length == CompletionSignature.Length)
                    {
                        if (line.SequenceEqual(CompletionSignature))
                        {
                            CommandExecutionCompleted = true;
                            return;
                        }
                    }
                }
                if (AcquisitionInProgress)
                {
                    if (line.Length == EndOfAcquisitionSignature.Length)
                    {
                        if (line.SequenceEqual(EndOfAcquisitionSignature))
                        {
                            AcquisitionInProgress = false;
                            return;
                        }
                    }
                    try
                    {
                        string[] parsed = line.Split(Splitter);
                        int c = int.Parse(parsed[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        float v = float.Parse(parsed[1].Trim(ToTrim), CultureInfo.InvariantCulture);
                        DataQueue.Enqueue(() =>
                        {
                            AcquisitionDataReceived?.Invoke(this, new AcquisitionEventArgs(c, v));
                        });
                    }
                    catch (Exception exc)
                    {
                        new Thread(DataErrorEventThreadStart).Start(new DataErrorEventArgs(exc, line));
                    }
                }
                else
                {
                    if (line.Length == AcquisitionSignature.Length)
                    {
                        if (line.SequenceEqual(AcquisitionSignature))
                        {
                            AcquisitionInProgress = true;
                            return;
                        }
                    }
                }
            }
            else
            {
                if (line.Length == ConnectionSignature.Length)
                {
                    if (line.SequenceEqual(ConnectionSignature))
                    {
                        IsConnected = true;
                        return;
                    }
                }
            }
        }

#endregion

        private void Log(Exception e, string m)
        {
            new Thread(() => { LogEvent?.Invoke(this, new LogEventArgs(e, m)); }).Start();
        }
        /// <summary>
        /// Check for timeout
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="timeout"></param>
        /// <returns>True = timeout, False = ok</returns>
        private bool Wait(ref bool flag, int timeout)
        {
            int i = 0;
            while (!flag)
            {
                Thread.Sleep(1);
                if (i++ > timeout) return true;
            }
            return false;
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

        public Controller(SerialPortStream port)
        {
            Buffer = new StringBuilder(16);
            TerminalQueue = new BlockingCollectionQueue();
            DataQueue = new BlockingCollectionQueue();
            Port = port;
            Port.DataReceived += Port_DataReceived;
            DataErrorEventThreadStart = (object x) => { DataError?.Invoke(this, (DataErrorEventArgs)x); };
            DeviceErrorThreadStart = (object x) => { DeviceError?.Invoke(this, (TerminalEventArgs)x); };
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
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler AcquisitionFinished;
        public event EventHandler CommandCompleted;
        public event EventHandler UnexpectedDisconnect;

#region Properties

        public SerialPortStream Port { get; }
        public int ConnectionTimeout { get; set; } = 5000; //mS
        public int CompletionTimeout { get; set; } = 3000; //mS
        public int TerminalTimeout { get; set; } = 1000; //mS
        public bool IsConnected
        {
            get { return _IsConnected; }
            private set
            {
                _IsConnected = value;
                OnPropertyChanged();
            }
        }
        public bool CommandExecutionCompleted
        {
            get { return _Completed; }
            set
            {
                var b = _Completed;
                _Completed = value;
                OnPropertyChanged();
                if (_Completed && !b)
                    new Thread(() => { CommandCompleted?.Invoke(this, new EventArgs()); }).Start();
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

#endregion

#region Methods

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
            bool timeout = await Task.Run(() => { return Wait(ref _Completed, CompletionTimeout); });
            if (timeout)
            {
                Log(new TimeoutException(), Default.msgControllerTimeout);
                _Completed = true;
                return false;
            }
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
            TerminalQueue.Enqueue(() => { TerminalEvent?.Invoke(this, new TerminalEventArgs(cmd)); });
            return await Task.Run(() => { return !Wait(ref _Completed, CompletionTimeout); });
        }
        public async Task<bool> StartAcquisition(int duration = 0)
        {
            if (!AcquisitionInProgress)
                return await SendCommand(Commands.ToggleAcquisition, duration > 0 ? duration.ToString() : null);
            return false;
        }
        public async Task<bool> StopAcquisition()
        {
            if (AcquisitionInProgress) return await SendCommand(Commands.ToggleAcquisition);
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
            var result = await Task.Run(() => { return !Wait(ref _IsConnected, ConnectionTimeout); });
            if (result) _Completed = true;
            return result;
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
