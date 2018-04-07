// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM.Logging
{
    public interface ILog
    {
        void Message(Enum logTypes, string message);
    }
}
