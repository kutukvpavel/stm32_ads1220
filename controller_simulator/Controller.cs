using System;
using System.Collections.Generic;
using System.Text;
using RJCP.IO.Ports;
using System.Timers;
using ScottPlot;

namespace controller_simulator
{
    public enum ControllerStates
    {
        Unconnected,
        Connected,
        Acquisition
    }

    public enum ControllerCommands : byte
    {
        ToggleAcquisition = (byte)'A'
    }

    public class Channel
    {
        public Channel(int code)
        {
            Code = code;
            Random = new Random();
            Data = DataGen.RandomWalk(Random, 500);
        }

        public int Code { get; }
        
        public double GetData()
        {
            if (Index >= Data.Length) Index = 0;
            return Data[Index++];
        }

        //Private

        private Random Random;
        private double[] Data;
        private int Index = 0;
    }

    public static class Controller
    {
        public static SerialPortStream Port { get; }
        public const int AcquisitionPeriod = 250;
        public const int BufferLength = 32;
        public const char LineBreak = '\n';
        public static readonly char[] Trim = { '\r' };
        public static ControllerStates State { get; private set; } = ControllerStates.Unconnected;
        public static readonly string ConnectionSignature = "READY...";
        public static readonly string AcquisitionSignature = "ACQ.";
        public static readonly string EndOfAcquisitionSignature = "END.";
        public static readonly string CompletionSignature = "PARSED.";
        public static readonly string AcquisitionDataFormat = "{0:X}: {1:F6}";
        public static List<Channel> Channels { get; }

        public static void Process()
        {
            try
            {
                if (!Port.IsOpen) return;
            }
            catch (NullReferenceException)
            {
                return;
            }
            if (State == ControllerStates.Unconnected)
            {
                State = ControllerStates.Connected;
                Send(ConnectionSignature);
            }
            else
            {
                Buffer.Append(Port.ReadExisting());
                var b = Buffer.ToString();
                var i = b.IndexOf(LineBreak);
                if (i > -1)
                {
                    Buffer.Remove(0, i + 1);
                    LineReceived(b.Substring(0, i).TrimEnd(Trim));
                }
            }
        }

        public static void Connect()
        {
            if (Port.IsOpen) Disconnect();
            Port.Open();
        }

        public static void Disconnect()
        {
            if (Port.IsOpen)
            {
                Port.Close();
            }
            State = ControllerStates.Unconnected;
        }

        //Private

        private static StringBuilder Buffer;
        private static Timer AcquisitionTimer;
        private static Timer RtcTimer;

        static Controller()
        {
            Channels = new List<Channel>();
            Buffer = new StringBuilder(32);
            Port = new SerialPortStream();
            AcquisitionTimer = new Timer(AcquisitionPeriod) { AutoReset = true, Enabled = true };
            AcquisitionTimer.Elapsed += AcquisitionTimer_Elapsed;
            RtcTimer = new Timer(1000) { AutoReset = false, Enabled = false };
            RtcTimer.Elapsed += RtcTimer_Elapsed;
        }

        private static void RtcTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (State == ControllerStates.Acquisition)
            {
                EndAcquisition();
            }
        }

        private static void AcquisitionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (State != ControllerStates.Acquisition) return;
            foreach (var item in Channels)
            {
                Send(string.Format(AcquisitionDataFormat, item.Code, item.GetData()));
            }
        }

        private static void LineReceived(string line)
        {
            switch ((ControllerCommands)line[0])
            {
                case ControllerCommands.ToggleAcquisition:
                    if (State != ControllerStates.Acquisition)
                    {
                        State = ControllerStates.Acquisition;
                        Send(AcquisitionSignature);
                        if (int.TryParse(line.Remove(0, 1), out int t)) RtcTimer.Interval = t * 1000;
                        RtcTimer.Start();
                    }
                    else
                    {
                        EndAcquisition();
                    }
                    break;
                default:
                    break;
            }
            Send(CompletionSignature);
        }

        private static void Send(string s)
        {
            Port.WriteLine(s);
        }

        private static void EndAcquisition()
        {
            RtcTimer.Stop();
            State = ControllerStates.Connected;
            Send(EndOfAcquisitionSignature);
        }
    }
}
