﻿using LoggerService;
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

namespace KUKITVAPI
{
    public class KUKITV : ITVAPI
    {
        /*
            source:
            https://www.xbmc-kodi.cz/prispevek-streamy-kuki-tv
        */

        private ILoggingService _log;
        private StatusEnum _status = StatusEnum.NotInitialized;
        private DeviceConnection _connection = new DeviceConnection();
        private string _session_key = null;

        private List<Channel> _cachedChannels = null;
        private List<EPGItem> _cachedEPG = null;
        private DateTime _cachedChannelsRefreshTime = DateTime.MinValue;
        private DateTime _cachedEPGRefreshTime = DateTime.MinValue;

        public KUKITV(ILoggingService loggingService)
        {
            _log = loggingService;
            _connection = new DeviceConnection();
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
                return true;
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
            _log.Info($"Logging to KUKI");

            if (String.IsNullOrEmpty(_connection.deviceId))
            {
                _status = StatusEnum.EmptyCredentials;
                return;
            }

            if (force)
                _status = StatusEnum.NotInitialized;


            if (!force && Status == StatusEnum.Logged)
            {
                _log.Info("Device is already logged");
                return;
            }

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

        public async Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG()
        {
            var epg = await GetEPG();

            var resNotSorted = new Dictionary<string, List<EPGItem>>();
            var res = new Dictionary<string, List<EPGItem>>();

            foreach (var epgItem in epg)
            {
                if (!resNotSorted.ContainsKey(epgItem.ChannelId))
                {
                    resNotSorted.Add(epgItem.ChannelId, new List<EPGItem>());
                }

                resNotSorted[epgItem.ChannelId].Add(epgItem);
            }

            // sorting each channel
            foreach (var chId in resNotSorted.Keys)
            {
                res.Add(chId, resNotSorted[chId].OrderBy(e => e.Start).ToList());
            }

            return res;
        }

        public async Task<List<Channel>> GetChannels(string quality = null)
        {
            if (((DateTime.Now-_cachedChannelsRefreshTime).TotalMinutes<60) &&
                _cachedChannels != null &&
                _cachedChannels.Count > 0)
            {
                return _cachedChannels;
            }

            var res = new List<Channel>();

            await Login();

            if (_status != StatusEnum.Logged)
                return res;

            try
            {
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
                        Id = chJson.GetStringValue("timeshift_ident"),
                        EPGId = chJson.GetStringValue("id"),
                        Type = chJson.GetStringValue("stream_type"),
                        Locked = "none",
                        Group = ""
                    };


                    ch.LogoUrl = $"https://www.kuki.cz/media/chlogo/{ch.Id}.png";

                    // white logo images
                    //var logo = chJson.GetStringValue("epg_logo");
                    //ch.LogoUrl = $"https://media.kuki.cz/imagefactory/channel_logo/channel-info-panel/fhd/{logo}";

                    var porn = chJson.GetStringValue("porn");
                    if (porn.ToLower() != "false")
                        ch.Locked = "pin";

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

            _cachedChannels = res;
            _cachedChannelsRefreshTime = DateTime.Now;

            return res;
        }

        public async Task<string> GetEPGItemDescription(EPGItem epgItem)
        {
            if (!string.IsNullOrEmpty(epgItem.Description))
                return epgItem.Description;

            epgItem.Description = await GetEPGProgramDetailDescription(epgItem.EPGId);

            return epgItem.Description;
        }

        private async Task<string> GetEPGProgramDetailDescription(string guidEPGProgramNumber)
        {
            try
            {
                var headerParams = new Dictionary<string, string>();
                headerParams.Add("X-SessionKey", _session_key);

                var epgProgramDetailResponse = await SendRequest($"https://as.kuki.cz/api-v2/epg-entity/{guidEPGProgramNumber}", "GET", null, headerParams);

                var epgProgramDetail = JObject.Parse(epgProgramDetailResponse);

                var desc = epgProgramDetail["description"].ToString();

                return desc;
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while getting epg program detail");
                _status = StatusEnum.ConnectionNotAvailable;

                return null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while getting epg program detail");
                _status = StatusEnum.GeneralError;

                return null;
            }
        }

        public async Task<List<EPGItem>> GetEPG()
        {
            if (((DateTime.Now - _cachedEPGRefreshTime).TotalMinutes < 60) &&
                _cachedEPG != null &&
                _cachedEPG.Count > 0)
            {
                return _cachedEPG;
            }

            var res = new List<EPGItem>();

            await Login();

            if (_status != StatusEnum.Logged)
                return res;

            try
            {
                var headerParams = new Dictionary<string, string>();
                headerParams.Add("X-SessionKey", _session_key);

                var channels = await GetChannels();
                var channelEPGIDs = new List<string>();

                // first channel is not loaded
                channelEPGIDs.Add($"channel:0");

                var chCount = 0;
                var totalChCount = 0;
                foreach (var ch in channels)
                {
                    channelEPGIDs.Add($"channel:{ch.EPGId}");

                    chCount++;
                    totalChCount++;

                    if (chCount>=10 ||
                        totalChCount == channels.Count)
                    {
                        chCount = 0;

                        var channelEPGIDsAsCommaSeparatedString = string.Join(",", channelEPGIDs);

                        var epgResponse = await SendRequest($"https://as.kuki.cz/api-v2/dashboard?rowGuidList=channel:{channelEPGIDsAsCommaSeparatedString}", "GET", null, headerParams);

                        foreach (Match rgm in Regex.Matches(epgResponse, "\"guid\":\"epg:"))
                        {
                            var pos = epgResponse.IndexOf("\"sourceLogo\"", rgm.Index);

                            var partJson = "{" + epgResponse.Substring(rgm.Index, pos - rgm.Index - 1) + "}";

                            var epg = JObject.Parse(partJson);

                            var ident = epg["ident"].ToString();
                            var title = epg["label"].ToString();
                            var times = $"{epg["startDate"]}{DateTime.Now.Year} {epg["start"]}";
                            var timef = $"{epg["endDate"]}{DateTime.Now.Year} {epg["end"]}";
                            var desc = String.Empty;

                            var guid = epg["guid"].ToString();

                            var item = new EPGItem()
                            {
                                ChannelId = ident,
                                Title = title,
                                Start = DateTime.ParseExact(times, "d.M.yyyy HH:mm", CultureInfo.InvariantCulture),
                                Finish = DateTime.ParseExact(timef, "d.M.yyyy HH:mm", CultureInfo.InvariantCulture),
                                EPGId = guid.Substring(4)
                            };

                            // excluding old programs
                            if (item.Finish < DateTime.Now)
                                continue;

                            res.Add(item);
                        }

                        channelEPGIDs.Clear();
                        channelEPGIDs.Add($"channel:0");
                    }
                }

            }
            catch (WebException wex)
            {

                _log.Error(wex, "Error while getting epg");
                _status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while getting epg");
                _status = StatusEnum.GeneralError;
            }

            _cachedEPG = res;
            _cachedEPGRefreshTime = DateTime.Now;

            return res;
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

        public async Task Stop()
        {
            // nothing to stop
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
