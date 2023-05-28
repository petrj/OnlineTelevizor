using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor
{
    public interface IOnKeyDown
    {
        void OnKeyDown(string key, bool longPress);
        void OnTextSent(string text);
    }
}
