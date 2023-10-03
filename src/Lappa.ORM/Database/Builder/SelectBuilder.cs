// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Lappa.ORM.Caching;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal void BuildSelectAll()
        {
            SqlQuery.AppendFormat(numberFormat, "SELECT * FROM " + connectorQuery.Table, PluralizedEntityName);
        }

        internal void BuildSelect(IReadOnlyList<MemberInfo> members)
        {
            SqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                SqlQuery.AppendFormat(numberFormat, connectorQuery.Table + ", ", members[i].GetName());

            SqlQuery.AppendFormat(numberFormat, "FROM " + connectorQuery.Table, PluralizedEntityName);

            SqlQuery.Replace(", FROM", " FROM");
        }

        internal void BuildSelectCount()
        {
            SqlQuery.AppendFormat(numberFormat, "SELECT COUNT(*) FROM " + connectorQuery.Table, PluralizedEntityName);
        }

        internal void BuildWhereAll(Expression expression)
        {
            // ToDo: Add support for query more than 1 table
            SqlQuery.AppendFormat(numberFormat, "SELECT * FROM " + connectorQuery.Table + " WHERE ", PluralizedEntityName);

            Visit(expression);
        }

        internal void BuildWhere(Expression expression, IReadOnlyList<MemberInfo> members)
        {
            SqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                SqlQuery.AppendFormat(numberFormat, connectorQuery.Table + ", ", members[i].GetName());

            SqlQuery.AppendFormat(numberFormat, "FROM " + connectorQuery.Table + " WHERE ", PluralizedEntityName);

            // Fix query
            SqlQuery.Replace(", FROM", " FROM");

            Visit(expression);
        }

        internal void BuildWhereCount(Expression expression)
        {
            SqlQuery.AppendFormat(numberFormat, "SELECT COUNT(*) FROM " + connectorQuery.Table + " WHERE ", PluralizedEntityName);

            Visit(expression);
        }

        internal void BuildWhereForeignKey(Type foreignKeyType, string foreignKeyTable, string foreignKeyName, object foreignKeyValue)
        {
            // Clear the base query.
            SqlQuery.Clear();
            SqlParameters.Clear();

            var properties = foreignKeyType.GetReadWriteProperties();

            // Re-assign properties for our foreign key entity.
            Properties = new List<(PropertyInfo Info, TypeInfoCache InfoCache)>(properties.Length);

            foreach (var p in properties)
            {
                Properties.Add((p, new TypeInfoCache(p.PropertyType.IsArray, p.PropertyType.GetCustomAttribute<GroupAttribute>() != null, p.PropertyType.IsCustomClass(), p.PropertyType.IsCustomStruct())));
            }

            SqlQuery.AppendFormat(numberFormat, "SELECT * FROM " + connectorQuery.Table + " WHERE ", foreignKeyTable);
            SqlQuery.AppendFormat(numberFormat, connectorQuery.Equal, foreignKeyName, foreignKeyValue);

            SqlParameters.Add($"@{foreignKeyName}", foreignKeyValue is bool ? Convert.ToByte(foreignKeyValue) : foreignKeyValue);
        }
    }
}
