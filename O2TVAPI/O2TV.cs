using LoggerService;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TVAPI;

namespace O2TVAPI
{
    public class O2TV :  ITVAPI
    {
        private ILoggingService _log;
        private StatusEnum _status = StatusEnum.NotInitialized;
        private DeviceConnection _connection = new DeviceConnection();
        private string _remoteAccessToken = null;
        private string _serviceId = null;
        public string DeviceName { get; set; } = "123456";

        public O2TV(ILoggingService loggingService)
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


        public StatusEnum Status
        {
            get
            {
                return _status;
            }
        }

        public async Task<List<Channel>> GetChanels()
        {
            /*
            var headerPostData = new Dictionary<string, string>();

            // header = { 
            // 'User-Agent' : 'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0', 
            // 'Content-Type' : 'application/json'}

            if service != None:
                header.update({
                'x-o2tv-access-token' : str(service['access_token']), 'x-o2tv-sdata' : str(service['sdata']), 'x-o2tv-device-id' : addon.get

            headerPostData.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
            headerPostData.Add("x-o2tv-access-token", "gzip");
            // headerPostData.Add("Connection", "Keep-Alive");
            //headerPostData.Add("Content-Type", "application/x-www-form-urlencoded;charset=UTF-8");
            headerPostData.Add("X-NanguTv-Device-Id", DeviceName);
            headerPostData.Add("X-NanguTv-Device-Name", "tvbox");

            var getChannelsResponse = await SendRequest("https://api.o2tv.cz/unity/api/v1/channels/", "POST", null, headerPostData);
            var authResponseJson = JObject.Parse(authResponse);

            */
            return new List<Channel>();
        }

        public async Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG()
        {
            return new Dictionary<string, List<EPGItem>>();
        }

        public async Task<List<EPGItem>> GetEPG()
        {
            return new List<EPGItem>();
        }

        public string GetEPGEventUrl(EPGItem item)
        {
            return null;
        }

        public async Task<string> GetEPGItemDescription(EPGItem epgItem)
        {
            return null;
        }

        public async Task<List<Quality>> GetStreamQualities()
        {
            return new List<Quality>();
        }

        public async Task Lock()
        {

        }

        private Dictionary<string, string> GetHeaderData()
        {
            // header = { 'X-NanguTv-App-Version' : 'Android#6.4.1', 
            //    'User-Agent' : 'Dalvik/2.1.0',
            //    'Accept-Encoding' : 'gzip',
            //    'Connection' : 'Keep-Alive',
            //    'Content-Type' : 'application/x-www-form-urlencoded;charset=UTF-8',
            //    'X-NanguTv-Device-Id' : addon.getSetting('deviceid'),
            //    'X-NanguTv-Device-Name' : addon.getSetting('devicename')}

            // update: header.update({ 'X-NanguTv-Access-Token' : str(service['access_token']), 'X-NanguTv-Device-Id' : addon.getSetting('deviceid')})

            var header = new Dictionary<string, string>();
            header.Add("X-NanguTv-Device-Name", "tvbox");
            header.Add("X-NanguTv-Device-Id", DeviceName);
            header.Add("X-NanguTv-App-Version", "Android#6.4.1");
            header.Add("Accept-Encoding", "gzip");

            return header;
        }

        public async Task Login(bool force = false)
        {
            _log.Debug($"Logging to O2TV");

            if (String.IsNullOrEmpty(_connection.deviceId) || String.IsNullOrEmpty(_connection.password))
            {
                _status = StatusEnum.EmptyCredentials;
                return;
            }

            if (force)
                _status = StatusEnum.NotInitialized;


            if (!force && Status == StatusEnum.Logged)
            {
                _log.Debug("Device is already logged");
                return;
            }

            try
            {
                // authorize:

                var postData = new Dictionary<string, string>();
                postData.Add("username", _connection.deviceId);
                postData.Add("password", _connection.password);

                _status = StatusEnum.NotInitialized;                

                var authResponse = await SendRequest("https://ottmediator.o2tv.cz/ottmediator-war/login", "POST", postData, GetHeaderData());
            
                var authResponseJson = JObject.Parse(authResponse);

                //{
                //  "contact_person_first_name": "",
                //  "services": [{
                //                        "service_id": "7b22757365724964223a224f54542d4e424e4d434a454f322d3433373237312271",
                //    "description": "Registrován emailem email@email.com",
                //    "tvod_purchase_available": false
                //  }],
                //  "remote_access_token": "70a51946f83f688db6deb02912b162ca2a017542a8c1f39ba59201a225edc928bd6621fd21b86a6a7908ec51265a49b9",
                //  "contact_person_last_name": "email@email.com"
                //}

                if (authResponseJson.HasValue("remote_access_token"))
                {
                    _remoteAccessToken = authResponseJson.GetStringValue("remote_access_token");                    
                    _log.Debug($"Setting remote access token: {_remoteAccessToken}");
                }

                if (authResponseJson.HasValue("services"))
                {
                    foreach (JObject service in authResponseJson.GetValue("services"))
                    {
                        _serviceId = service.GetStringValue("service_id");
                        _log.Debug($"Setting service id: {_serviceId}");
                        break;
                    }                    
                }

                var loginChoicePostData = new Dictionary<string, string>();
                loginChoicePostData.Add("service_id", _serviceId);
                loginChoicePostData.Add("remote_access_token", _remoteAccessToken);

                var loginChoiceResponse = await SendRequest("https://ottmediator.o2tv.cz:4443/ottmediator-war/loginChoiceService", "POST", loginChoicePostData, GetHeaderData());
                
                if (String.IsNullOrEmpty(loginChoiceResponse))
                {
                    _log.Debug($"Empty response");
                }
                
                // _status = StatusEnum.Logged;
                _status = StatusEnum.LoginFailed;
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

        public void ResetConnection()
        {

        }

        public void SetConnection(string deviceId, string password)
        {
            _connection.deviceId = deviceId;
            _connection.password = password;
        }

        public void SetCredentials(string username, string password, string childLockPIN = null)
        {

        }

        public async Task Stop()
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

        private async Task<string> SendRequest(string url, string method = "POST", Dictionary<string, string> postData = null, Dictionary<string, string> headers = null)
        {
            try
            {
                _log.Debug($"Sending request to {url}{Environment.NewLine}---------->");

                var request = (HttpWebRequest)WebRequest.Create(url);                

                request.Method = method;
                request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";  //"application /x-www-form-urlencoded";  //"application/x-www-form-urlencoded;charset=UTF-8";
                request.Accept = "application/json";
                request.UserAgent = "Dalvik/2.1.0";  // "okhttp/3.10.0";                             
                request.KeepAlive = true;
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

                    _log.Debug($"PostData: {postDataAsString}");

                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(Encoding.UTF8.GetBytes(postDataAsString), 0, postDataAsString.Length);
                    }
                }

                if (request.Headers.Count > 0)
                {
                    for (var i=0;i< request.Headers.Count; i++)
                    {
                        _log.Debug($"Header: {request.Headers.Keys[i]}={request.Headers.GetValues(i).FirstOrDefault()}");
                    }
                }                

                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    string responseString;
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        responseString = sr.ReadToEnd();
                    }

                    _log.Debug($"{Environment.NewLine}--------------------->{Environment.NewLine}");

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
