// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using LappaORM.Misc;

namespace LappaORM
{
    internal partial class QueryBuilder<T> : ExpressionVisitor where T : Entity, new()
    {
        public PropertyInfo[] Properties { get; }
        public Func<T, object>[] PropertyGetter { get; }
        public Action<T, object>[] PropertySetter { get; }

        StringBuilder sqlQuery = new StringBuilder();
        ConnectorQuery connectorQuery;

        // Use en-US as number format for all languages.
        IFormatProvider numberFormat = new CultureInfo("en-US").NumberFormat;

        internal QueryBuilder(ConnectorQuery connectorQuery)
        {
            this.connectorQuery = connectorQuery;
        }

        internal QueryBuilder(ConnectorQuery connectorQuery, PropertyInfo[] properties, IReadOnlyList<MemberInfo> members = null)
        {
            this.connectorQuery = connectorQuery;

            if (members != null)
            {
                var props = new PropertyInfo[members.Count];

                for (var i = 0; i < members.Count; i++)
                {
                    for (var j = 0; j < properties.Length; j++)
                    {
                        if (properties[j].Name == members[i].Name)
                        {
                            props[i] = properties[j];
                            break;
                        }
                    }
                }

                properties = props;
            }

            Properties = properties;

            PropertyGetter = new Func<T, object>[properties.Length];
            PropertySetter = new Action<T, object>[properties.Length];

            for (var i = 0; i < properties.Length; i++)
            {
                PropertyGetter[i] = properties[i].GetGetter<T>();
                PropertySetter[i] = properties[i].GetSetter<T>();
            }
        }

        protected override Expression VisitMember(MemberExpression memberEexpression)
        {
            // TODO: Find a better way...
            if ((memberEexpression.Member as PropertyInfo)?.PropertyType == typeof(bool))
            {
                var expressionString = memberEexpression.ToString();
                var count = 0;

                for (var i = 0; i != -1; i += 3)
                {
                    if (expressionString.IndexOf("Not", i) == -1)
                        break;

                    count++;
                }

                sqlQuery.AppendFormat(numberFormat, connectorQuery.Part0 + "{1}'{2}'", memberEexpression?.Member?.Name, " = ", count % 2 == 0 ? "1" : "0");
            }

            return memberEexpression;
        }

        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            sqlQuery.Append("(");

            string condition;

            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    condition = " = ";
                    break;
                case ExpressionType.NotEqual:
                    condition = " <> ";
                    break;
                case ExpressionType.GreaterThan:
                    condition = " > ";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    condition = " >= ";
                    break;
                case ExpressionType.LessThan:
                    condition = " < ";
                    break;
                case ExpressionType.LessThanOrEqual:
                    condition = " <= ";
                    break;
                case ExpressionType.AndAlso:
                    condition = " AND ";
                    break;
                case ExpressionType.OrElse:
                    condition = " OR ";
                    break;
                default:
                    throw new NotSupportedException($"{binaryExpression.NodeType} is not supported.");
            }

            if (condition == " AND " || condition == " OR ")
            {
                Visit(binaryExpression.Left);

                sqlQuery.Append(condition);
            }
            else
            {
                MemberExpression memberExp = null;
                object exVal = null;

                if (binaryExpression.Right.NodeType == ExpressionType.MemberAccess)
                    memberExp = binaryExpression.Right as MemberExpression;
                else if (binaryExpression.Right.NodeType == ExpressionType.Convert)
                    memberExp = (binaryExpression.Right as UnaryExpression)?.Operand as MemberExpression;
                else if (binaryExpression.Right.NodeType == ExpressionType.Constant)
                    exVal = (binaryExpression.Right as ConstantExpression)?.Value;
                else if (binaryExpression.Right.NodeType == ExpressionType.Call)
                {
                    var methodMember = Expression.Convert(binaryExpression.Right, typeof(object));

                    exVal = Expression.Lambda<Func<object>>(methodMember).Compile()();
                }

                exVal = exVal ?? GetExpressionValue(memberExp);

                var finalVal = exVal ?? Regex.Replace(Regex.Replace(binaryExpression.Right.ToString(), "^\"|\"$", ""), @"^Convert\(|\)$", "");
                var left = (binaryExpression.Left as MemberExpression)?.Member ?? ((binaryExpression.Left as UnaryExpression).Operand as MemberExpression).Member;

                sqlQuery.AppendFormat(numberFormat, connectorQuery.Part0 + "{1}'{2}'", left.Name, condition, finalVal is bool ? Convert.ToByte(finalVal) : finalVal);
            }

