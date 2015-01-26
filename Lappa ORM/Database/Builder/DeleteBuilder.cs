// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildDelete(QuerySettings querySettings, T entity, PropertyInfo[] primaryKeys)
        {
            sqlQuery.AppendFormat(querySettings.DeleteQuery, typeof(T).Name.Pluralize(), typeof(T).Name[0]);
            sqlQuery.AppendFormat(querySettings.Equal, primaryKeys[0].Name, primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
                sqlQuery.AppendFormat(querySettings.AndEqual, primaryKeys[i].Name, primaryKeys[i].GetGetter<T>().GetValue(entity));

            return sqlQuery.ToString();
        }

        internal string BuildDelete(Expression expression, QuerySettings querySetting, string param)
        {
            sqlQuery.AppendFormat(querySetting.DeleteQuery, typeof(T).Name.Pluralize(), param);

            Visit(expression);

            return sqlQuery.ToString();
        }
    }
}
