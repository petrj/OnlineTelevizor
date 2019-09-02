using LoggerService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TVAPI;

namespace KUKITVAPI
{
    public class KUKITV : ITVAPI
    {
        private ILoggingService _log;
        private StatusEnum _status = StatusEnum.NotInitialized;
        private DeviceConnection _connection = new DeviceConnection();        

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

        public async Task<List<Channel>> GetChanels()
        {
            var ch = new Channel()
            {
                ChannelNumber = "1",
                Name = "CT"
            };

            return new List<Channel>() { ch };
        }

        public async Task<List<EPGItem>> GetEPG()
        {
            return new List<EPGItem>() {};
        }

        public async Task<List<Quality>> GetStreamQualities()
        {
            return new List<Quality>() { };
        }

        public async Task Login(bool force = false)
        {
            _log.Debug($"Logging to KUKI");

            var sn = new Dictionary<string, string>();
            sn.Add("sn", _connection.deviceId);

            // authorize:

            var authResponse = SendRequest("https://as.kuki.cz/api/register", "POST", sn);

            //var _sessionKey = 

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

        private string SendRequest(string url, string method = "GET", Dictionary<string, string> postData = null, Dictionary<string, string> headers = null)
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

                using (var response = request.GetResponse() as HttpWebResponse)
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
