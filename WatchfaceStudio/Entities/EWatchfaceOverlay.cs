﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatchfaceStudio.Entities
{
    [Flags]
    public enum EWatchfaceOverlay
    {
        None = 0,
        WearIcons = 1,
        Card = 2
    }
}
