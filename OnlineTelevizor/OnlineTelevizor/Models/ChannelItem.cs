using SledovaniTVAPI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TVAPI;

namespace OnlineTelevizor.Models
{
    public class ChannelItem : BaseNotifableObject
    {
        public List<SubTitleTrack> Subtitles { get; set; } = new List<SubTitleTrack>();
        private List<EPGItem> _EPGItems { get; set; } = new List<EPGItem>();

        public Dictionary<int,string> AudioTracks { get; set; } = new Dictionary<int, string>();
        public string VideoTrackDescription { get; set; } = String.Empty;

        public static ChannelItem CreateFromChannel(Channel channel)
        {
            var ch = new ChannelItem
            {
                ChannelNumber = channel.ChannelNumber,
                Name = channel.Name,
                Url = channel.Url,
                Id = channel.Id,
                LogoUrl = channel.LogoUrl,
                Type = channel.Type,
                Group = channel.Group
            };

            foreach (var track in channel.SubTitles)
            {
                ch.Subtitles.Add(new SubTitleTrack
                {
                    Title = track.Title,
                    Url = track.Url
                });
            }

            return ch;
        }

        public string ChannelNumber { get; set; }

        public string Name { get; set; }
        public string Url { get; set; }
        public string Id { get; set; }
        public string LogoUrl { get; set; }

        public string Type { get; set; }
        public string Group { get; set; }

        public bool IsCasting { get; set; } = false;
        public bool IsRecording { get; set; } = false;
        public bool IsFav { get; set; } = false;

        public string FavStateIcon
        {
            get
            {
                if (IsFav)
                return "Fav.png";

                return null;
            }
        }

        public string RecordStateIcon
        {
            get
            {
                if (IsRecording)
                    return "Rec.png";

                return null;
            }
        }

        public string CastingStateIcon
        {
            get
            {
                if (IsCasting)
                    return "Cast.png";

                return null;
            }
        }

        public void NotifyStateChange()
        {
            OnPropertyChanged(nameof(RecordStateIcon));
            OnPropertyChanged(nameof(CastingStateIcon));
            OnPropertyChanged(nameof(FavStateIcon));
        }

        public void NotifyEPGChange()
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
        }

        public void ClearEPG()
        {
            _EPGItems.Clear();
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

        public EPGItem NextEPGItem
        {
            get
            {
                if (_EPGItems.Count <= 1)
                    return null;

                return _EPGItems[1];
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
