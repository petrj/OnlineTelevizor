using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVPlayer.Models
{
    public interface ISledovaniTVConfiguration
    {
        string Username { get; set; }
        string Password { get; set; }
        string ChildLockPIN { get; set; }

        bool ShowLocked { get; set; }
        bool EnableLogging { get; set; }

        string DeviceId { get; set; }
        string DevicePassword { get; set; }
    }
}
