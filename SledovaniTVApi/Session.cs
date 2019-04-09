using System;

namespace SledovaniTVAPI
{
    public class Session : JSONObject
    {
        public Session()
        { }

        public string PHPSESSID { get; set; }
    }
}
