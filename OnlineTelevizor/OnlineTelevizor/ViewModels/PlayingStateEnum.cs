using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineTelevizor.ViewModels
{
    public enum PlayingStateEnum
    {
        Stopped = 0,
        PlayingInternal = 1,
        PlayingInPreview = 2,
        Casting = 3
    }
}
