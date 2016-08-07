// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LappaORM.Logging
{
    internal class Log : ILog<LogTypes>
    {
        public void Message(LogTypes logTypes, string message)
        {
            // Dummy logging.
        }
    }
}
