// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM.Logging
{
    [Flags]
    enum LogTypes
    {
        None    = 0x0,
        Info    = 0x1,
        Warning = 0x2,
        Error   = 0x4
    }
}
