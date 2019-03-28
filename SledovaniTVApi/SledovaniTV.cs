using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SledovaniTVAPI
{
    public class SledovaniTV
    {
        private ILoggingService _log;
        private const string ServiceUrl = "http://sledovanitv.cz/api/";

        private Credentials _credentials;
        private DeviceConnection _deviceConnection;
        private Session _session;

        public Channels Channels { get; set; }

        public SledovaniTV(Credentials credentials, ILoggingService loggingService)
        {
            _log = loggingService;
            _credentials = credentials;
        }

        public DeviceConnection Connection
        {
            get
            {
                return _deviceConnection;
            }
            set
            {
                _deviceConnection = value;
            }
        }

        private async Task<T> SendJSONRequest<T>(string functionName, Dictionary<string, string> pars)
        {
            var result = await SendRequest(functionName, pars);
            return JsonConvert.DeserializeObject<T>(result);
        }

        private async Task<string> SendRequest(string functionName, Dictionary<string, string> parameters)
        {
            _log.Debug($"Calling function {functionName}");

            var url = ServiceUrl + functionName;

            var first = true;
            foreach (var kvp in parameters)
            {
                if (first)
                {
                    first = false;
                    url += "?";
                }
                else
                {
                    url += "&";
                }
                url += $"{kvp.Key}={kvp.Value}";
            }

            var result = await SendRequest(url);

            return result;
        }

        private async Task<string> SendRequest(string url, string method = "GET")
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);

                var contentType = "application/x-www-form-urlencoded";

                request.Method = method;
                request.ContentType = contentType;
                request.Accept = "application/json";
                request.Timeout = 100 * 60 * 1000; // 100 min timeout per one request

                _log.Debug($"Sending {method} request to url: {request.RequestUri}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"Method: {request.Method}");
                _log.Debug($"RequestUri: {request.RequestUri}");
                _log.Debug($"Timeout: {request.Timeout}");
                _log.Debug($"ContentType: {request.ContentType}");
                _log.Debug($"ContentLength: {request.ContentLength}");

                foreach (var header in request.Headers)
                {
                    _log.Debug($"Header: {header.ToString()}");
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

        /// <summary>
        /// Pairing user with service
        /// </summary>
        private async Task Pair()
        {
            if (DevicePaired)
            {
                _log.Info($"User is already paired");
                return;  // already paired
            }

            try
            {
                _log.Info($"Pairing user credentials");

                var ps = new Dictionary<string, string>()
                {
                    { "username", _credentials.Username },
                    { "password", _credentials.Password },
                    { "type", "samsungtv" }
                };

                _deviceConnection = await SendJSONRequest<DeviceConnection>("create-pairing", ps);

                _log.Info("Received User Connection:");
                _log.Info(_deviceConnection.ToString());

            } catch (Exception ex)
            {
                _log.Error(ex, "Error while pairing user with SledovaniTV service");
                throw;
            }
        }

        private Dictionary<string, string> ParamsForLogin
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { "deviceId", _deviceConnection.deviceId },
                    { "password", _deviceConnection.password },
                    { "version", "3.2.004" },
                    { "lang", "cs" },
                    { "unit", "default" }
                };
            }
        }

        public async Task Login()
        {
            try
            {

                if (_session != null && !String.IsNullOrEmpty(_session.PHPSESSID))
                    return; // PHPSESSID already exists

                _log.Info($"Login user");

                await Pair();

                _session = await SendJSONRequest<Session>("device-login", ParamsForLogin);

                if (_session.status == "0" && _session.error == "bad login")
                {
                    _log.Info($"Bad login, pairing and logging again");

                    // pairing again
                    _deviceConnection.deviceId = null;
                    _deviceConnection.password = null;
                    await Pair();

                    _session = await SendJSONRequest<Session>("device-login", ParamsForLogin);

                    if (_session.status == "0" && _session.error == "bad login")
                    {
                        throw new Exception("Login failed");
                    }
                }

                _log.Info($"Received PHPSESSID: {_session.PHPSESSID}");
            }
             catch (Exception ex)
            {
                _log.Error(ex, "Error while login to SledovaniTV service");
                throw;
            }
        }

        public bool DevicePaired
        {
            get
            {
                return (_deviceConnection != null && !_deviceConnection.IsEmpty);
            }
        }

        public async Task ReloadChanels()
        {
            await Login();

            try
            {
                var ps = new Dictionary<string, string>()
                {
                    { "format", "androidtv" },
                    { "PHPSESSID", _session.PHPSESSID }
                };

                Channels = await SendJSONRequest<Channels>("playlist", ps);

                // running in Liveplayer?
                // JSON deserializing does not work!
                // https://docs.microsoft.com/cs-cz/xamarin/tools/live-player/limitations
                // https://bugzilla.xamarin.com/show_bug.cgi?id=58912

                var channelsString = await SendRequest("playlist", ps);
                var matches = Regex.Matches(channelsString, "\"name\":\"(.*?\"),\"type\":\"(.*?\"),\"url\":\"(.*?\")");

                var index = 0;
                foreach (Match m in matches)
                {
                    var value = m.Value;

                    var nameMatch = Regex.Match(value,"\"name\":\"(.*?\")");
                    var urlMatch = Regex.Match(value,"\"url\":\"(.*?\")");

                    Channels.channels[index].name = nameMatch.Value.Substring(8, nameMatch.Value.Length-9);
                    Channels.channels[index].url = urlMatch.Value.Substring(7, urlMatch.Value.Length - 8);

                    index++;
                }

                _log.Info($"Received {Channels.channels.Count} channels");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while refreshing channels");
            }
        }

        public async Task Unlock()
        {
            await Login();

            var ps = new Dictionary<string, string>()
            {
                { "pin", _credentials.ChildLockPIN },
                { "whitelogo", "1" },
                { "PHPSESSID", _session.PHPSESSID }
            };

            Channels = await SendJSONRequest<Channels>("pin-unlock", ps);

            _log.Info($"Received {Channels.channels.Count} channels");
        }
    }
}
