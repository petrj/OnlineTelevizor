using System;

namespace TVAPI
{
    public class Credentials : JSONObject
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ChildLockPIN { get; set; }

        public bool IsEmpty
        {
            get
            {
                return String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Password);
            }
        }
    }
}
