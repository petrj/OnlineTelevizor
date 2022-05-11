using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TVAPI
{
    public interface ITVAPI
    {
        void SetCredentials(string username, string password, string childLockPIN = null);
        void SetConnection(string deviceId, string password);
        DeviceConnection Connection { get; }
        bool EPGEnabled { get; }
        bool QualityFilterEnabled { get; }
        bool RecordingEnabled { get; }
        StatusEnum Status { get; }
        Task Login(bool force = false);
        Task<List<Channel>> GetChannels(string quality = null);
        Task<List<EPGItem>> GetEPG();
        Task<Dictionary<string, List<EPGItem>>> GetChannelsEPG();
        Task<string> GetEPGItemDescription(EPGItem epgItem);
        Task<List<Quality>> GetStreamQualities();
        void ResetConnection();
        Task Unlock();
        Task Lock();
        Task Stop();
    }
}
