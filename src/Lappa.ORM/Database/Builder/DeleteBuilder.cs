// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildDelete(T entity, PropertyInfo[] primaryKeys)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();

            sqlQuery.AppendFormat(NumberFormat, connectorQuery.DeleteQuery, PluralizedEntityName, PluralizedEntityName[0]);

            var mainPrimaryKeyName = primaryKeys[0].GetName();

            // Append first primary key condition.
            sqlQuery.AppendFormat(NumberFormat, connectorQuery.Equal, mainPrimaryKeyName);
            sqlParameters.Add($"{mainPrimaryKeyName}", primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
            {
                var primaryKeyName = primaryKeys[i].GetName();

                sqlQuery.AppendFormat(NumberFormat, connectorQuery.AndEqual, primaryKeyName);
                sqlParameters.Add($"{primaryKeyName}", primaryKeys[i].GetGetter<T>().GetValue(entity));
            }

            return (sqlQuery, sqlParameters);
        }

        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildDelete(Expression expression)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();

            sqlQuery.AppendFormat(NumberFormat, connectorQuery.DeleteQuery, PluralizedEntityName);

            var queryExpression = new QueryExpression<T>(connectorQuery, sqlQuery, sqlParameters);

            queryExpression.Visit(expression);

            return (sqlQuery, sqlParameters);
        }
    }
}
