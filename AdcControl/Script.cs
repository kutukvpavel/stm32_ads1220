using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace AdcControl
{
    public class Script
    {
        public class Command
        {
            public int Offset { get; set; }
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public Controller.Commands Designator { get; set; }
            public float? Argument { get; set; }
        }

        static Script()
        {
            Options.Converters.Add(new JsonStringEnumConverter());
        }
        public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        public static Script Example { get; } = new Script(new List<Command>()
        {
            new Command() { Offset = 10, Designator = Controller.Commands.SetDacSetpoint, Argument = 1 },
            new Command() { Offset = 10, Designator = Controller.Commands.CompensateDacCurrent },
            new Command() { Offset = 10, Designator = Controller.Commands.SetDepolarizationPercent, Argument = 0.5f },
            new Command() { Offset = 5, Designator = Controller.Commands.SetDepolarizationSetpoint, Argument = 0.5f },
            new Command() { Offset = 10, Designator = Controller.Commands.SetDepolarizationPercent, Argument = 0 },
            new Command() { Offset = 10, Designator = Controller.Commands.SetDacSetpoint, Argument = 0 },
            new Command() { Offset = 10, Designator = Controller.Commands.ToggleAcquisition }
        });
        public static int RetryCommand { get; set; } = 3;

        private Script(List<Command> commands)
        {
            Stored = commands;
        }
        public Script(string json) : this(JsonSerializer.Deserialize<List<Command>>(json, Options))
        {
            var tmp = new Dictionary<int, Command>(Stored.Count);
            int time = 0;
            foreach (var item in Stored)
            {
                time += item.Offset;
                tmp.Add(time, item);
            }
            TotalTime = time;
            Parsed = new SortedDictionary<int, Command>(tmp);
        }

        SortedDictionary<int, Command> Parsed { get; }
        List<Command> Stored { get; }

        public int TotalTime { get; }

        public Command TryGetCommand(int time)
        {
            if (Parsed.TryGetValue(time, out Command cmd))
            {
                return cmd;
            }
            else
            {
                return null;
            }
        }

        public string Dump()
        {
            return JsonSerializer.Serialize(Stored, Options);
        }
    }
}
