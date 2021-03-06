﻿// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM.Logging
{
    internal class Log : ILog
    {
        public void Message(Enum logTypes, string message)
        {
            // Use the default logger when no custom logger is assigned.
#if DEBUG
            Console.WriteLine(message);
#endif
        }
    }
}
