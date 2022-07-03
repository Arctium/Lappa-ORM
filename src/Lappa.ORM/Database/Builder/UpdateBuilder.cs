// Copyright (C) Arctium.
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
        internal void BuildUpdate(T entity, PropertyInfo[] properties, PropertyInfo[] primaryKeys)
        {
            SqlQuery.AppendFormat(numberFormat, connectorQuery.UpdateQuery, PluralizedEntityName, PluralizedEntityName[0]);

            for (var i = 0; i < properties.Length; i++)
            {
                // Don't update primary keys.
                if (properties[i].HasAttribute<PrimaryKeyAttribute>())
                    continue;

                var name = properties[i].GetName();
                var value = properties[i].GetGetter<T>().GetValue(entity);

                SqlQuery.AppendFormat(numberFormat, connectorQuery.Equal + ", ", name);
                SqlParameters.Add($"@{name}", value is bool ? Convert.ToByte(value) : value);
            }

            SqlQuery.AppendFormat(numberFormat, connectorQuery.UpdateQueryEnd, PluralizedEntityName, PluralizedEntityName[0]);

            var mainPrimaryKeyName = primaryKeys[0].GetName();

            // Append first primary key condition.
            SqlQuery.AppendFormat(numberFormat, connectorQuery.Equal, mainPrimaryKeyName);
            SqlParameters.Add($"@{mainPrimaryKeyName}", primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
            {
                var primaryKeyName = primaryKeys[i].GetName();

                SqlQuery.AppendFormat(numberFormat, connectorQuery.AndEqual, primaryKeyName);
                SqlParameters.Add($"{primaryKeyName}", primaryKeys[i].GetGetter<T>().GetValue(entity));
            }

            SqlQuery.Replace(", WHERE", " WHERE");
            SqlQuery.Replace(", FROM", " FROM");
        }

        internal void BuildUpdate(MethodCallExpression[] expression, Expression condition)
        {
            SqlQuery.AppendFormat(numberFormat, connectorQuery.UpdateQuery, Pluralize<T>());

            for (var i = 0; i < expression.Length; i++)
            {
                var memberName = (expression[i].Arguments[0] as MemberExpression).Member.GetName();

                MemberExpression memberExpression = null;
                object memberValue = null;

                if (expression[i].Arguments[1].NodeType == ExpressionType.MemberAccess)
                    memberExpression = expression[i].Arguments[1] as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Convert)
                    memberExpression = (expression[i].Arguments[1] as UnaryExpression).Operand as MemberExpression;
                else if (expression[i].Arguments[1].NodeType == ExpressionType.Constant)
                    memberValue = (expression[i].Arguments[1] as ConstantExpression).Value;

                memberValue = memberValue ?? GetExpressionValue(memberExpression);

                SqlQuery.AppendFormat(numberFormat, connectorQuery.Equal + ", ", memberName);
                SqlParameters.Add($"@{memberName}", memberValue);
            }

            if (condition != null)
            {
                SqlQuery.Append("WHERE ");

                Visit(condition);

                SqlQuery.Replace(", WHERE", " WHERE");
                SqlQuery.Replace(", FROM", " FROM");
            }
            else
                SqlQuery.Remove(SqlQuery.Length - 2, 2);
        }
    }
}
