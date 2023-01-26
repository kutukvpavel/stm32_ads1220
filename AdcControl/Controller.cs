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

namespace AdcControl
{
    public class Controller : INotifyPropertyChanged
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
        protected BlockingCollectionQueue TerminalQueue;
        protected BlockingCollectionQueue DataQueue;
        protected SerialPortStreamAdapter Adapter;
        protected IModbusMaster Master;
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
            TerminalQueue = new BlockingCollectionQueue();
            DataQueue = new BlockingCollectionQueue();
            Port = port;
            DataErrorEventThreadStart = (object x) => { DataError?.Invoke(this, (DataErrorEventArgs)x); };
            DeviceErrorThreadStart = (object x) => { DeviceError?.Invoke(this, (TerminalEventArgs)x); };
        }

        public event EventHandler<AcquisitionEventArgs> AcquisitionDataReceived;
        public event EventHandler<DataErrorEventArgs> DataError;
        public event EventHandler<TerminalEventArgs> DeviceError;
        public event EventHandler<LogEventArgs> LogEvent;
        public event EventHandler<TerminalEventArgs> TerminalEvent;
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler AcquisitionFinished;
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

#endregion
}
