using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVAPI
{
    public class TVChannel : JSONObject
    {
       public string ChannelNumber { get; set; }

       public string Name { get; set;  }
       public string Url { get; set; }
       public string Id { get; set; }
       public string Type { get; set; }
       public string LogoUrl { get; set; }
       public string Locked { get; set; }
       public string ParentLocked { get; set; }
       public string Group { get; set; }

       public List<EPGItem> EPGItems { get; set; } = new List<EPGItem>();

       public String CurrentEPGTitle
       {
            get
            {
                if (EPGItems.Count == 0)
                    return null;

                return EPGItems[0].Title;
            }
       }
    }
}
