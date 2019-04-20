using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTVLive.Models
{
    public class FilterItem : BaseNotifableObject
    {
        private int _count = 0;

        public string Name { get; set; }
        public virtual string GUIName
        {
            get
            {
                return Name;
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
