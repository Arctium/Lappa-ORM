// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using Lappa.ORM.Misc;
using static Lappa.ORM.Misc.Helper;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildUpdate(T entity, PropertyInfo[] properties, PropertyInfo[] primaryKeys)
        {
            var typeName = Pluralize<T>();

            sqlQuery.AppendFormat(numberFormat, connectorQuery.UpdateQuery, typeName, typeName[0]);

            for (var i = 0; i < properties.Length; i++)
            {
                // Don't update primary keys.
                if (properties[i].HasAttribute<PrimaryKeyAttribute>())
                    continue;

                var value = properties[i].GetGetter<T>().GetValue(entity);

                sqlQuery.AppendFormat(numberFormat, connectorQuery.Equal + ", ", properties[i].GetName(), value is bool ? Convert.ToByte(value) : value);
            }

            sqlQuery.AppendFormat(numberFormat, connectorQuery.UpdateQueryEnd, typeName, typeName[0]);
            sqlQuery.AppendFormat(numberFormat, connectorQuery.Equal, primaryKeys[0].GetName(), primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
                sqlQuery.AppendFormat(numberFormat, connectorQuery.AndEqual, primaryKeys[i].GetName(), primaryKeys[i].GetGetter<T>().GetValue(entity));

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(MethodCallExpression[] expression, bool preSql)
        {
            sqlQuery.AppendFormat(numberFormat, connectorQuery.UpdateQuery, Pluralize<T>());

            for (var i = 0; i < expression.Length; i++)
            {
                var member = (expression[i].Arguments[0] as MemberExpression).Member.GetName();
                MemberExpression memberExp = null;
                object value = null;

                if (expression[i].Arguments[1].NodeType == ExpressionType.MemberAccess)
                    memberExp = expression[i].Arguments[1] as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Convert)
                    memberExp = (expression[i].Arguments[1] as UnaryExpression).Operand as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Constant)
                    value = (expression[i].Arguments[1] as ConstantExpression).Value;

                value = value ?? GetExpressionValue(memberExp);

                sqlQuery.AppendFormat(numberFormat, connectorQuery.Equal + ", ", member, value is bool ? Convert.ToByte(value) : value);
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
