using System;
using System.Collections.Generic;
using System.Text;
using TVAPI;

namespace OnlineTelevizor.Models
{
    public class MediaDetail
    {
        public string MediaUrl { get; set;  }
        public string Title { get; set; }
        public string Type { get; set; }
        public string ChanneldID { get; set; }
        public EPGItem CurrentEPGItem { get; set; }        
        public string LogoUrl { get; set; }
    }
}
