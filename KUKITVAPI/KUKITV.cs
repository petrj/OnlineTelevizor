using LoggerService;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TVAPI;

namespace KUKITVAPI
{
    public class KUKITV : ITVAPI
    {
        private ILoggingService _log;
        private StatusEnum _status = StatusEnum.NotInitialized;
        private DeviceConnection _connection = new DeviceConnection();
        private string _session_key = null;

        public KUKITV(ILoggingService loggingService)
        {
            _log = loggingService;
            _connection = new DeviceConnection();            
        }

        public DeviceConnection Connection
        {
            get
            {
                return _connection;
            }
        }

        public StatusEnum Status
        {
            get
            {
                return _status;
            }
        }

        public async Task Login(bool force = false)
        {
            _log.Debug($"Logging to KUKI");

            try
            {

                var sn = new Dictionary<string, string>();
                sn.Add("sn", _connection.deviceId);

                _status = StatusEnum.NotInitialized;

                // authorize:

                var authResponse = await SendRequest("https://as.kuki.cz/api/register", "POST", sn);
                var authResponseJson = JObject.Parse(authResponse);

                // get session key:
                if (
                      authResponseJson.HasValue("session_key")
                    )
                {
                    _session_key = authResponseJson.GetStringValue("session_key");
                    _status = StatusEnum.Logged;
                }
                else
                {
                    _status = StatusEnum.LoginFailed;
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Login failed");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Login failed");
                _status = StatusEnum.ConnectionNotAvailable;
            }
        }

        public async Task<List<Channel>> GetChanels()
        {
            var res = new List<Channel>();
                       
            var headerParams = new Dictionary<string, string>();
            headerParams.Add("X-SessionKey", _session_key);

            // get channels list:

            var channelsResponse = await SendRequest("https://as.kuki.cz/api/channels.json", "GET", null, headerParams);            
            var channelsJsonString = Regex.Split(channelsResponse, "},\\s{0,1}{");

            var number = 1;

            foreach (var channelJsonString in channelsJsonString)
            {
                var chJsonString = channelJsonString; 

                if (chJsonString.StartsWith("["))
                    chJsonString = chJsonString.Substring(1);

                if (chJsonString.EndsWith("]"))
                    chJsonString = chJsonString.Substring(0, chJsonString.Length-1);

                if (!chJsonString.StartsWith("{"))
                    chJsonString = "{" + chJsonString;

                if (!chJsonString.EndsWith("}"))
                    chJsonString = chJsonString + "}";

                var chJson = JObject.Parse(chJsonString);

                var ch = new Channel()
                {
                    ChannelNumber = number.ToString(),
                    Name = chJson.GetStringValue("name"),
                    Id = chJson.GetStringValue("timeshift_ident"),
                    Type = chJson.GetStringValue("stream_type"),
                    Locked = "none"                  
                    
                };

                ch.LogoUrl = "https://www.kuki.cz/media/chlogo/" + chJson.GetStringValue("epg_logo");

                var playTokenPostParams = new Dictionary<string, string>();
                playTokenPostParams.Add("type", "live");
                playTokenPostParams.Add("ident", ch.Id);

                // get play token:

                var playTokenResponse = await SendRequest("https://as.kuki.cz/api/play-token", "POST", playTokenPostParams, headerParams);
                var playTokenResponseJSon = JObject.Parse(playTokenResponse);

                var sign = playTokenResponseJSon.GetStringValue("sign");
                var expires = playTokenResponseJSon.GetStringValue("expires");

                ch.Url = $"http://media.kuki.cz:8116/{ch.Id}/stream.m3u8?sign={sign}&expires={expires}";

                res.Add(ch);

                number++;
            }

            return res;
        }

        public async Task<List<EPGItem>> GetEPG()
        {
            return new List<EPGItem>() {};
        }

        public async Task<List<Quality>> GetStreamQualities()
        {
            var q = new Quality()
            {
                Id = "0",
                Name = "Standard",
                Allowed = "1"
            };

            return new List<Quality>() { };
        }

        public void ResetConnection()
        {
            
        }

        public void SetConnection(string deviceId, string password)
        {
            _connection.deviceId = deviceId;
        }

        public void SetCredentials(string username, string password, string childLockPIN = null)
        {
           
        }

        public async Task Lock()
        {
            
        }

        public async Task Unlock()
        {            

        }

        private string GetRequestsString(Dictionary<string, string> p)
        {
            var url = "";
            var first = true;
            foreach (var kvp in p)
            {
                if (first)
                {
                    first = false;                    
                }
                else
                {
                    url += "&";
                }
                url += $"{kvp.Key}={kvp.Value}";
            }

            return url;
        }

        private async Task<string> SendRequest(string url, string method = "GET", Dictionary<string, string> postData = null, Dictionary<string, string> headers = null)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);

                var contentType = "application/x-www-form-urlencoded";

                request.Method = method;
                request.ContentType = contentType;
                request.Accept = "application/json";
                request.Timeout = 10 * 1000; // 10 sec timeout per one request

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
                
                _log.Debug($"Sending {method} request to url: {request.RequestUri}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"Method: {request.Method}");
                _log.Debug($"RequestUri: {request.RequestUri}");
                _log.Debug($"Timeout: {request.Timeout}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"ContentLength: {request.ContentLength}");

                if (postData != null)
                {
                    var postDataAsString = GetRequestsString(postData);

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(Encoding.ASCII.GetBytes(postDataAsString), 0, postDataAsString.Length);
                    }
                }

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    string responseString;
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = sr.ReadToEnd();
                    }
  
                    _log.Debug($"Response: {responseString}");
                    _log.Debug($"StatusCode: {response.StatusCode}");
                    _log.Debug($"StatusDescription: {response.StatusDescription}");

                    _log.Debug($"ContentLength: {response.ContentLength}");
                    _log.Debug($"ContentType: {response.ContentType}");
                    _log.Debug($"ContentEncoding: {response.ContentEncoding}");  

                    return responseString;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }

    }
}
