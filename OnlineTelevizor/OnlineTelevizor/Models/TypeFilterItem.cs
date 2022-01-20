using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor.Models
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
                    case "*": res = "Vše"; break;
                    case "tv": res = "Televizní kanály"; break;
                    case "radio": res = "Rádia"; break;
                }

                return $"{res} ({Count.ToString()})";
            }
        }
    }
}
