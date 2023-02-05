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

namespace DVBStreamerAPI
{
    public class DVBStreamerClient : ITVAPI
    {
        private ILoggingService _log;
        private StatusEnum _status = StatusEnum.NotInitialized;
        private DeviceConnection _connection = new DeviceConnection();
        private string _url = null;

        public DVBStreamerClient(ILoggingService loggingService)
        {
            _log = loggingService;
        }

        public string LastErrorDescription { get; set; } = String.Empty;

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

        public bool EPGEnabled
        {
            get
            {
                return false;
            }
        }

        public bool QualityFilterEnabled
        {
            get
            {
                return false;
            }
        }

        public bool AdultLockEnabled
        {
            get
            {
                return false;
            }
        }

        public async Task Login(bool force = false)
        {
            _log.Info($"DVBStreamer login");

            if (String.IsNullOrEmpty(_url))
            {
                _status = StatusEnum.EmptyCredentials;
                return;
            }

            try
            {
                var res = await SendRequest(new Dictionary<string, string> { { "action", "getServiceState" } });
                var resJson = JObject.Parse(res);

                // get session key:
                if (
                      resJson.HasValue("serviceState")
                    )
                {
                    _status = StatusEnum.Logged;
                }
                else
                {
                    _status = StatusEnum.ConnectionNotAvailable;
                }

            }
            catch (Exception wex)
            {
                _log.Error(wex, "Login failed");
                _status = StatusEnum.ConnectionNotAvailable;
            }
        }

        public async Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG()
        {
            var epg = await GetEPG();

            var res = new Dictionary<string, List<EPGItem>>();
            foreach (var epgItem in epg)
            {
                res.Add(epgItem.ChannelId, new List<EPGItem>());
            }

            return res;
        }

        public async Task<List<Channel>> GetChannels(string quality = null)
        {
            var res = new List<Channel>();

            await Login();

            if (_status != StatusEnum.Logged)
                return res;

            try
            {
                // get channels list:

                var channelsResponse = await SendRequest(new Dictionary<string, string> { { "action", "listChannels" } });
                var channelsJsonString = Regex.Split(channelsResponse, "},\\s{0,1}{");

                var number = 1;

                foreach (var channelJsonString in channelsJsonString)
                {
                    var chJsonString = channelJsonString;

                    if (chJsonString.StartsWith("["))
                        chJsonString = chJsonString.Substring(1);

                    if (chJsonString.EndsWith("]"))
                        chJsonString = chJsonString.Substring(0, chJsonString.Length - 1);

                    if (!chJsonString.StartsWith("{"))
                        chJsonString = "{" + chJsonString;

                    if (!chJsonString.EndsWith("}"))
                        chJsonString = chJsonString + "}";

                    var chJson = JObject.Parse(chJsonString);

                    var ch = new Channel()
                    {
                        ChannelNumber = number.ToString(),
                        Name = chJson.GetStringValue("name"),
                        Id = chJson.GetStringValue("id"),
                        Type = chJson.GetStringValue("type"),
                        LogoUrl = chJson.GetStringValue("logo"),
                        Url = chJson.GetStringValue("url"),
                        Locked = "none",
                        Group = ""
                    };

                    res.Add(ch);

                    number++;
                }
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while getting channels");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while getting channels");
                _status = StatusEnum.GeneralError;
            }

            return res;
        }

        public async Task<List<EPGItem>> GetEPG()
        {
            return new List<EPGItem>() { };
        }

        public async Task<string> GetEPGItemDescription(EPGItem epgItem)
        {
            return String.Empty;
        }

        public async Task<List<Quality>> GetStreamQualities()
        {
            var res = new List<Quality>();
            var qualities = new Dictionary<int, string>()
            {
                { 0, "original" },
                { 1, "800x520" },
                { 2, "710x400" },
                { 3, "530x300" },
                { 4, "355x200" },
                { 5, "220x120" },
                { 6, "142x80" },
                { 7, "88x50" },
            };

            foreach (var kvp in qualities)
            {
                var q = new Quality()
                {
                    Id = kvp.Key.ToString(),
                    Name = kvp.Value,
                    Allowed = "1"
                };

                res.Add(q);
            }

            return res;
        }

        public void ResetConnection()
        {

        }

        public void SetConnection(string deviceId, string password)
        {
            _url = deviceId;
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
            var url = "?";
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

        private async Task<string> SendRequest(Dictionary<string, string> urlParams = null)
        {
            try
            {
                var url = _url + "/api.php" + GetRequestsString(urlParams);

                var request = (HttpWebRequest)WebRequest.Create(url);

                var contentType = "application/x-www-form-urlencoded";

                request.Method = "GET";
                request.ContentType = contentType;
                request.Accept = "application/json";
                request.Timeout = 10 * 1000; // 10 sec timeout per one request

                _log.Debug($"Sending request to url: {request.RequestUri}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"Method: {request.Method}");
                _log.Debug($"RequestUri: {request.RequestUri}");
                _log.Debug($"Timeout: {request.Timeout}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"ContentLength: {request.ContentLength}");

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

        public async Task Stop()
        {
            try
            {
                await SendRequest(new Dictionary<string, string> { { "action", "stop" } });
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while getting channels");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while getting channels");
                _status = StatusEnum.GeneralError;
            }
        }

        public bool SubtitlesEnabled
        {
            get
            {
                return false;
            }
        }
    }
}
