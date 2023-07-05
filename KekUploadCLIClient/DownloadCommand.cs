using KekUploadLibrary;
using ManyConsole;

namespace KekUploadCLIClient;

public class DownloadCommand : ConsoleCommand
{
    private const int Success = 0;
    private const int Failure = 2;

    public DownloadCommand()
    {
        IsCommand("Download", "Download a File");
        HasLongDescription("Can be used to download a File from a Server running KotwOSS/UploadServer");
        HasRequiredOption("u|url=", "The base Download Url from the file", p => DownloadUrl = p);
        HasRequiredOption("f|file=", "The path to save the file to", p => FileLocation = p);
        HasOption("s|silent=", "If the command should be executed silently", t => { });
    }

    private string? FileLocation { get; set; }
    private string? DownloadUrl { get; set; }

    public override int Run(string[] remainingArguments)
    {
        if (DownloadUrl != null && FileLocation != null)
        {
            Program.WriteLine("Starting with the download!");
            Program.WriteLine("");
            var progressBar = new ProgressBar();

            var client = new DownloadClient();
            client.ProgressChangedEvent += (size, downloaded, percentage) =>
            {
                if (size != null)
                    Program.WriteLine("Downloaded " + Utils.SizeToString(downloaded) + " of " +
                                      Utils.SizeToString((long) size) + "!");
                else Program.WriteLine("Downloaded " + Utils.SizeToString(downloaded) + "!");

                progressBar.SetProgress((float) (percentage ?? 0));
            };
            try
            {
                client.Download(DownloadUrl, new DownloadItem(FileLocation));
                Program.WriteLine("Successfully downloaded file to: " + Path.GetFullPath(FileLocation));
                return Success;
            }
            catch (KekException e)
            {
                Program.WriteLine("Could not download the file! Are you sure you entered a correct url?");
                Program.WriteLine("Exception: " + e.Message);
                if (e.Error != null)
                    Program.WriteLine("Server Response Error: " + e.Error);
                return Failure;
            }
        }

        Program.WriteLine("Please enter a valid url and a file location!");
        return Failure;
    }
}