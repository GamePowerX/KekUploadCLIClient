using System.Security.Cryptography;

namespace KekUploadCLIClient
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length >= 3)
            {
                if (args[0].ToLower().Equals("upload"))
                {
                    var file = Path.GetFullPath(args[1]);
                    if (File.Exists(file))
                    {
                        var fileInfo = new FileInfo(file);
                        var client = new HttpClient();
                        var request = new HttpRequestMessage()
                        {
                            RequestUri = new Uri(args[2] + "/c/" + fileInfo.Extension),
                            Method = HttpMethod.Post
                        };
                        var responseMessage = client.Send(request);
                        if (responseMessage.IsSuccessStatusCode)
                        {
                            var reader = new StreamReader(responseMessage.Content.ReadAsStream());
                            var uploadStreamId = reader.ReadToEnd();
                            Console.WriteLine("Upload Stream ID: " + uploadStreamId);
                            var size = ConvertBytesToMegabytes(fileInfo.Length);
                            Console.WriteLine("File Size: " + size + " MiB");
                            var chunkSize = 1048576 * 2;
                            var chunkCount = (int)Math.Ceiling(fileInfo.Length / (double)chunkSize);
                            Console.WriteLine("Total Chunk Count: " + chunkCount);
                            var stream = File.OpenRead(file);
                            for (int i = 0; i < chunkCount; i++)
                            {
                                byte[] chunk = new byte[chunkSize];
                                Console.WriteLine("Offset: " + i * chunkSize);
                                Console.WriteLine("Stream Lenght: " + stream.Length);
                                var offset = i * chunkSize;
                                int index = 0;
                                while (index < chunkSize)
                                {
                                    int bytesRead = stream.Read(chunk, offset + index, 1);
                                    if (bytesRead == 0)
                                    {
                                      break;
                                    }
                                    index += bytesRead;
                                }
                                if (index != 0) // Our previous chunk may have been the last one
                                {
                                    var hash = HashChunk(chunk);
                                    Console.WriteLine("Chunk Hash: " + hash);
                                    // index is the number of bytes in the chunk
                                    var uploadRequest = new HttpRequestMessage
                                    {
                                        RequestUri = new Uri(args[2] + "/u/" + uploadStreamId + "/" + hash),
                                        Method = HttpMethod.Post,
                                        Content = new ByteArrayContent(chunk)
                                    };
                                    var responseMsg = client.Send(uploadRequest);
                                    if (!responseMsg.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine("An Error occured whilst uploading a chunk! Status Code:" + responseMessage.StatusCode);
                                        Console.WriteLine("Result: " + responseMsg.Content.ReadAsStringAsync().Result);
                                        return;
                                    }
                                }
                                if (index != chunk.Length) // We didn't read a full chunk: we're done
                                {
                                    var hash = HashFile(file);
                                    Console.WriteLine("Hash: " + hash);
                                    var finishRequest = new HttpRequestMessage
                                    {   
                                        RequestUri = new Uri(args[2] + "/f/" + uploadStreamId + "/" + SHA1.Create().ComputeHash(File.OpenRead(file)).ToString()),
                                        Method = HttpMethod.Post
                                    };
                                    var finishResponse = client.Send(finishRequest);
                                    if (!finishResponse.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine("An Error occured whilst finishing the upload! Status Code:" + responseMessage.StatusCode);
                                        Console.WriteLine("Result: " + finishResponse.Content.ReadAsStringAsync().Result);
                                        return;
                                    }

                                    var downloadId = finishResponse.Content.ReadAsStringAsync().Result; 
                                    Console.WriteLine("Finished the upload! Download Url: " + args[2] + "/e/" + downloadId);
                                }
                            }
                        }else Console.WriteLine("Could not successfully contact upload server! Are you sure you entered a correct url?");
                    }else Console.WriteLine("Please enter a valid file name!");
                }else if (args[0].ToLower().Equals("download"))
                {
                    var client = new HttpClient();
                    var downloadUrl = args[1].Replace("/e/", "/d/");
                    var downloadRequest = new HttpRequestMessage
                    {
                        RequestUri = new Uri(downloadUrl),
                        Method = HttpMethod.Get
                    };
                    var fileStream = File.OpenWrite(args[2]);
                    var response = client.Send(downloadRequest);
                    if (response.IsSuccessStatusCode)
                    {
                        response.Content.ReadAsStream().CopyTo(fileStream);
                        Console.WriteLine("Successfully downloaded file to: " + Path.GetFullPath(args[2]));
                    }else Console.WriteLine("Could not download the file! Are you sure you entered a correct url?");
                }else Console.WriteLine("Please enter valid arguments! For help enter help!");
            }else if (args.Length > 0)
            {
                if (args[0].ToLower().Equals("help"))
                {
                    Console.WriteLine("Possible arguments are download and upload! Examples: upload ./kek.png https://u.kotw.dev OR download https://u.kotw.dev/e/xxxxxx ./kek.png");
                }else Console.WriteLine("Please enter a valid argument! Enter help for help!");
            }
            else
            {
                Console.WriteLine("Please enter a valid argument! Enter help for help!");
            }
        }

        private static string HashFile(string file)
        {
            var stream = File.OpenRead(file);
            var hash = SHA1.Create().ComputeHash(stream);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        private static string HashChunk(byte[] chunk)
        {
            var hash = SHA1.Create().ComputeHash(chunk);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        private static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        private static double ConvertKilobytesToMegabytes(long kilobytes)
        {
            return kilobytes / 1024f;
        }
    }
}

