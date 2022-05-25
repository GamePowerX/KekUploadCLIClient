using System.Security.Cryptography;
using KekUploadLibrary;
using ManyConsole;

namespace KekUploadCLIClient;

public class UploadCommand : ConsoleCommand
{
    private const int Success = 0;
    private const int Failure = 2;

    private string? FileLocation { get; set; }
    private string? ApiBaseUrl { get; set; }
    private int ChunkSize { get; set; }
    private bool Name { get; set; }

    public UploadCommand()
    {
        IsCommand("Upload", "Upload a File");
        HasLongDescription("Can be used to upload a File to a Server running KotwOSS/UploadServer");
        HasRequiredOption("f|file=", "The path of the file to upload.", p => FileLocation = p);
        HasRequiredOption("u|url=", "The base Api Url from the upload Server", p => ApiBaseUrl = p);
        HasOption("c|chunkSize=", "The Size of the Chunks for uploading (in KiB)", t => ChunkSize = t == null ? 1024*1024*2 : Convert.ToInt32(t));
        HasOption("s|silent=", "If the command should be executed silently", t =>{});
        var actionNameWasExecuted = false;
        HasOption("n|name=", "If the file should be uploaded with a name", t =>
        {
            Name = t == null || Convert.ToBoolean(t);
            actionNameWasExecuted = true;
        });
        if(!actionNameWasExecuted) Name = true;
    }

    public override int Run(string[] remainingArguments)
    {
        if (FileLocation == null || ApiBaseUrl == null) return Failure;
        var file = Path.GetFullPath(FileLocation);
        if(!File.Exists(file))
        {
            Program.WriteLine("File doesn't exist.");
            return Failure;
        }
        var fileInfo = new FileInfo(file);
        ChunkSize = ChunkSize <= 0 ? 1024 * 2 : ChunkSize;
        ChunkSize *= 1024;
        var client = new UploadClient(ApiBaseUrl, ChunkSize, Name);
        ProgressBar? progressBar = null;
        client.UploadStreamCreateEvent += (sender, args) =>
        {
            Program.WriteLine("Upload Stream ID: " + args.UploadStreamId);
            var size = Utils.SizeToString(fileInfo.Length);
            Program.WriteLine("File Size: " + size);
            Program.WriteLine("");
            progressBar = new ProgressBar();
        };
        var announcedChunks = false;
        client.UploadChunkCompleteEvent += (sender, args) =>
        {
            if (!announcedChunks)
            {
                announcedChunks = true;
                Program.WriteLine("Chunks: " + args.TotalChunkCount);
            }
            progressBar?.SetProgress((args.CurrentChunkCount+1) * 100 / (float)args.TotalChunkCount);
            Program.WriteLine("Chunk Hash: " + args.ChunkHash);
        };
        
        try
        {
            var url = client.Upload(new UploadItem(file));
            Program.WriteLine("");
            Program.WriteLine("Finished the upload! Download Url: " + url);
            if(Program.Silent) Console.WriteLine(url);
            return Success;
        }
        catch (KekException e)
        {
            Program.WriteLine("An error occured during upload: " + e.Message);
            Program.WriteLine("Exception: " + e.Message);
            if(e.Error!=null)
                Program.WriteLine("Server Response Error: " + e.Error);
            return Failure;
        }
    }
}