// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Lappa.ORM.Caching;

namespace Lappa.ORM
{
    interface IQueryBuilder
    {
        bool IsSelectQuery { get; set; }

        StringBuilder SqlQuery { get; }
        Dictionary<string, object> SqlParameters { get; }

        object EntityDummy { get; }
        string EntityName { get; }
        string PluralizedEntityName { get; }

        List<(PropertyInfo Info, TypeInfoCache InfoCache)> Properties { get; }
    }
}
