// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;

namespace Lappa_ORM
{
    internal interface IDatabase
    {
        T[] CreateEntities<T>(DataTable data, QueryBuilder<T> builder) where T : Entity, new();
    }
}
