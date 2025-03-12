using Sfs2X.Entities.Data;
using Sfs2X.Requests;

namespace LibraServer
{
    internal class FoxClient : FoxMinimal
    {
        public FoxClient( NebulaUtils.LoginResult _result) : base( _result)
        {
         //   SendPacket(new HandshakeRequest("1.7.15", null, "windows-desktop:7.10.12"));
         //   ReadRaw();
        }
        public int GetActorIdByProfileId(string profileId)
        {
            SFSObject o = SFSObject.NewInstance();
            o.PutUtfString("p", profileId);
            SendPacket(new ExtensionRequest("gui", o));
            var l = HandleExtResponse();
            return l.GetInt("u");
        }
        public Entry[] GetAllBlockStars(int actorid)
        {
            SFSObject o = SFSObject.NewInstance();
            o.PutInt("ai", actorid);
            o.PutUtfString("k", "browser_user_wall_robots");
            o.PutInt("s", 0);
            SendPacket(new ExtensionRequest("gbc", o));
            var ar = HandleExtResponse().GetSFSArray("c");
            Entry[] array = new Entry[ar.Count];
            for (int l = 0; l < ar.Count; l++)
            {
                var objec = ar.GetSFSObject(l);
                array[l] = new Entry
                {
                    Name = objec.GetUtfString("on"),
                    Id = objec.GetInt("r")
                }; 
             //  string cleanedText = Regex.Replace(objec.GetUtfString("on"), @"[^\u0000-\u007F]+", "");
               // ob[cleanedText] = objec.GetInt("r");
            }
            return array;
        }
    }
}
