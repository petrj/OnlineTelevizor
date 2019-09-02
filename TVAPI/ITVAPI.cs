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
        StatusEnum Status { get; }       
        Task Login(bool force = false);
        Task<List<EPGItem>> GetEPG();
        Task<List<Quality>> GetStreamQualities();
        void ResetConnection();
        Task<List<Channel>> GetChanels();
        Task Unlock();
        Task Lock();
    }
}
