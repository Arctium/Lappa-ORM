// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LappaORM.Logging
{
    public interface ILog<in T>
    {
        void Message(T logTypes, string message);
    }
}
