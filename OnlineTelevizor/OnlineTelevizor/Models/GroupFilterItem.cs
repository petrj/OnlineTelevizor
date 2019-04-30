using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor.Models
{
    public class GroupFilterItem : FilterItem
    {
        public override string GUIName
        {
            get
            {
                var res = Name;

                switch (Name)
                {
                    case "*": res= "Všechny skupiny"; break;
                    case "general": res = "Obecné"; break;
                    case "": res = "Nepojmenovaná skupina"; break;
                    case "news": res = "Zpravodajství"; break;
                    case "children": res = "Pro děti"; break;
                    case "documentary": res = "Dokumenty"; break;
                    case "foreign": res = "Zahraniční"; break;
                    case "regional": res = "Regionální"; break;
                    case "movie": res = "Filmy"; break;
                    case "other": res = "Ostatní"; break;
                    case "music": res = "Hudební"; break;
                    case "sport": res = "Sportovní"; break;
                    case "erotic": res = "Erotické"; break;
                }

                return $"{res} ({Count.ToString()})";
            }
        }
    }
}
