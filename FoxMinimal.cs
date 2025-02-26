using System.Net;
using System.Net.Sockets;
using Sfs2X.Core;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Util;

namespace LibraServer
{
    internal class FoxMinimal : IDisposable
    {
        NebulaUtils.LoginResult result;
        TcpClient client;
   
        public FoxMinimal(NebulaUtils.LoginResult _result)
        {
            result = _result;
            client = new TcpClient();
        
            using (WebClient web = new WebClient()) client.Connect(web.DownloadString($"https://central-{(NebulaUtils.NotEurope.Contains(Program.Server) ? "us" : "eu")}-alb.rbpapis.com/clusterstat/serverinfo").Split('"')[7], 843);
        }

    

        public void SendPacket(BaseRequest request)
        {
            request.Execute(null);
            ISFSObject isfsobject = new SFSObject();
            isfsobject.PutByte("c", Convert.ToByte(request.TargetController));
            isfsobject.PutShort("a", Convert.ToInt16(request.Id));
            isfsobject.PutSFSObject("p", request.Message.Content);
            ByteArray array = isfsobject.ToBinary();
            bool isCompressed = Constants.SHOULD_COMPRESS_BIG_PACKETS ? array.Length > 1024 : false;
            if (isCompressed) array.Compress();
            PacketHeader header = new PacketHeader(false, isCompressed, false, false);
            ByteArray ar = new ByteArray();
            ar.WriteByte(header.Encode());
            ar.WriteUShort((ushort)array.Length);
            ar.WriteBytes(array.Bytes);
            client.GetStream().Write(ar.Bytes);
            client.GetStream().Flush();
        }
        public byte[] ReadRaw()
        {
            
            byte[] buffer = new byte[1024]; // Adjust buffer size as needed
            using MemoryStream ms = new MemoryStream();

            int bytesRead;
            while ((bytesRead = client.GetStream().Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, bytesRead);

                // If no more data is available, break (basic assumption)
                if (!client.GetStream().DataAvailable)
                    break;
            }
            return ms.ToArray();
        }
        public ISFSObject HandleExtResponse()
        {
            var raw = ReadRaw();
            PacketHeader packetHeader = PacketHeader.FromBinary(raw[0]);
       
                byte[] newByteArray = new byte[raw.Length - 1 - (packetHeader.BigSized ? 4 : 2)];
                Buffer.BlockCopy(raw, (packetHeader.BigSized ? 4 : 2) + 1, newByteArray, 0,newByteArray.Length);
            
            ByteArray array = new ByteArray(newByteArray);
            if (packetHeader.Compressed) array.Uncompress();
    
            SFSObject @object = SFSObject.NewFromBinaryData(array);
            byte controllerId = @object.GetByte("c");
            if(controllerId != 1)
            {
                throw new Exception("This isn't Extension response!");
            }
            return @object.GetSFSObject("p").GetSFSObject("p");
        }
    
        public bool LoginToFox()
        {
          
            SFSObject o = SFSObject.NewInstance();
            o.PutUtfString("sre", Program.Server.ToLower());
            o.PutUtfString("pid", result.ProfileId);
            o.PutUtfString("ntk", result.AccessToken);
            o.PutUtfString("unc", Program.Server.ToUpper());
            o.PutUtfString("gv", "7.10.12");
            o.PutUtfString("os", "windows-desktop");
            LoginRequest request = new LoginRequest(NebulaUtils.NotEurope.Contains(Program.Server) ? $"{result.Username}_en-{Program.Server.ToUpper()}_persistent" : $"{result.Username}_{Program.Server.ToLower()}-{Program.Server.ToUpper()}_persistent", "", "BuilderGame", o);
            SendPacket(request);
            var response = ReadRaw();
            return response.Length > 100;
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
