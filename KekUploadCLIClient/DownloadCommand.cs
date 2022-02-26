using ManyConsole;

namespace KekUploadCLIClient;

public class DownloadCommand : ConsoleCommand
{
    private const int Success = 0;
    private const int Failure = 2;
    
    public string FileLocation { get; set; }
    public string DownloadUrl { get; set; }

    public DownloadCommand()
    {
        IsCommand("Download", "Download a File");
        HasLongDescription("Can be used to download a File from a Server running KotwOSS/UploadServer");
        HasRequiredOption("u|url=", "The base Download Url from the file", p => DownloadUrl = p);
        HasRequiredOption("f|file=", "The path to save the file to", p => FileLocation = p);
    }

    public override int Run(string[] remainingArguments)
    {
        Console.WriteLine("Starting with the download!");
        Console.WriteLine();
        ProgressBar progressBar = new ProgressBar();
            

        var downloadUrl = DownloadUrl.Replace("/e/", "/d/");
            
        var client = new HttpClientDownloadWithProgress(downloadUrl, FileLocation);
        client.ProgressChanged += (size, downloaded, percentage) =>
        {
            if (size != null)
            {
                Console.WriteLine("Downloaded " + SizeToString(downloaded) + " of " + SizeToString((long)size) + "!");
            }else Console.WriteLine("Downloaded " + SizeToString(downloaded) + "!");
            progressBar.SetProgress((float)(percentage != null ? percentage : 0));
        };
        Task task = client.StartDownload();
        task.Wait();
        progressBar.Dispose();
        if (task.IsCompletedSuccessfully)
        {
            Console.WriteLine("Successfully downloaded file to: " + Path.GetFullPath(FileLocation));
            return Success; 
        }
        else
        {
            Console.WriteLine("Could not download the file! Are you sure you entered a correct url?");
            return Failure;
        }
    }

    private static string SizeToString(long size) {
        if(size >= 1099511627776) {
            return decimal.Round((decimal)(Math.Round(size / 10995116277.76)*0.01), 2) + " TiB";
        } else if(size >= 1073741824) {
            return decimal.Round((decimal)(Math.Round(size / 10737418.24)*0.01), 2) + " GiB";
        } else if(size >= 1048576) {
            return decimal.Round((decimal)(Math.Round(size / 10485.76)*0.01), 2) + " MiB";
        } else if(size >= 1024) {
            return decimal.Round((decimal)(Math.Round(size / 10.24)*0.01), 2) + " KiB";
        } else return size + " bytes";
    }
}