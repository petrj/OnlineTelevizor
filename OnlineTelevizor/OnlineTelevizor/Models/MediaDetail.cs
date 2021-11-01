using System;
using System.Collections.Generic;
using System.Text;
using TVAPI;

namespace OnlineTelevizor.Models
{
    public class MediaDetail
    {
        public MediaDetail()
        {

        }

        public MediaDetail(ChannelItem channel)
        {
            MediaUrl = channel.Url;
            Title = channel.Name;
            Type = channel.Type;
            ChanneldID = channel.Id;
            CurrentEPGItem = channel.CurrentEPGItem;
            NextEPGItem = channel.NextEPGItem;
            LogoUrl = channel.LogoUrl;
        }

        public string MediaUrl { get; set;  }
        public string Title { get; set; }
        public string Type { get; set; }
        public string ChanneldID { get; set; }
        public EPGItem CurrentEPGItem { get; set; }
        public EPGItem NextEPGItem { get; set; }
        public string LogoUrl { get; set; }
    }
}
