using System;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;


namespace controller_simulator
{
    public static class Program
    {
        static readonly Timer PollTimer = new Timer(100) { AutoReset = true, Enabled = false };
        static readonly Thread ExecutionThread = new Thread(ProcessController);
        static readonly CancellationTokenSource ExecutionCancellationTokenSource = new CancellationTokenSource();

        static int Main(string[] args)
        {
            if (!CLI.Parse(args)) return -1;
            PollTimer.Elapsed += PollTimer_Elapsed;
            ExecutionThread.Start(ExecutionCancellationTokenSource.Token);
            PollTimer.Start();
            var k = ConsoleKey.NoName;
            while (k != ConsoleKey.Escape)
            {
                switch (k)
                {
                    case ConsoleKey.Enter:
                        Controller.Connect();
                        break;
                    default: break;
                }
                k = Console.ReadKey().Key;
            }
            if (ExecutionThread.IsAlive)
            {
                ExecutionCancellationTokenSource.Cancel();
            }
            while (ExecutionThread.IsAlive) ;
            return 0;
        }

        private static void PollTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!ExecutionThread.IsAlive)
            {
                Console.WriteLine(" Execution thread exited.");
                PollTimer.Stop();
            }
        }

        private static void ProcessController(object token)
        {
            while (!((CancellationToken)token).IsCancellationRequested)
            {
                Controller.Process();
            }
        }
    }
}
