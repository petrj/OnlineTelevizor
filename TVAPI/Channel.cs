using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace TVAPI
{
    public class Channel : JSONObject
    {
        public string ChannelNumber { get; set; }

        public string Name { get; set; }
        public string Url { get; set; }
        public string Id { get; set; }
        public string EPGId { get; set; }
        public string Type { get; set; }
        public string LogoUrl { get; set; }
        public string Locked { get; set; }
        public string Group { get; set; }

        public List<SubTitleTrack> SubTitles { get; set; } = new List<SubTitleTrack>();
    }
}
