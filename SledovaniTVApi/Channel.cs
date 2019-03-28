using System;
namespace SledovaniTVAPI
{
    public class Channel : JSONObject
    {
        public string id { get; set; }

        public string name { get; set; }
        public string type { get; set; }
        public string url { get; set; }

        public string timedataUrl { get; set; }
        public string streamType { get; set; }
        public string ulogoUrlrl { get; set; }
        public string locked { get; set; }
        public string parentLocked { get; set; }
        public string lockedMessage { get; set; }
        public string utimeshiftBeforeEventrl { get; set; }
        public string timeshiftAfterEvent { get; set; }
        public string drm { get; set; }
        public string availability { get; set; }
        public string group { get; set; }
        public string audio { get; set; }
        public string dvbMrl { get; set; }
        public string multicastSource { get; set; }
        public string whiteLogoUrl { get; set; }
    }
}
