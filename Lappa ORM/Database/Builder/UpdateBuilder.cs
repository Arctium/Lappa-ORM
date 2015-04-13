// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildUpdate(QuerySettings querySettings, T entity, PropertyInfo[] properties, PropertyInfo[] primaryKeys)
        {
            var typeName = typeof(T).Name;

            sqlQuery.AppendFormat(querySettings.UpdateQuery, typeName.Pluralize(), typeName[0]);

            for (var i = 0; i < properties.Length; i++)
                sqlQuery.AppendFormat(querySettings.Equal + ", ", properties[i].Name, properties[i].GetGetter<T>().GetValue(entity));

            sqlQuery.AppendFormat(querySettings.UpdateQueryEnd, typeName.Pluralize(), typeName[0]);
            sqlQuery.AppendFormat(querySettings.Equal, primaryKeys[0].Name, primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
                sqlQuery.AppendFormat(numberFormat, querySettings.AndEqual, primaryKeys[i].Name, primaryKeys[i].GetGetter<T>().GetValue(entity));

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(QuerySettings querySettings, T entity, PropertyInfo[] primaryKeys, string[] fields)
        {
            var type = typeof(T);
            var typeName = type.Name;

            sqlQuery.AppendFormat(querySettings.UpdateQuery, typeName.Pluralize(), typeName[0]);

            for (var i = 0; i < fields.Length; i++)
                sqlQuery.AppendFormat(numberFormat, querySettings.Equal + ", ", fields[i], type.GetProperty(fields[i]).GetGetter<T>().GetValue(entity));

            sqlQuery.AppendFormat(querySettings.UpdateQueryEnd, typeName.Pluralize(), typeName[0]);
            sqlQuery.AppendFormat(querySettings.Equal, primaryKeys[0].Name, primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
                sqlQuery.AppendFormat(numberFormat, querySettings.AndEqual, primaryKeys[i].Name, primaryKeys[i].GetGetter<T>().GetValue(entity));

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(Expression expression, QuerySettings querySettings, T entity, string param, string[] fields)
        {
            var type = typeof(T);
            var typeName = type.Name;

            sqlQuery.AppendFormat(querySettings.UpdateQuery, typeName.Pluralize(), param);

            for (var i = 0; i < fields.Length; i++)
                sqlQuery.AppendFormat(querySettings.Equal + ", ", fields[i], type.GetProperty(fields[i]).GetGetter<T>().GetValue(entity));

            sqlQuery.AppendFormat(numberFormat, querySettings.UpdateQueryEnd, typeName.Pluralize(), param);

            Visit(expression);

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(MethodCallExpression[] expression, QuerySettings querySettings, string param, bool preSql)
        {
            sqlQuery.AppendFormat(querySettings.UpdateQuery, typeof(T).Name.Pluralize(), param);

            for (var i = 0; i < expression.Length; i++)
            {
                var member = (expression[i].Arguments[0] as MemberExpression).ToString();
                MemberExpression memberExp = null;
                object value = null;

                if (expression[i].Arguments[1].NodeType == ExpressionType.MemberAccess)
                    memberExp = expression[i].Arguments[1] as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Convert)
                    memberExp = (expression[i].Arguments[1] as UnaryExpression).Operand as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Constant)
                    value = (expression[i].Arguments[1] as ConstantExpression).Value;

                value = value ?? GetExpressionValue(memberExp);

                sqlQuery.AppendFormat(numberFormat, querySettings.Equal + ", ", member, value);
            }

            if (!preSql)
                sqlQuery.Remove(sqlQuery.Length - 2, 2);

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(Expression expression)
        {
            Visit(expression);

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }
    }
}
