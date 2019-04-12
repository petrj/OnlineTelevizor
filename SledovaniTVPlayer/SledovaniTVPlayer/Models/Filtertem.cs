using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVPlayer.Models
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
                    case "general": return "Obecne";
                    case "": return "Nepojmenovana skupina";
                    case "news": return "Zpravodajstvi";
                    case "children": return "Pro deti";
                    case "documentary": return "Dokumenty";
                    case "foreign": return "Zahranicni";
                    case "regional": return "Regionalni";
                    case "movie": return "Filmy";

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
