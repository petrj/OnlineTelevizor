using System;
using System.Collections.Generic;
namespace SledovaniTVAPI
{
    public class Channels : JSONObject
    {
        //public Channel[] channels { get; set; }
        public List<Channel> channels { get; set; }
    }
}
