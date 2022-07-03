// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Lappa.ORM.Constants;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    public partial class Database<T>
    {
        public ValueTask<bool> Create<TEntity>(bool replaceTable = false) where TEntity : IEntity, new()
        {
            throw new NotImplementedException($"Database.Create not implemented for {Connector.Settings.DatabaseType}.");
        }
    }
}
