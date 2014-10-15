﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NAPS2
{
    public interface ILogger
    {
        void Error(string message);
        void ErrorException(string message, Exception exception);
    }
}