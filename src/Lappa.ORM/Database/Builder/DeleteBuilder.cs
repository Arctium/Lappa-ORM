// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal void BuildDelete(T entity, PropertyInfo[] primaryKeys)
        {
            SqlQuery.AppendFormat(numberFormat, connectorQuery.DeleteQuery, PluralizedEntityName, PluralizedEntityName[0]);

            var mainPrimaryKeyName = primaryKeys[0].GetName();

            // Append first primary key condition.
            SqlQuery.AppendFormat(numberFormat, connectorQuery.Equal, mainPrimaryKeyName);
            SqlParameters.Add($"{mainPrimaryKeyName}", primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
            {
                var primaryKeyName = primaryKeys[i].GetName();

                SqlQuery.AppendFormat(numberFormat, connectorQuery.AndEqual, primaryKeyName);
                SqlParameters.Add($"{primaryKeyName}", primaryKeys[i].GetGetter<T>().GetValue(entity));
            }
        }

        internal void BuildDelete(Expression expression)
        {
            SqlQuery.AppendFormat(numberFormat, connectorQuery.DeleteQuery, PluralizedEntityName);

            Visit(expression);
        }
    }
}
