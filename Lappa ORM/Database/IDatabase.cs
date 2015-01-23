// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;

namespace Lappa_ORM
{
    internal interface IDatabase
    {
        T[] CreateEntities<T>(DataTable data, QueryBuilder<T> builder) where T : Entity, new();
        List<T> GetEntityList<T>(DataTable data, QueryBuilder<T> builder) where T : Entity, new();
        Dictionary<TKey, TValue> GetEntityDictionary<TKey, TValue>(DataTable data, QueryBuilder<TValue> builder, Func<TValue, TKey> func) where TValue : Entity, new();
    }
}
