using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LibraServer
{
    public struct Entry
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    internal class Program
    {

        static readonly List<NebulaUtils.LoginResult> botsList = new List<NebulaUtils.LoginResult>();
        public static string Server="PL";
        static void Main()
        {
            InitTCP();
            Console.ReadLine();
        }

        static bool CompressIfNeeded(ref byte[] bytes)
        {
            if (bytes.Length > Constants.MIN_BEFORE_COMPRESS) // Define your threshold for compression
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (DeflateStream deflate = new DeflateStream(ms, CompressionMode.Compress))
                    {
                        deflate.Write(bytes, 0, bytes.Length);
                    }
                    bytes = ms.ToArray();
                }
                return true;
            }
            return false;
        }

        static void HandleClient(TcpClient tcpClient)
        {
            var stream = tcpClient.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            while (true)
            {

                if (!stream.Socket.Connected) break;
                    string request = reader.ReadLine();
                if (string.IsNullOrEmpty(request)) continue;
                    Console.WriteLine($"Request: {request}");
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    string line;
                    string body = "";
                    while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                    {
                        Console.WriteLine(line);
                        string[] split = line.Split(':');
                        if (split.Length > 1)
                            headers[split[0]] = split[1].TrimStart();

                    }
                    if (headers.ContainsKey("Content-Length"))
                    {
                        byte[] ar = new byte[Convert.ToInt32(headers["Content-Length"])];
                        for (int i = 0; i < ar.Length; i++)
                        {
                            ar[i] = (byte)reader.Read();
                        }
                        body = Encoding.UTF8.GetString(ar);
                    }


                    string[] requestParts = request.Split(' ');
                    if (requestParts.Length < 3)
                    {
                        stream.Write(Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n\r\n"));
                    break ;
                    }

                    string method = requestParts[0]; // GET, POST, etc.
                    string url = requestParts[1]; // The URL requested
                    string version = requestParts[2]; // HTTP version (e.g., HTTP/1.1)

                    // Process the request based on URL and method
                    if (url == "/WebService/GetBlockStars")
                    {
                        HandleGetBlockStars(stream, body);
                    }
                    else if (url == "/WebService/GetBotsCount")
                    {
                        HandleGetBotsCount(stream);
                    }
                    else if (url == "/WebService/AddNewBotToPool")
                    {
                        HandleUpdateBotToPool(stream, body);
                    }
                    else if (url == "/")
                    {
                        HandleRootPage(stream);
                    }
                    else
                    {
                        string filePath = Directory.GetCurrentDirectory() + url;
                        if (File.Exists(filePath))
                        {
                            byte[] bytes = File.ReadAllBytes(filePath);
                            bool isCompressed = CompressIfNeeded(ref bytes);
                            stream.Write(Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: {GetMimeType(filePath)}\r\n{(isCompressed ? "Content-Encoding: deflate\r\n" : "")}Content-Length: {bytes.Length}\r\nConnection: keep-alive\r\n\r\n"));
                            stream.Write(bytes);
                        }
                        else
                        {
                            stream.Write(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\nConnection: keep-alive\r\n\r\n"));
                        break;
                        }


                    }
                    stream.Flush();


                
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
        static void HandleGetBlockStars( NetworkStream writer,string body)
        {
            // For simplicity, we assume blockstarsCache is populated

            var response = JObject.Parse(body);
            string profileid = response["profileid"].ToString();
            using(FoxClient fx = new FoxClient(botsList[0]))
            {
                fx.LoginToFox();
                int actorid = fx.GetActorIdByProfileId(profileid);
                string response2 = JsonConvert.SerializeObject(fx.GetAllBlockStars(actorid));
                string toreply = $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {response2.Length}\r\nConnection: keep-alive\r\n\r\n{response2}";
                writer.Write(Encoding.UTF8.GetBytes(toreply));

            }
           
        }

        static void HandleGetBotsCount(NetworkStream writer)
        {
            var response = JsonConvert.SerializeObject(new { botsList.Count });

            writer.Write(Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nConnection: keep-alive\r\nContent-Length: {response.Length}\r\n\r\n{response}"));
        }

        static void HandleUpdateBotToPool(NetworkStream writer, string body)
        {
          
            try
            {
                var result = JsonConvert.DeserializeObject<NebulaUtils.LoginResult>(body);
                lock (botsList)
                {
                    bool loc0 = false;
                    for (int i = 0; i < botsList.Count; i++)
                    {
                        if (botsList[i].Username == result.Username)
                        {
                            loc0 = true;
                            botsList.RemoveAt(i);
                            botsList.Add(result);
                            break;
                        }
                    }
                    if (!loc0) botsList.Add(result);
                }

                writer.Write(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nConnection: keep-alive\r\nContent-Length: 0\r\n\r\n"));
     
            }
            catch (Exception)
            {
                writer.Write(Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\nConnection: keep-alive\r\n\r\n"));
            }
        }

        static void HandleRootPage(NetworkStream writer)
        {
            // Serve an index.html file
            string filePath = "index.html";
            if (File.Exists(filePath))
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                bool isCompressed = CompressIfNeeded(ref bytes);
              
                writer.Write(Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n{(isCompressed? "Content-Encoding: deflate\r\n":"")}Content-Length: {bytes.Length}\r\nConnection: keep-alive\r\n\r\n"));
                writer.Write(bytes);
            }
            else
            {
                writer.Write(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\nConnection: keep-alive\r\n\r\n"));
               
            }
        }

        static void InitTCP()
        {
            // Create a TCP listener
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 20148);
            tcpListener.Start();
            Console.WriteLine("Server started!");

            // Handle incoming connections
            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(state => HandleClient(tcpClient));
            }
        }
    }
}
