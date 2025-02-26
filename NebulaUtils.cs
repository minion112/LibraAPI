using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace LibraServer
{
    class NebulaUtils
    {
        internal struct LoginResult
        {
            public bool Success { get; set; }
            public string AccessToken { get; set; }

            public string RefreshToken { get; set; }

            public string ProfileId { get; set; }

            public string Username { get; set; }
        }
 
        public static LoginResult Login(string username, string password, string pid = null, bool useProxy = true)
        {
            LoginResult loginResult = LoginAndGetResult(username, password, useProxy);
            bool flag = !loginResult.Success;
            LoginResult result;
            if (flag)
            {
                loginResult.Username = username;
                result = loginResult;
            }
            else
            {
                if (pid == null)
                    loginResult.ProfileId = GetProfileId(loginResult.AccessToken, LoginIdFromToken(loginResult.AccessToken));
                else
                    loginResult.ProfileId = pid;
                loginResult = RefreshAndGetResult(loginResult, useProxy);
                loginResult = RefreshAndGetResult(loginResult, useProxy);
                result = loginResult;
            }
            return result;
        }

        private static string GetProfileId(string token, string loginid)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers["Authorization"] = "Bearer " + token;
                string text = client.DownloadString(string.Concat(new string[]
                {
                "https://",
                NotEurope.Contains(Program.Server) ? "us" : "eu",
                ".mspapis.com/profileidentity/v1/logins/",
                loginid,
                "/profiles"
                }));

                return text.Split(new char[1]
                {
                '"'
                })[3];
            }
        }
        private static string LoginIdFromToken(string token)
        {
            string text = token.Split(new char[]
            {
                '.'
            })[1].Replace("-", "+").Replace("_", "/");
            for (int i = 0; i < text.Length % 4; i++)
            {
                text += "=";
            }
            return Encoding.ASCII.GetString(Convert.FromBase64String(text)).Split(new char[1]
            {
                '"'
            })[19];
        }
        private static LoginResult RefreshAndGetResult(LoginResult result, bool useProxy)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://" + (NotEurope.Contains(Program.Server) ? "us" : "eu") + "-secure.mspapis.com/loginidentity/connect/token");
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Headers["authorization"] = "Basic dW5pdHkuY2xpZW50OnNlY3JldA==";
            WebResponse webResponse = null;
            try
            {
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    using (StreamWriter streamWriter = new StreamWriter(requestStream))
                    {
                        streamWriter.Write("grant_type=refresh_token&refresh_token=" + result.RefreshToken + "&acr_values=gameId%3aywru%20profileId:" + result.ProfileId);
                    }
                }
                webResponse = httpWebRequest.GetResponse();
            }
            catch (Exception)
            {
                result.Success = false;
                return result;
            }
            string text = "";
            using (Stream responseStream = webResponse.GetResponseStream())
            {
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    text = streamReader.ReadToEnd();
                }
            }
            webResponse.Dispose();
            JObject jobject = JObject.Parse(text);
            result.AccessToken = jobject.GetValue("access_token").ToString();
            return result;
        }

        private static LoginResult LoginAndGetResult(string u, string p, bool useProxy)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://" + (NotEurope.Contains(Program.Server) ? "us" : "eu") + "-secure.mspapis.com/loginidentity/connect/token");
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Timeout = 6000;
            WebResponse webResponse = null;
            try
            {
                using (Stream requestStream = httpWebRequest.GetRequestStream())
                {
                    using (StreamWriter streamWriter = new StreamWriter(requestStream))
                    {
                        streamWriter.Write(string.Concat(

                            "client_id=unity.client&grant_type=password&username=",

                            u,
                            "&password=",
                            p
                        ));
                    }
                }
                webResponse = httpWebRequest.GetResponse();
            }
            catch (Exception)
            {
                return default(LoginResult);
            }
            LoginResult result = default(LoginResult);
            string text = "";
            using (Stream responseStream = webResponse.GetResponseStream())
            {
                using (StreamReader streamReader = new StreamReader(responseStream))
                {
                    text = streamReader.ReadToEnd();
                }
            }
            webResponse.Dispose();
            JObject jobject = JObject.Parse(text);
            result.Success = true;
            result.AccessToken = jobject.GetValue("access_token").ToString();
            result.RefreshToken = jobject.GetValue("refresh_token").ToString();
            result.Username = u.Split('|')[1];

            return result;
        }
        public static string[] NotEurope = new string[]
        {
            "AU",
            "NZ",
            "CA",
            "US"
        };

    }
}
