// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Lappa.ORM.Caching;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal StringBuilder BuildSelectAll()
        {
            return new StringBuilder().AppendFormat(NumberFormat, "SELECT * FROM " + connectorQuery.Table, PluralizedEntityName);
        }

        internal StringBuilder BuildSelect(IReadOnlyList<MemberInfo> members)
        {
            var sqlQuery = new StringBuilder();

            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(NumberFormat, connectorQuery.Table + ", ", members[i].GetName());

            sqlQuery.AppendFormat(NumberFormat, "FROM " + connectorQuery.Table, PluralizedEntityName);
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery;
        }

        internal StringBuilder BuildSelectCount()
        {
            return new StringBuilder().AppendFormat(NumberFormat, "SELECT COUNT(*) FROM " + connectorQuery.Table, PluralizedEntityName);
        }

        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildWhereAll(Expression expression)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();

            // ToDo: Add support for query more than 1 table
            sqlQuery.AppendFormat(NumberFormat, "SELECT * FROM " + connectorQuery.Table + " WHERE ", PluralizedEntityName);

            var queryExpression = new QueryExpression<T>(connectorQuery, sqlQuery, sqlParameters);

            queryExpression.Visit(expression);

            return (sqlQuery, sqlParameters);
        }

        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildWhere(Expression expression, IReadOnlyList<MemberInfo> members)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();

            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(NumberFormat, connectorQuery.Table + ", ", members[i].GetName());

            sqlQuery.AppendFormat(NumberFormat, "FROM " + connectorQuery.Table + " WHERE ", PluralizedEntityName);

            // Fix query
            sqlQuery.Replace(", FROM", " FROM");

            var queryExpression = new QueryExpression<T>(connectorQuery, sqlQuery, sqlParameters);

            queryExpression.Visit(expression);

            return (sqlQuery, sqlParameters);
        }

        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildWhereCount(Expression expression)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();

            sqlQuery.AppendFormat(NumberFormat, "SELECT COUNT(*) FROM " + connectorQuery.Table + " WHERE ", PluralizedEntityName);

            var queryExpression = new QueryExpression<T>(connectorQuery, sqlQuery, sqlParameters);

            queryExpression.Visit(expression);

            return (sqlQuery, sqlParameters);
        }

        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildWhereForeignKey(Type foreignKeyType, string foreignKeyTable, string foreignKeyName, object foreignKeyValue)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();
            var properties = foreignKeyType.GetReadWriteProperties();

            // Re-assign properties for our foreign key entity.
            Properties = new List<(PropertyInfo Info, TypeInfoCache InfoCache)>(properties.Length);

            foreach (var p in properties)
            {
                Properties.Add((p, new TypeInfoCache(p.PropertyType.IsArray, p.PropertyType.GetCustomAttribute<GroupAttribute>() != null, p.PropertyType.IsCustomClass(), p.PropertyType.IsCustomStruct())));
            }

            sqlQuery.AppendFormat(NumberFormat, "SELECT * FROM " + connectorQuery.Table + " WHERE ", foreignKeyTable);
            sqlQuery.AppendFormat(NumberFormat, connectorQuery.Equal, foreignKeyName, foreignKeyValue);

            sqlParameters.Add($"@{foreignKeyName}", foreignKeyValue is bool ? Convert.ToByte(foreignKeyValue) : foreignKeyValue);

            return (sqlQuery, sqlParameters);
        }
    }
}
