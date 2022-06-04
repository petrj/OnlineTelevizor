using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor.Models
{
    public interface IOnlineTelevizorConfiguration
    {
        TVAPIEnum TVApi { get; set; }

        // credentials
        string Username { get; set; }
        string Password { get; set; }
        string ChildLockPIN { get; set; }

        string KUKIsn { get; set; }

        string O2TVUsername { get; set; }
        string O2TVPassword { get; set; }

        string DVBStreamerUrl { get; set; }

        //  user settings
        string StreamQuality { get; set; }
        string ChannelFilterGroup { get; set; }
        string ChannelFilterType { get; set; }
        string ChannelFilterName { get; set; }

        List<string> FavouriteChannelNames { get; set; }
        bool ShowOnlyFavouriteChannels { get; set; }

        bool ShowLocked { get; set; }
        bool ShowAdultChannels { get; set; }

        bool InternalPlayer { get; set; }
        bool InternalPlayerSwitchEnabled { get; }

        bool Fullscreen { get; set; }
        bool FullscreenSwitchEnabled { get; }

        bool PlayOnBackground { get; set; }
        bool PlayOnBackgroundSwitchEnabled { get; }

        bool IsRunningOnTV { get; set; }

        /// <summary>
        /// -1    : no chanel
        /// 0     : last channel
        /// 1...n : channel number
        /// </summary>
        string AutoPlayChannelNumber { get; set; }
        string LastChannelNumber { get; set; }

        AppFontSizeEnum AppFontSize { get; set; }

        bool Purchased { get; set; }

        bool PurchaseTokenSent { get; set; }

        bool NotPurchased { get; }

        string PurchaseProductId { get; }

        bool DebugMode { get; set; }

        // private cached login credentailes
        string DeviceId { get; set; }
        string DevicePassword { get; set; }

        string OutputDirectory { get; }

        long UsableSpace { get; }
    }
}
