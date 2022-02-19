using System.Security.Cryptography;

namespace KekUploadCLIClient
{
    class Program
    {
        public static String version = "1.0.0";


        public static void Main(string[] args) {
            Console.WriteLine(MainLoop(args));
        }

        public static string MainLoop(string[] args) {
            if(args.Length == 0) return GetHelp();

            switch(args[0].ToLower()) {
                case "upload":
                    if(args.Length < 3) return GetHelp();

                    var file = Path.GetFullPath(args[1]);
                    if(!File.Exists(file)) return "File doesn't exist.";
                    return Upload(file, args[2]);

                case "download":
                    if(args.Length < 3) return GetHelp();

                    return Download(args[0], args[1]);

                case "help": 
                    return GetHelp();

                default:
                    return "Subcommand '" + args[0]  + "' not found! Use 'kup help' to get a list of possible options.";
            }
        }

        public static string Download(string url, string output) {
            var client = new HttpClient();

            var downloadUrl = url.Replace("/e/", "/d/");
            
            var downloadRequest = new HttpRequestMessage {
                RequestUri = new Uri(downloadUrl),
                Method = HttpMethod.Get
            };

            var fileStream = File.OpenWrite(output);
            var response = client.Send(downloadRequest);

            if (response.IsSuccessStatusCode) {
                response.Content.ReadAsStream().CopyTo(fileStream);
                return "Successfully downloaded file to: " + Path.GetFullPath(output);
            } else return "Could not download the file! Are you sure you entered a correct url?";
        }

        public static string Upload(string file, string baseapi) {
            var fileInfo = new FileInfo(file);
            var client = new HttpClient();

            var request = new HttpRequestMessage() {
                RequestUri = new Uri(baseapi + "/c/" + fileInfo.Extension.Substring(1)),
                Method = HttpMethod.Post
            };

            var responseMessage = client.Send(request);

            if(!responseMessage.IsSuccessStatusCode) return "Could not create uploadstream!";

            var uploadStreamId = new StreamReader(responseMessage.Content.ReadAsStream()).ReadToEnd();

            Console.WriteLine("Upload Stream ID: " + uploadStreamId);
            
            var size = SizeToString(fileInfo.Length);
            
            Console.WriteLine("File Size: " + size);
            
            var stream = File.OpenRead(file);

            var fileSize = fileInfo.Length;
            const int maxChunkSize = 1024*1024*2;
            var chunks = (int)Math.Ceiling(fileSize/(double)maxChunkSize);

            Console.WriteLine("Chunks: " + chunks);

            for(int chunk = 0; chunk < chunks; chunk++) {
                var chunkSize = Math.Min(stream.Length-chunk*maxChunkSize, maxChunkSize);

                byte[] buf = new byte[chunkSize];

                int readBytes = 0;
                while(readBytes < chunkSize) readBytes += stream.Read(buf, readBytes, (int)Math.Min(stream.Length-(readBytes+chunk*chunkSize), chunkSize));

                var hashs = HashChunk(buf);
                Console.WriteLine("Chunk Hash: " + hashs);

                // index is the number of bytes in the chunk
                var uploadRequest = new HttpRequestMessage {
                    RequestUri = new Uri(baseapi + "/u/" + uploadStreamId + "/" + hashs),
                    Method = HttpMethod.Post,
                    Content = new ByteArrayContent(buf)
                };

                var responseMsg = client.Send(uploadRequest);
                if (!responseMsg.IsSuccessStatusCode) return "Some error i dont want to show u lol.";
            }


            var hash = HashFile(file);

            Console.WriteLine("File Hash: " + hash);

            var finishRequest = new HttpRequestMessage {   
                RequestUri = new Uri(baseapi + "/f/" + uploadStreamId + "/" + hash),
                Method = HttpMethod.Post
            };

            var finishResponse = client.Send(finishRequest);
            if (!finishResponse.IsSuccessStatusCode) return "SHIT WHAT THE FUCK HAPPENED?";

            var downloadId = finishResponse.Content.ReadAsStringAsync().Result; 
            
            return "Finished the upload! Download Url: " + baseapi + "/e/" + downloadId;
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
                return Math.Round(size / 10995116277.76)*0.01 + " TiB";
            } else if(size >= 1073741824) {
                return Math.Round(size / 10737418.24)*0.01 + " GiB";
            } else if(size >= 1048576) {
                return Math.Round(size / 10485.76)*0.01 + " MiB";
            } else if(size >= 1024) {
                return Math.Round(size / 10.24)*0.01 + " KiB";
            } else return size + " bytes";
        }

        public static string GetHelp() {
            return @"
KekUploadCLIClient v" + version + @" made by CraftingDragon007 and KekOnTheWorld.

kup help
    Shows this list

kup upload <file> <base api url>
    Uploads a file

kup download <url> <destination>
    Downloads a file
            ";
        }
    }
}

