using System.Text;
using ManyConsole;

namespace KekUploadCLIClient;

public class Program
{
    private static TextWriter? _console;

    public static bool Silent { get; private set; }

    public static int Main(string[] args)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
        var version = fvi.FileVersion;
        
        var builder = new StringBuilder();
        foreach (var s in args)
        {
            builder.Append(s);
            builder.Append(' ');
        }

        var commands = GetCommands();
        if (builder.ToString().ToLower().Contains(" -s true") ||
            builder.ToString().ToLower().Contains(" --silent true"))
        {
            Silent = true;
            _console = TextWriter.Null;
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, TextWriter.Null);
        }

        Silent = false;
        _console = Console.Out;
        Console.WriteLine("KekUploadCLIClient v" + (version ?? "unknown") + " made by CraftingDragon007 and KekOnTheWorld.");
        return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
    }

    public static IEnumerable<ConsoleCommand> GetCommands()
    {
        return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
    }

    public static void WriteLine(string text)
    {
        _console?.WriteLine(text);
    }
}