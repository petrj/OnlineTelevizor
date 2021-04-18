using SledovaniTVAPI;
using System;
using System.Collections.Generic;
using System.Text;
using TVAPI;

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

        private void NotifyEPGChange()
        {
            OnPropertyChanged(nameof(CurrentEPGTitle));
            OnPropertyChanged(nameof(EPGTime));
            OnPropertyChanged(nameof(EPGProgress));
            OnPropertyChanged(nameof(NextTitle));
            OnPropertyChanged(nameof(EPGTimeStart));
            OnPropertyChanged(nameof(EPGTimeFinish));
        }

        public void AddEPGItem(EPGItem epgItem)
        {
            _EPGItems.Add(epgItem);

            NotifyEPGChange();
        }

        public void ClearEPG()
        {
            _EPGItems.Clear();

            NotifyEPGChange();
        }

        public string EPGTime
        {
            get
            {
                var epg = CurrentEPGItem;

                return (epg == null)
                    ? null
                    : epg.Start.ToString("HH:mm")
                        + " - " +
                      epg.Finish.ToString("HH:mm");
            }
        }

        public double EPGProgress
        {
            get
            {
                var epg = CurrentEPGItem;

                return (epg == null)
                    ? 0
                    : epg.Progress;
            }
        }

        public string NextTitle
        {
            get
            {
                if (_EPGItems.Count <= 1)
                    return null;

                var title = _EPGItems[1].Title;
                if (title != null)
                {
                    title = title.Trim();
                    return $"-> {title}";
                }

                return null;
            }
        }


        public string EPGTimeStart
        {
            get
            {
                var epg = CurrentEPGItem;

                return (epg == null)
                    ? null
                    : epg.Start.ToString("HH:mm");
            }
        }

        public string EPGTimeFinish
        {
            get
            {
                var epg = CurrentEPGItem;

                return (epg == null)
                    ? null                  
                    : epg.Finish.ToString("HH:mm");
            }
        }

        public String CurrentEPGTitle
        {
            get { return _EPGItems.Count == 0 ? null : _EPGItems[0].Title; }
        }

        public EPGItem CurrentEPGItem
        {
            get { return _EPGItems.Count == 0 ? null : _EPGItems[0]; }
        }
     
    }
}