            Visit(binaryExpression.Right);

            sqlQuery.Append(")");

            return binaryExpression;
        }

        object GetValue(object objReference, MemberExpression memberExpression, BinaryExpression binaryExpression)
        {
            object val = null;

            if (memberExpression != null)
            {
                var fieldInfo = memberExpression.Member as FieldInfo;
                var propertyInfo = memberExpression.Member as PropertyInfo;

                if (fieldInfo != null)
                    val = fieldInfo.GetValue(objReference).ChangeTypeSet(fieldInfo.FieldType);
                else if (propertyInfo != null)
                    val = propertyInfo.GetValue(objReference).ChangeTypeSet(propertyInfo.PropertyType);
            }

            if (binaryExpression != null && val == null)
            {
                memberExpression = (binaryExpression.Right as UnaryExpression)?.Operand as MemberExpression;

                if (memberExpression != null)
                {
                    var fieldInfo = memberExpression.Member as FieldInfo;
                    var propertyInfo = memberExpression.Member as PropertyInfo;

                    if (fieldInfo != null)
                        val = fieldInfo.GetValue(objReference).ChangeTypeSet(fieldInfo.FieldType);
                    else if (propertyInfo != null)
                        val = propertyInfo.GetValue(objReference).ChangeTypeSet(propertyInfo.PropertyType);
                }
            }

            if (val == null && (binaryExpression == null || (!(binaryExpression.Right is MemberExpression) || objReference.GetType() == (binaryExpression.Left as MemberExpression)?.Type)))
                return objReference;

            return val;
        }

        protected internal object GetExpressionValue(MemberExpression memberExpression, BinaryExpression binaryExpression = null)
        {
            var memberExp = memberExpression;

            if (memberExp != null)
            {
                if (memberExp.Expression == null)
                    return GetValue(null, memberExpression, binaryExpression);

                var memberExpressionStore = new List<MemberExpression>();

                while (memberExp.Expression is MemberExpression)
                {
                    memberExpressionStore.Add(memberExp);

                    memberExp = (MemberExpression)memberExp.Expression;
                }

                var constExpression = (memberExp.Expression as ConstantExpression);

                FieldInfo info;
                object objReference;

                if (constExpression == null)
                    objReference = (memberExp.Member as FieldInfo)?.GetValue(null) ?? (memberExp.Member as PropertyInfo)?.GetValue(null);
                else
                {
                    info = constExpression.Value.GetType().GetRuntimeFields().SingleOrDefault(fi => fi.Name == memberExp.Member.Name);
                    objReference = info?.GetValue(constExpression.Value);
                }

                if (objReference != null)
                {
                    if (objReference.GetType().GetTypeInfo().IsPrimitive || objReference.GetType() == typeof(string))
                        return objReference;

                    for (var i = memberExpressionStore.Count; i > 1; i--)
                    {
                        if (memberExpressionStore[i - 1].Member is PropertyInfo)
                            objReference = (memberExpressionStore[i - 1].Member as PropertyInfo).GetValue(objReference);
                        else
                            objReference = (memberExpressionStore[i - 1].Member as FieldInfo).GetValue(objReference);

                        memberExpressionStore.RemoveAt(i - 1);
                    }

                    return GetValue(objReference, memberExpression, binaryExpression);
                }
            }

            return null;
        }
    }
}
