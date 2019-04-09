using System;
namespace SledovaniTVAPI
{
    public class EPGItem : JSONObject
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
    }
}
