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
    public class O2TV : ITVAPI
    {
        /*
            source:
            https://github.com/waladir/plugin.video.archivo2tv/
        */

        private ILoggingService _log;
        private StatusEnum _status = StatusEnum.NotInitialized;

        private O2TVSession _session = new O2TVSession();

        public string DeviceName { get; set; } = "123456";

        public O2TV(ILoggingService loggingService)
        {
            _log = loggingService;
        }

        public DeviceConnection Connection
        {
            get
            {
                return new DeviceConnection()
                {
                    deviceId = _session.UserName,
                    password = _session.Password
                };
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

        private async Task<string> GetChannelUrl(string channelKey)
        {
            if (_status != StatusEnum.Logged)
                return null;

            try
            {
                var headerPostData = GetUnityHeaderData();

                //channelKey = channelKey.Replace("'", "''");
                channelKey = System.Web.HttpUtility.UrlEncode(channelKey);

                var getChannelResponse = await SendRequest($"https://api.o2tv.cz/unity/api/v1/channels/playlist/?channelKey={channelKey}", "GET", null, headerPostData);
                var getChannelResponseJson = JObject.Parse(getChannelResponse);

                /*

                  Response:
                    {
                        "setup":
                            {
                                "title":"O2TV Sport2 HD",
                                "description":"Stanice právě nevysílá"
                            },
                        "playlist":
                            [
                                {
                                    "type":"LIVE",
                                    "id":"29871762",
                                    "assetId":"29871762",
                                    "streamUrls":
                                        {
                                            "main":"https://stc.o2tv.cz/calc/at/0fae5cee2c0de55dbedca3571a660300/1539704743794/subscr/subscription/deviceId/562-tv-pc.mpd",
                                            "timeshift":"https://stc.o2tv.cz/at/33c07990479ca192e3022debbede292b/1621803129172/subscr/OTT-NONMOJEO2-437271/123456/562-tv-pc-20210523T175959.mpd"
                                        },
                                    "subtitles":[],
                                    "o2_from_timestamp":1621792800000,
                                    "o2_to_timestamp":1621807200000,
                                    "epgStartOverlap":1000,
                                    "epgEndOverlap":900000,
                                    "playStartover":false
                                }
                            ]
                    }
                */

                if (!getChannelResponseJson.HasValue("playlist"))
                {
                    return null;
                }

                var playList = getChannelResponseJson["playlist"];

                foreach (JObject playListItem in playList)
                {
                    if (!playListItem.HasValue("streamUrls"))
                    {
                        continue;
                    }

                    var streamUrls = playListItem["streamUrls"] as JObject;

                    if (!streamUrls.HasValue("main"))
                    {
                        continue;
                    }

                    var url = streamUrls.GetStringValue("main");

                    return url;
                }

                return null;
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

            return null;
        }


        public async Task<List<Channel>> GetChanels()
        {
            _log.Debug("Getting channels");

            var res = new List<Channel>();

            await Login();

            if (_status != StatusEnum.Logged)
                return res;

            try
            {
                // logo images:

                var channelImages = new Dictionary<string, string>();

                var headerPostData = GetUnityHeaderData();

                var getChannelsResponse = await SendRequest("https://api.o2tv.cz/unity/api/v1/channels", "GET", null, headerPostData);
                var getChannelsResponseJson = JObject.Parse(getChannelsResponse);

                // https://www.o2tv.cz  +  /assets/images/tv-logos/negative/ct1-hd.png

                if (getChannelsResponseJson.HasValue("result"))
                {
                    foreach (JObject result in getChannelsResponseJson["result"])
                    {
                        if (!result.HasValue("channel"))
                            continue;

                        var ch = result["channel"] as JObject;

                        if (!ch.HasValue("images") || !ch.HasValue("channelKey"))
                            continue;

                        var channleKey = ch.GetStringValue("channelKey");

                        var images = ch["images"] as JObject;

                        if (!images.HasValue("color"))
                            continue;

                        var colorUrl = images["color"] as JObject;

                        if (!colorUrl.HasValue("url"))
                            continue;

                        var url = colorUrl.GetStringValue("url");

                        channelImages.Add(channleKey, $"https://www.o2tv.cz{url}");
                    }
                }

                /*
                   {
	                    "result": [
		                    {
			                    "channel": {
				                    "channelKey": "ČT1 HD",
				                    "name": "ČT1 HD",
				                    "weight": 1,
				                    "npvr": true,
				                    "o2tv": true,
				                    "defaultGroup": false,
				                    "live": false,
				                    "npvrForStartedProgram": true,
				                    "npvrForEndedProgram": true,
				                    "storedMediaDuration": 10100,
				                    "epgStartOverlap": 1000,
				                    "epgEndOverlap": 900000,
				                    "images": {
					                    "color": {
						                    "url": "/assets/images/tv-logos/negative/ct1-hd.png"
					                    },
					                    "color_115": {
						                    "url": "/assets/images/tv-logos/negative_115/ct1-hd.png"
					                    },
					                    "blackWhite": {
						                    "url": "/assets/images/tv-logos/win/ct1-hd.png"
					                    }
				                    },
				                    "startOverTvEnabled": true,
				                    "keyForCache": "001"
			                    },
			                    "live": {
				                    "epgId": 29889305,
				                    "start": 1622031300000,
				                    "end": 1622037000000,
				                    "npvr": true,
				                    "timeShift": false,
				                    "name": "Po stopách krve, V HLAVNÍ ROLI ZLOČIN… Petr Schulhoff – 35 let od úmrtí",
				                    "availableTo": 1622636100000
			                    }
		                    },
                    ...
                 */

                // channels stream urls:

                var number = 1;
                foreach (var ch in _session.LiveChannels)
                {
                    var channel = new Channel()
                    {
                        Name = ch,
                        Id = ch,
                        ChannelNumber = number.ToString(),
                        Type = "O2TV",
                        Group = "O2TV"
                    };

                    channel.Url = await GetChannelUrl(ch);

                    if (channelImages.ContainsKey(ch))
                    {
                        channel.LogoUrl = channelImages[ch];
                    }

                    res.Add(channel);
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

        public async Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG()
        {
            _log.Debug("Getting channels EPG");

            var res = new Dictionary<string, List<EPGItem>>();

            await Login();

            if (_status != StatusEnum.Logged)
                return res;

            try
            {
                
                //var toUT = fromUT + 4 * 60 * 60;
                var channelKey = "ČT sport HD";
                var channelKeyUrlEncoded = System.Web.HttpUtility.UrlEncode(channelKey);

                res.Add(channelKey, new List<EPGItem>());

                var headerPostData = GetUnityHeaderData();
                var fromUT = Convert.ToInt64(DateTime.Now.AddHours(-4).Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds); 
                var response = await SendRequest($"https://api.o2tv.cz/unity/api/v1/epg/depr/?channelKey={channelKeyUrlEncoded}&from={fromUT}", "GET", null, headerPostData);
                var responseJson = JObject.Parse(response);

                if (!responseJson.HasValue("epg"))
                    throw new Exception($"Invalid response: {response}");

               var epgJson = responseJson.GetValue("epg") as JObject;

                if (!epgJson.HasValue("items"))
                    throw new Exception($"Invalid response: {response}");

                foreach (JObject epgItem in epgJson.GetValue("items"))
                {
                    //var epgItems = epgJson.GetValue("items") as JObject;

                    if (!epgItem.HasValue("channel"))
                        throw new Exception($"Invalid response: {response}");

                    //var channel = epgItem.GetValue("channel") as JObject;

                    //if (!channel.HasValue("channelKey"))
                        //throw new Exception($"Invalid response: {response}");                    

                    if (!epgItem.HasValue("programs"))
                        throw new Exception($"Invalid response: {response}");

                    //var epgPrograms = epgItems.GetValue("programs") as JObject;

                    foreach (JObject program in epgItem.GetValue("programs"))
                    {
                        if (
                            !program.HasValue("epgId") ||
                            !program.HasValue("start") ||
                            !program.HasValue("end") ||
                            !program.HasValue("name")
                            )
                         throw new Exception($"Invalid response: {response}");

                        var startString = program.GetStringValue("start");
                        var startInt = Int64.Parse(startString);

                        var finishString = program.GetStringValue("end");
                        var finishInt = Int64.Parse(finishString);

                        startInt = startInt / 1000; // ms to s
                        finishInt = finishInt / 1000; // ms to s

                        var startDate = new DateTime(1970, 1, 1).AddHours(+2).AddSeconds(startInt);  // UTC+2?
                        var finishDate = new DateTime(1970, 1, 1).AddHours(+2).AddSeconds(finishInt);  // UTC+2?

                        var item = new EPGItem()
                        {
                            ChannelId = channelKey,
                            Title = program.GetStringValue("name"),
                            Start = startDate,
                            Finish = finishDate,
                            EPGId = program.GetStringValue("epgId")
                        };

                        res[channelKey].Add(item);
                    }

                }

                /*
                 Response:

                {
                    "epg": {
                        "totalCount": 1,
                        "offset": 0,
                        "items": [
                            {
                                "channel": {
                                    "channelKey": "ČT sport HD",
                                    "name": "ČT sport HD",
                                    "logoUrl": "/assets/images/tv-logos/original/ct-sport-hd.png",
                                    "weight": 76,
                                    "npvr": true,
                                    "o2tv": true,
                                    "defaultGroup": false,
                                    "live": false,
                                    "npvrForStartedProgram": true,
                                    "npvrForEndedProgram": true,
                                    "storedMediaDuration": 10100,
                                    "epgStartOverlap": 1000,
                                    "epgEndOverlap": 900000
                                },
                                "programs": [
                                    {
                                        "epgId": 29896663,
                                        "start": 1622130000000,
                                        "end": 1622133600000,
                                        "npvr": true,
                                        "timeShift": false,
                                        "name": "USA - Lotyšsko, Hokej",
                                        "availableTo": 1622734800000
                                    },
                                    {
                                        "epgId": 29896664,
                                        "start": 1622133600000,
                                        "end": 1622144400000,
                                        "npvr": true,
                                        "timeShift": false,
                                        "name": "Švédsko - Česko, Hokej",
                                        "availableTo": 1622738400000
                                    },
                            .......
                 */
            }
            catch (WebException wex)
            {
                _log.Error(wex, "Error while getting channels epg");
                //_status = StatusEnum.ConnectionNotAvailable;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while getting channels epg");
                //_status = StatusEnum.GeneralError;
            }

            return res;
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

        private Dictionary<string, string> GetUnityHeaderData()
        {
            // header = {
            // 'User-Agent' : 'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0',
            // 'Content-Type' : 'application/json'}

            // update({
            // 'x-o2tv-access-token' : str(service['access_token']),
            // 'x-o2tv-sdata' : str(service['sdata']),
            // 'x-o2tv-device-id' : addon.getSetting('deviceid'),
            // 'x-o2tv-device-name' : addon.getSetting('devicename')})

            var header = new Dictionary<string, string>();

            header.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
            header.Add("Content-Type", "application/json");

            header.Add("x-o2tv-access-token", _session.AccessToken);
            header.Add("x-o2tv-sdata", _session.SData);
            header.Add("x-o2tv-device-id", DeviceName);
            header.Add("x-o2tv-device-name", "tvbox");

            return header;
        }

        public async Task Login(bool force = false)
        {
            _log.Debug($"Logging to O2TV");

            if (String.IsNullOrEmpty(_session.UserName) || String.IsNullOrEmpty(_session.Password))
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
                postData.Add("username", _session.UserName);
                postData.Add("password", _session.Password);

                _status = StatusEnum.NotInitialized;

                var authResponse = await SendRequest("https://ottmediator.o2tv.cz/ottmediator-war/login", "POST", postData, GetHeaderData());

                var authResponseJson = JObject.Parse(authResponse);

                //{
                //  "contact_person_first_name": "",
                //  "services": [{
                //    "service_id": "value",
                //    "description": "Registrován emailem value",
                //    "tvod_purchase_available": false
                //  }],
                //  "remote_access_token": "value",
                //  "contact_person_last_name": "value"
                //}

                if (authResponseJson.HasValue("remote_access_token"))
                {
                    _session.RemoteAccessToken = authResponseJson.GetStringValue("remote_access_token");
                    _log.Debug($"Setting remote access token: {_session.RemoteAccessToken}");
                }

                if (!authResponseJson.HasValue("services"))
                {
                    throw new Exception("Invalid response");
                }

                foreach (JObject service in authResponseJson.GetValue("services"))
                {
                    _session.ServiceId = service.GetStringValue("service_id");
                    _session.ServiceDescription = service.GetStringValue("description");
                    _log.Debug($"Setting service id: {_session.ServiceId}");

                    var loginChoicePostData = new Dictionary<string, string>();
                    loginChoicePostData.Add("service_id", _session.ServiceId);
                    loginChoicePostData.Add("remote_access_token", _session.RemoteAccessToken);

                    var loginChoiceResponse = await SendRequest("https://ottmediator.o2tv.cz:4443/ottmediator-war/loginChoiceService", "POST", loginChoicePostData, GetHeaderData());

                    if (String.IsNullOrEmpty(loginChoiceResponse))
                    {
                        _log.Debug($"Empty response");
                    }

                    var tokenPostData = new Dictionary<string, string>();
                    tokenPostData.Add("grant_type", "remote_access_token");
                    tokenPostData.Add("client_id", "tef-web-portal-etnetera");
                    tokenPostData.Add("client_secret", "2b16ac9984cd60dd0154f779ef200679");
                    tokenPostData.Add("platform_id", "231a7d6678d00c65f6f3b2aaa699a0d0");
                    tokenPostData.Add("language", "cs");
                    tokenPostData.Add("remote_access_token", _session.RemoteAccessToken);
                    tokenPostData.Add("authority", "tef-sso");
                    tokenPostData.Add("isp_id", "1");

                    var tokenResponse = await SendRequest("https://oauth.o2tv.cz/oauth/token", "POST", tokenPostData, GetHeaderData());
                    var tokenResponseJson = JObject.Parse(tokenResponse);

                    // Response:
                    // {
                    //  "access_token":"value",
                    //  "refresh_token":"value",
                    //  "expires_in":315359999
                    // }

                    if (!tokenResponseJson.HasValue("access_token"))
                    {
                        throw new Exception("Invalid response");
                    }

                    _session.AccessToken = tokenResponseJson.GetStringValue("access_token");

                    _log.Debug($"Setting access token: {_session.AccessToken}");

                    var subscriptionHeader = GetHeaderData();
                    subscriptionHeader.Add("X-NanguTv-Access-Token", _session.AccessToken);

                    var subscriptionResponse = await SendRequest("https://app.o2tv.cz/sws/subscription/settings/subscription-configuration.json", "POST", null, subscriptionHeader);
                    var subscriptionResponseJson = JObject.Parse(subscriptionResponse);

                    /* Response:
                     {
                          "preferredAudioLanguage": null,
                          "allPreferredAudioLanguages": [],
                          "preferredSubtitlesLanguage": null,
                          "allPreferredSubtitleLanguages": [],
                          "preferredGuiLanguage": null,
                          "parentalPinHash": "value",
                          "parentalPinLength": 5,
                          "parentalListingAudience": "OVER_18",
                          "parentalPlaybackAudience": "OVER_18",
                          "isPurchasePinSet": true,
                          "purchasePinLength": 5,
                          "stbModelManufacturer": null,
                          "stbModelSeries": null,
                          "stbModelHasTuner": null,
                          "remoteControlAvailable": null,
                          "subscription": "value",
                          "subscriptionStbAccount": "value",
                          "subscriptionStbAccountId": value,
                          "locality": "OTT",
                          "isp": "1",
                          "address": {
                            "firstName": null,
                            "middleName": null,
                            "lastName": null,
                            "street": null,
                            "city": null,
                            "county": null,
                            "zipCode": null
                          },
                          "email": "value",
                          "publicIpAddress": "value",
                          "country": "CZ",
                          "deviceName": null,
                          "pairedDevicesLimit": 100,
                          "pairedDeviceLimits": [],
                          "bandwidth": null,
                          "messagingId": "value",
                          "deviceId": "123456",
                          "securemediaId": null,
                          "securemediaPassword": null,
                          "verimatrix3DeviceId": null,
                          "identityId": 1643064,
                          "username": null,
                          "subscriptionCreateTimestamp": value,
                          "pairedDevices": [
                            {
                              "deviceId": "value",
                              "deviceName": "value",
                              "lastLoginTimestamp": 1621602161495,
                              "lastLoginIpAddress": "value"
                            },
                            {
                              "deviceId": "value",
                              "deviceName": "value",
                              "lastLoginTimestamp": 1621720800000,
                              "lastLoginIpAddress": "value"
                            },
                            {
                              "deviceId": "value",
                              "deviceName": "value",
                              "lastLoginTimestamp": 1621548000000,
                              "lastLoginIpAddress": "value"
                            },
                            {
                              "deviceId": "123456",
                              "deviceName": "tvbox",
                              "lastLoginTimestamp": 1621720800000,
                              "lastLoginIpAddress": "value"
                            }
                          ],
                          "identities": [{
                            "identityId": value,
                            "name": "value",
                            "username": null,
                            "master": true,
                            "defaultStbAccountCode": null,
                            "pairingPin": null,
                            "pairingPinExpirationTimestamp": null,
                            "subscriptionStbAccounts": [{
                              "code": "value",
                              "deviceName": null,
                              "messagingId": "value",
                              "stbAssigned": false,
                              "remoteControlAvailable": false,
                              "subscriptionCode": "value"
                            }]
                          }],
                          "allSubscriptionStbAccounts": [],
                          "allSubscriptions": [{
                            "subscriptionCode": "value",
                            "allSubscriptionStbAccounts": [{
                              "code": "value",
                              "deviceName": null,
                              "messagingId": "value",
                              "stbAssigned": false,
                              "remoteControlAvailable": false,
                              "defaultStbAccount": false
                            }]
                          }],
                          "userPrefs": {},
                          "pvrEnabled": false,
                          "billingParams": {
                            "currency": "CZK",
                            "altCurrency": null,
                            "tvodOrdered": true,
                            "tariff": "ottRegisteredNonMojeO2Customer",
                            "offers": [
                              "hbbtv-svod",
                              "OTT-devices",
                              "PTVS"
                            ]
                          },
                          "subscriptionChannels": {},
                          "pvrPrograms": {},
                          "purchasedTimeshiftPrograms": {},
                          "timeshiftIntervals": []
                        }
                        */

                    if (!subscriptionResponseJson.HasValue("subscription") ||
                        !subscriptionResponseJson.HasValue("isp") ||
                        !subscriptionResponseJson.HasValue("locality") ||
                        !subscriptionResponseJson.HasValue("billingParams")
                        )
                    {
                        throw new Exception("Invalid response");
                    }

                    var billingParams = subscriptionResponseJson.GetValue("billingParams") as JObject;

                    if (!billingParams.HasValue("offers") ||
                        !billingParams.HasValue("tariff")
                        )
                    {
                        throw new Exception("Invalid response");
                    }

                    _session.Subscription = subscriptionResponseJson.GetStringValue("subscription");
                    _session.Isp = subscriptionResponseJson.GetStringValue("isp");
                    _session.Locality = subscriptionResponseJson.GetStringValue("locality");
                    _session.Offers = billingParams.GetStringValue("offers");
                    _session.Tariff = billingParams.GetStringValue("tariff");

                    _log.Debug($"Setting subsription: {_session.Subscription}");

                    //var profileHeader = GetUnityHeaderData();

                    var profileHeader = new Dictionary<string, string>();

                    profileHeader.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:75.0) Gecko/20100101 Firefox/75.0");
                    profileHeader.Add("Content-Type", "application/json");

                    profileHeader.Add("x-o2tv-access-token", _session.AccessToken);
                    //header.Add("x-o2tv-sdata", _session.SData);
                    profileHeader.Add("x-o2tv-device-id", DeviceName);
                    profileHeader.Add("x-o2tv-device-name", "tvbox");

                    var profileResponse = await SendRequest("https://api.o2tv.cz/unity/api/v1/user/profile/", "GET", null, profileHeader);
                    var profileResponseJSON = JObject.Parse(profileResponse);

                    /*  Reponse:

                    {
                    "code":"value",
                    "sdata":"value",
                    "tariff":"ottRegisteredNonMojeO2Customer",
                    "locality":"OTT",
                    "deviceId":"123456",

                    "subscription":
                        {
                            "hasService":true,
                            "offers":["hbbtv-svod","OTT-devices","PTVS"],
                            "isO2Identifier":false
                        },
                    "details":
                        {
                            "userName":"value",
                            "email":"value"
                        },
                    "ottChannels":
                        {
                            "live":["O2 Sport8 HD","O2 Sport2 HD","O2 Sport7 HD","O2 Sport","O2 Sport5 HD","O2 Sport HD","O2 Tenis HD","ČT sport HD","PremierSportHD","O2 Sport6 HD","O2 Sport1 HD","O2 Fotbal HD","O2 Sport4 HD","O2 Sport3 HD"],
                            "pvrRecording":["O2 Sport8 HD","O2 Sport2 HD","O2 Sport7 HD","O2 Sport","O2 Sport5 HD","O2 Sport HD","O2 Tenis HD","ČT sport HD","PremierSportHD","O2 Sport6 HD","O2 Sport1 HD","O2 Fotbal HD","O2 Sport4 HD","O2 Sport3 HD"],
                            "pvrPlaying":["O2 Sport8 HD","O2 Sport2 HD","O2 Sport7 HD","O2 Sport","O2 Sport5 HD","O2 Sport HD","O2 Tenis HD","ČT sport HD","PremierSportHD","O2 Sport6 HD","O2 Sport1 HD","O2 Fotbal HD","O2 Sport4 HD","O2 Sport3 HD"],
                            "free":["O2 Info"]
                        },
                    "encodedChannels":"00502602702802b02c0h70h80h91ha0hb0hc0hd0hf",
                    "prepaid":false,
                    "migrationInfo":
                        {
                            "status":
                                {
                                    "intValue":1,
                                    "value":
                                    "migrated"
                                 },
                        "source":
                                {
                                    "intValue":1,
                                    "value":
                                    "MOCK_SOURCE"
                                },
                        "phase":
                                {
                                    "intValue":0,
                                    "value":
                                    "IGNORE"
                                }
                        }
                    }   */

                    if (!profileResponseJSON.HasValue("sdata") ||
                        !profileResponseJSON.HasValue("encodedChannels") ||
                        !profileResponseJSON.HasValue("ottChannels")
                        )
                    {
                        throw new Exception("Invalid response");
                    }

                    _session.SData = profileResponseJSON.GetStringValue("sdata");
                    _session.EncodedChannels = profileResponseJSON.GetStringValue("encodedChannels");

                    _log.Debug($"Setting sdata: {_session.SData}");

                    var channels = profileResponseJSON.GetValue("ottChannels") as JObject;

                    if (!channels.HasValue("live"))
                    {
                        throw new Exception("Invalid response");
                    }

                    _session.LiveChannels.Clear();

                    foreach (string channel in channels.GetValue("live"))
                    {
                        _session.LiveChannels.Add(channel);
                        _log.Debug($"Adding channel: {channel}");
                    }

                    _status = StatusEnum.Logged;
                    return;
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
            _session.UserName = deviceId;
            _session.Password = password;
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
                        if (header.Key == "User-Agent")
                        {
                            request.UserAgent = header.Value;
                        }
                        else if (header.Key == "Content-Type")
                        {
                            request.ContentType = header.Value;
                        }
                        else
                        {
                            request.Headers.Add(header.Key, header.Value);
                        }
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
                    for (var i = 0; i < request.Headers.Count; i++)
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
