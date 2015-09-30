// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Lappa_ORM.Misc;
using static Lappa_ORM.Misc.Helper;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildUpdate(T entity, PropertyInfo[] properties, PropertyInfo[] primaryKeys)
        {
            var typeName = Pluralize<T>();

            sqlQuery.AppendFormat(numberFormat, querySettings.UpdateQuery, typeName, typeName[0]);

            for (var i = 0; i < properties.Length; i++)
                sqlQuery.AppendFormat(numberFormat, querySettings.Equal + ", ", properties[i].Name, properties[i].GetGetter<T>().GetValue(entity));

            sqlQuery.AppendFormat(numberFormat, querySettings.UpdateQueryEnd, typeName, typeName[0]);
            sqlQuery.AppendFormat(numberFormat, querySettings.Equal, primaryKeys[0].Name, primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
                sqlQuery.AppendFormat(numberFormat, querySettings.AndEqual, primaryKeys[i].Name, primaryKeys[i].GetGetter<T>().GetValue(entity));

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(MethodCallExpression[] expression, bool preSql)
        {
            sqlQuery.AppendFormat(numberFormat, querySettings.UpdateQuery, Pluralize<T>());

            for (var i = 0; i < expression.Length; i++)
            {
                var member = (expression[i].Arguments[0] as MemberExpression).Member.Name;
                MemberExpression memberExp = null;
                object value = null;

                if (expression[i].Arguments[1].NodeType == ExpressionType.MemberAccess)
                    memberExp = expression[i].Arguments[1] as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Convert)
                    memberExp = (expression[i].Arguments[1] as UnaryExpression).Operand as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Constant)
                    value = (expression[i].Arguments[1] as ConstantExpression).Value;

                value = value ?? GetExpressionValue(memberExp);

                sqlQuery.AppendFormat(numberFormat, querySettings.Equal + ", ", member, value is bool ? Convert.ToByte(value) : value);
            }

            if (!preSql)
                sqlQuery.Remove(sqlQuery.Length - 2, 2);

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(Expression expression)
        {
            sqlQuery.Append("WHERE ");

            Visit(expression);

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }
    }
}
