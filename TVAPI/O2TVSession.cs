using System;
using System.Collections.Generic;

namespace TVAPI
{
    public class O2TVSession
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public string RemoteAccessToken { get; set; }
        public string AccessToken { get; set; }
        public string ServiceId { get; set; }
        public string ServiceDescription { get; set; }

        public string Subscription { get; set; }
        public string Isp { get; set; }
        public string Locality { get; set; }
        public string Offers { get; set; }
        public string Tariff { get; set; }

        public string SData { get; set; }
        public string EncodedChannels { get; set; }

        public List<string> LiveChannels { get; set; } = new List<string>();
    }
}
