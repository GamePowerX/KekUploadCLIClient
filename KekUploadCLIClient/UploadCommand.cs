using System.Security.Cryptography;
using ManyConsole;

namespace KekUploadCLIClient;

public class UploadCommand : ConsoleCommand
{
    private const int Success = 0;
    private const int Failure = 2;
    
    public string FileLocation { get; set; }
    public string ApiBaseUrl { get; set; }
    public int ChunkSize { get; set; }

    public UploadCommand()
    {
        IsCommand("Upload", "Upload a File");
        HasLongDescription("Can be used to upload a File to a Server running KotwOSS/UploadServer");
        HasRequiredOption("f|file=", "The path of the file to upload.", p => FileLocation = p);
        HasRequiredOption("u|url=", "The base Api Url from the upload Server", p => ApiBaseUrl = p);
        HasOption("s|size=", "The Size of the Chunks for uploading (in KiB)", t => ChunkSize = t == null ? 1024*1024*2 : Convert.ToInt32(t));
    }

    public override int Run(string[] remainingArguments)
    {
        try
        {
            var file = Path.GetFullPath(FileLocation);
            if(!File.Exists(file))
            {
                Console.WriteLine("File doesn't exist.");
                return Failure;
            }
            var fileInfo = new FileInfo(file);
            var client = new HttpClient();

            var request = new HttpRequestMessage() {
                RequestUri = new Uri(ApiBaseUrl + "/c/" + fileInfo.Extension.Substring(1)),
                Method = HttpMethod.Post
            };

            var responseMessage = client.Send(request);

            if(!responseMessage.IsSuccessStatusCode)
            {
                Console.WriteLine("Could not create uploadstream!");
                return Failure;
            } 

            var uploadStreamId = new StreamReader(responseMessage.Content.ReadAsStream()).ReadToEnd();

            Console.WriteLine("Upload Stream ID: " + uploadStreamId);
            
            var size = SizeToString(fileInfo.Length);
            
            Console.WriteLine("File Size: " + size);
            
            var stream = File.OpenRead(FileLocation);

            var fileSize = fileInfo.Length;
            ChunkSize = ChunkSize == 0 ? 1024 * 2 : ChunkSize;
            int maxChunkSize = 1024 * ChunkSize;
            var chunks = (int)Math.Ceiling(fileSize/(double)maxChunkSize);

            Console.WriteLine("Chunks: " + chunks);
            Console.WriteLine();

            ProgressBar progressBar = new ProgressBar();
            
            for(int chunk = 0; chunk < chunks; chunk++) {
                var chunkSize = Math.Min(stream.Length-chunk*maxChunkSize, maxChunkSize);

                byte[] buf = new byte[chunkSize];

                int readBytes = 0;
                while(readBytes < chunkSize) readBytes += stream.Read(buf, readBytes, (int)Math.Min(stream.Length-(readBytes+chunk*chunkSize), chunkSize));

                var hashs = HashChunk(buf);
                Console.WriteLine("Chunk Hash: " + hashs);

                // index is the number of bytes in the chunk
                var uploadRequest = new HttpRequestMessage {
                    RequestUri = new Uri(ApiBaseUrl + "/u/" + uploadStreamId + "/" + hashs),
                    Method = HttpMethod.Post,
                    Content = new ByteArrayContent(buf)
                };

                var responseMsg = client.Send(uploadRequest);
                if (!responseMsg.IsSuccessStatusCode)
                {
                    Console.WriteLine("Some error i dont want to show u lol.");
                    return Failure;
                }
                progressBar.SetProgress((chunk+1) * 100 / (float)chunks);
                //DrawTextProgressBar(chunk + 1, chunks);
            }
            
            progressBar.Dispose();
            
            var hash = HashFile(file);

            Console.WriteLine("File Hash: " + hash);

            var finishRequest = new HttpRequestMessage {   
                RequestUri = new Uri(ApiBaseUrl + "/f/" + uploadStreamId + "/" + hash),
                Method = HttpMethod.Post
            };

            var finishResponse = client.Send(finishRequest);
            if (!finishResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("SHIT WHAT THE FUCK HAPPENED?");
                return Failure;
            }

            var downloadId = finishResponse.Content.ReadAsStringAsync().Result;
            
            Console.WriteLine("Finished the upload! Download Url: " + ApiBaseUrl + "/e/" + downloadId);
            return Success;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);

            return Failure;
        }
    }
    
    private static string HashChunk(byte[] chunk) {
        var hash = SHA1.Create().ComputeHash(chunk);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    private static string HashFile(string file) {
        var stream = File.OpenRead(file);
        var hash = SHA1.Create().ComputeHash(stream);
        return string.Concat(hash.Select(b => b.ToString("x2")));
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