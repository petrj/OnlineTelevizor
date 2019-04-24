using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVLive.Models
{
    public class TypeFilterItem : FilterItem
    {
        public override string GUIName
        {
            get
            {
                switch (Name)
                {
                    case "*": return "Všechny typy";
                    case "tv": return "Televizní kanály";
                    case "radio": return "Rádia";
                    default: return Name;
                }
            }
        }
    }
}
