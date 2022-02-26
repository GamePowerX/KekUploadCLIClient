using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using ManyConsole;

namespace KekUploadCLIClient
{
    class Program
    {
        public static string version = "1.0.0";


        public static int Main(string[] args)
        {
            Console.WriteLine("KekUploadCLIClient v" + version + " made by CraftingDragon007 and KekOnTheWorld.");
            var commands = GetCommands();
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }
        
        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }
    }
}

