using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVPlayer.Models
{
    public interface ISledovaniTVConfiguration
    {
        // credentials
        string Username { get; set; }
        string Password { get; set; }
        string ChildLockPIN { get; set; }

        //  user settings
        string StreamQuality { get; set; }
        string ChannelGroup { get; set; }
        string ChannelType { get; set; }

        bool ShowLocked { get; set; }
        bool EnableLogging { get; set; }
        LoggingLevelEnum LoggingLevel { get; set; }

        // private cached login credentailes
        string DeviceId { get; set; }
        string DevicePassword { get; set; }
    }
}
