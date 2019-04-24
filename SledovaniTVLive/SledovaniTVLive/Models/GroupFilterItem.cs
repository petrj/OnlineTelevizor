using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVLive.Models
{
    public class GroupFilterItem : FilterItem
    {
        public override string GUIName
        {
            get
            {
                switch (Name)
                {
                    case "*": return "Všechny skupiny";
                    case "general": return "Obecné";
                    case "": return "Nepojmenovaná skupina";
                    case "news": return "Zpravodajství";
                    case "children": return "Pro děti";
                    case "documentary": return "Dokumenty";
                    case "foreign": return "Zahraniční";
                    case "regional": return "Regionální";
                    case "movie": return "Filmy";
                    case "other": return "Ostatní";
                    case "music": return "Hudební";

                    default: return Name;
                }
            }
        }
    }
}
