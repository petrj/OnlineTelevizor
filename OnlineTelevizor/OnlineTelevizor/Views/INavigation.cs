using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor
{
    public interface INavigationSelectNextItem
    {
        void SelectNextItem();
    }

    public interface INavigationSelectPreviousItem
    {
        void SelectPreviousItem();
    }

    public interface INavigationSendOKButton
    {
        void SendOKButton();
    }

    public interface INavigationScrollUpDown
    {
        void ScrollDown();
        void ScrollUp();
    }
}
