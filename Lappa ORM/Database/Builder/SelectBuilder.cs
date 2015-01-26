// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildSelectAll()
        {
            sqlQuery.AppendFormat("SELECT * FROM " + QuerySettings.Part0, typeof(T).Name.Pluralize());

            return sqlQuery.ToString();
        }

        internal string BuildSelect(IReadOnlyList<MemberInfo> members)
        {
            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(QuerySettings.Part0 + ", ", members[i].Name);

            sqlQuery.AppendFormat("FROM " + QuerySettings.Part0, typeof(T).Name.Pluralize());

            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildWhereAll(Expression expression, string param)
        {
            // ToDo: Add support for query more than 1 table
            sqlQuery.AppendFormat("SELECT * FROM " + QuerySettings.Part0 + " " + QuerySettings.Part1 + " WHERE ", typeof(T).Name.Pluralize(), param);

            Visit(expression);

            return sqlQuery.ToString();
        }

        internal string BuildWhere(Expression expression, string param, IReadOnlyList<MemberInfo> members)
        {
            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(QuerySettings.Part0 + ", ", members[i].Name);

            sqlQuery.AppendFormat("FROM " + QuerySettings.Part0 + " " + QuerySettings.Part1 + " WHERE ", typeof(T).Name.Pluralize(), param);

            // Fix query
            sqlQuery.Replace(", FROM", " FROM");

            Visit(expression);

            return sqlQuery.ToString();
        }
    }
}
