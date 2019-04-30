using SledovaniTVAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor.Models
{
    public class ChannelItem : BaseNotifableObject
    {
        private List<EPGItem> _EPGItems { get; set; } = new List<EPGItem>();

        public static ChannelItem CreateFromChannel(Channel channel)
        {
            return new ChannelItem
            {
                ChannelNumber = channel.ChannelNumber,
                Name = channel.Name,
                Url = channel.Url,
                Id = channel.Id,
                LogoUrl = channel.LogoUrl,
                Type = channel.Type,
                Group = channel.Group
            };
        }

        public string ChannelNumber { get; set; }

        public string Name { get; set; }
        public string Url { get; set; }
        public string Id { get; set; }
        public string LogoUrl { get; set; }

        public string Type { get; set; }
        public string Group { get; set; }

        public void AddEPGItem(EPGItem epgItem)
        {
            _EPGItems.Add(epgItem);
            OnPropertyChanged(nameof(CurrentEPGTitle));
        }

        public void ClearEPG()
        {
            _EPGItems.Clear();
            OnPropertyChanged(nameof(CurrentEPGTitle));
        }

        public String CurrentEPGTitle
        {
            get
            {
                if (_EPGItems.Count == 0)
                    return null;

                return _EPGItems[0].Title;
            }
        }
    }
}
