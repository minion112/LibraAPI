using System.IO.Compression;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LibraServer
{
    public struct Entry  
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    internal class Program
    {
        static readonly HttpListener listener = new HttpListener();

        public static string Server = "PL";
        static NebulaUtils.LoginResult result;
        static readonly ItemsCache<Entry> blockstarsCache = new ItemsCache<Entry>(); 
        static void Main()
        {
        
            var lr = NebulaUtils.Login("PL|ZolwikPoranek10", "bsp321", useProxy: false);
            if (lr.Success)
            {
                result = lr;
                InitHTTP();
             

            }
               
            Console.ReadLine();
        }

        static void CompressIfNeeded(ref HttpListenerResponse response,ref byte[] bytes)
        {
            if (bytes.Length > Constants.MIN_BEFORE_COMPRESS)
            {
                response.AddHeader("Content-Encoding", "deflate");
                using (MemoryStream ms = new MemoryStream())
                {
                    using (DeflateStream deflate = new DeflateStream(ms, CompressionMode.Compress))
                    {
                         deflate.Write(bytes, 0, bytes.Length);
                    }
                    bytes = ms.ToArray();
                }
            }
        }
        static async Task HandleClient(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            if (request.Url?.AbsolutePath == "/WebService/GetBlockStars")
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    request.InputStream.CopyTo(ms);
                    ms.Position = 0;
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        try
                        {
                            string profileId = JObject.Parse(json).GetValue("profileid").ToString();
                         
                            if (blockstarsCache.HasItemWithName(profileId))
                            {
                                string respons = JsonConvert.SerializeObject(blockstarsCache.GetItem(profileId));
                                byte[] bajty = Encoding.UTF8.GetBytes(respons);
                                CompressIfNeeded(ref response, ref bajty);
                                response.StatusCode = (int)HttpStatusCode.OK;
                                response.ContentType = "application/json";
                                response.ContentLength64 = bajty.Length;
                                response.OutputStream.Write(bajty);
                            }
                            else
                            {
                                using (var primaryBot = new FoxClient(result))
                                {
                                    int actorid = primaryBot.GetActorIdByProfileId(profileId);
                                    var blockstars = primaryBot.GetAllBlockStars(actorid);
                                    blockstarsCache.AddItems(profileId, blockstars);
                                    string respons = JsonConvert.SerializeObject(blockstars);
                                    byte[] bajty = Encoding.UTF8.GetBytes(respons);
                                    CompressIfNeeded(ref response, ref bajty);
                                    response.StatusCode = (int)HttpStatusCode.OK;
                                    response.ContentType = "application/json";
                                    response.ContentLength64 = bajty.Length;
                                    response.OutputStream.Write(bajty);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            response.OutputStream.Write(Encoding.UTF8.GetBytes(ex.Message));
                        }
                                
                           
                        
                        
                    }

                }

            }
            else
               if (request.Url?.AbsolutePath == "/")
            {
                response.ContentType = "text/html";
                response.StatusCode = (int)HttpStatusCode.OK;
                byte[] bytes = File.ReadAllBytes("index.html");

                CompressIfNeeded(ref response, ref bytes);

                response.ContentLength64 = bytes.Length;
                response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                string filePath = Directory.GetCurrentDirectory() + request.Url.AbsolutePath;
                if (File.Exists(filePath))
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    response.ContentType = GetMimeType(filePath);

                    CompressIfNeeded(ref response, ref bytes);
                    response.ContentLength64 = bytes.Length;
                    response.StatusCode = (int)HttpStatusCode.OK;
                   response.OutputStream.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }

            response.OutputStream.Flush();
            response.Close();
        }
        static async void InitHTTP()
        {
          
            listener.Prefixes.Add("http://192.168.0.141:80/");
            listener.Start();
            Console.WriteLine("Server started at http://192.168.0.141:80/");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
             await HandleClient(context);

                //if (request.Url.AbsolutePath == "/events")
                //{
                //    Console.WriteLine("Client connected to SSE stream");

                //    response.ContentType = "text/event-stream";
                //    response.Headers.Add("Cache-Control", "no-cache");
                //    response.Headers.Add("Connection", "keep-alive");
                //    response.StatusCode = (int)HttpStatusCode.OK;

                //    var output = response.OutputStream;

                //    lock (eventSubscribers)
                //    {
                //        eventSubscribers.Add(output);
                //    }

                //    // Keep connection alive (prevent response.Close() from being called)
                //    await Task.Delay(-1);
                //}
               
            }
        }

        static string GetMimeType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}
