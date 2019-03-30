using System;

namespace SledovaniTVAPI
{   
        public enum StatusEnum
        {   
            NotInitialized = 0,
            EmptyCredentials = 1,
            Paired = 2,
            PairingFailed = 3,
            Logged = 4,
            LoginFailed = 5
        }
}
