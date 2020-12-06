using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace controller_simulator
{
    public static class CLI
    {
        public static bool Parse(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(InitGlobals);
            return Success;
        }

        private static void InitGlobals(Options parsed)
        {
            Controller.Port.PortName = parsed.PortName;
            Console.WriteLine("Using port " + parsed.PortName);
            foreach (var item in parsed.ChannelCodes)
            {
                Controller.Channels.Add(new Channel(item));
                Console.WriteLine("Added channel " + item.ToString("X"));
            }
            Success = true;
        }

        private static bool Success = false;

        private class Options
        {
            [Option('c', "channels", Required = true)]
            public IEnumerable<int> ChannelCodes { get; set; }

            [Option('p', "port", Required = true)]
            public string PortName { get; set; }
        }
    }
}
