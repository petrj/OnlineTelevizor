using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVLive.Models
{
    public class FilterItem : BaseNotifableObject
    {
        private int _count = 0;

        public string Name { get; set; }
        public string GUIName
        {
            get
            {
                switch (Name)
                {
                    case "general": return "Obecné";
                    case "": return "Nepojmenovaná skupina";
                    case "news": return "Zpravodajství";
                    case "children": return "Pro děti";
                    case "documentary": return "Dokumenty";
                    case "foreign": return "Zahraniční";
                    case "regional": return "Regionální";
                    case "movie": return "Filmy";
                    case "other": return "Ostatní";

                    default: return Name;
                }
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
                OnPropertyChanged(nameof(CountAsString));
            }
        }

        public string CountAsString
        {
            get
            {
                return "(" + _count.ToString() + ")";
            }
        }

    }
}
