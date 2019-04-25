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
                var res = Name;

                switch (Name)
                {
                    case "*": res = "Všechny typy"; break;
                    case "tv": res = "Televizní kanály"; break;
                    case "radio": res = "Rádia"; break;                    
                }

                return $"{res} {CountAsString}";
            }
        }
    }
}
