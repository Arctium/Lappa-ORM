// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static Lappa_ORM.Misc.Helper;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildSelectAll()
        {
            sqlQuery.AppendFormat(numberFormat, "SELECT * FROM " + QuerySettings.Part0, Pluralize<T>());

            return sqlQuery.ToString();
        }

        internal string BuildSelect(IReadOnlyList<MemberInfo> members)
        {
            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(numberFormat, QuerySettings.Part0 + ", ", members[i].Name);

            sqlQuery.AppendFormat(numberFormat, "FROM " + QuerySettings.Part0, Pluralize<T>());

            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildWhereAll(Expression expression, string param)
        {
            // ToDo: Add support for query more than 1 table
            sqlQuery.AppendFormat(numberFormat, "SELECT * FROM " + QuerySettings.Part0 + " " + QuerySettings.Part1 + " WHERE ", Pluralize<T>(), param);

            Visit(expression);

            return sqlQuery.ToString();
        }

        internal string BuildWhere(Expression expression, string param, IReadOnlyList<MemberInfo> members)
        {
            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(numberFormat, QuerySettings.Part0 + ", ", members[i].Name);

            sqlQuery.AppendFormat(numberFormat, "FROM " + QuerySettings.Part0 + " " + QuerySettings.Part1 + " WHERE ", Pluralize<T>(), param);

            // Fix query
            sqlQuery.Replace(", FROM", " FROM");

            Visit(expression);

            return sqlQuery.ToString();
        }
    }
}
