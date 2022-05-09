using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using ManyConsole;

namespace KekUploadCLIClient
{
    class Program
    {
        public const string Version = "1.0.0.3";

        private static TextWriter? _console;

        public static int Main(string[] args)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var s in args)
            {
                builder.Append(s);
                builder.Append(" ");
            }
            
            var commands = GetCommands();
            if (builder.ToString().ToLower().Contains(" -s true") || builder.ToString().ToLower().Contains(" --silent true"))
            {
                Silent = true;
                _console = TextWriter.Null;
                return ConsoleCommandDispatcher.DispatchCommand(commands, args, TextWriter.Null);
            }
            Silent = false;
            _console = Console.Out;
            Console.WriteLine("KekUploadCLIClient v" + Version + " made by CraftingDragon007 and KekOnTheWorld.");
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }
        
        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }

        public static bool Silent { get; set; }
        public static void WriteLine(string text)
        {
            _console?.WriteLine(text);
        }
    }
}

