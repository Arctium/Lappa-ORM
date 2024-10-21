// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Lappa.ORM.Misc;

using static Lappa.ORM.Misc.Helper;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildUpdate(T entity, PropertyInfo[] properties, PropertyInfo[] primaryKeys)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();

            sqlQuery.AppendFormat(NumberFormat, connectorQuery.UpdateQuery, PluralizedEntityName, PluralizedEntityName[0]);

            for (var i = 0; i < properties.Length; i++)
            {
                // Don't update primary keys.
                if (properties[i].HasAttribute<PrimaryKeyAttribute>())
                    continue;

                var name = properties[i].GetName();
                var value = PropertyGetter[i](entity);

                sqlQuery.AppendFormat(NumberFormat, connectorQuery.Equal + ", ", name);
                sqlParameters.Add($"@{name}", value is bool ? Convert.ToByte(value) : value);
            }

            sqlQuery.AppendFormat(NumberFormat, connectorQuery.UpdateQueryEnd, PluralizedEntityName, PluralizedEntityName[0]);

            var mainPrimaryKeyName = primaryKeys[0].GetName();

            // Append first primary key condition.
            sqlQuery.AppendFormat(NumberFormat, connectorQuery.Equal, mainPrimaryKeyName);
            sqlParameters.Add($"@{mainPrimaryKeyName}", primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
            {
                var primaryKeyName = primaryKeys[i].GetName();

                sqlQuery.AppendFormat(NumberFormat, connectorQuery.AndEqual, primaryKeyName);
                sqlParameters.Add($"{primaryKeyName}", primaryKeys[i].GetGetter<T>().GetValue(entity));
            }

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return (sqlQuery, sqlParameters);
        }

        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildUpdate(MethodCallExpression[] expression, Expression condition)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();
            var queryExpression = new QueryExpression<T>(connectorQuery, sqlQuery, sqlParameters);

            sqlQuery.AppendFormat(NumberFormat, connectorQuery.UpdateQuery, Pluralize<T>());

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

                memberValue ??= QueryExpression<T>.GetExpressionValue(memberExpression);

                sqlQuery.AppendFormat(NumberFormat, connectorQuery.Equal + ", ", memberName);
                sqlParameters.Add($"@{memberName}", memberValue);
            }

            if (condition != null)
            {
                sqlQuery.Append("WHERE ");

                queryExpression.Visit(condition);

                sqlQuery.Replace(", WHERE", " WHERE");
                sqlQuery.Replace(", FROM", " FROM");
            }
            else
                sqlQuery.Remove(sqlQuery.Length - 2, 2);

            return (sqlQuery, sqlParameters);
        }
    }
}
