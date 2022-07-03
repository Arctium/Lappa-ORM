// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
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
    }
}
