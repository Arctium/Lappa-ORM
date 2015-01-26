// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T> : ExpressionVisitor where T : Entity, new()
    {
        public PropertyInfo[] Properties { get; }
        public Func<T, object>[] PropertyGetter { get; }
        public Action<T, object>[] PropertySetter { get; }

        StringBuilder sqlQuery = new StringBuilder();

        internal QueryBuilder() { }

        internal QueryBuilder(PropertyInfo[] properties, IReadOnlyList<MemberInfo> members = null)
        {
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

        protected override Expression VisitBinary(BinaryExpression bExpression)
        {
            sqlQuery.Append("(");

            Visit(bExpression.Left);

            string condition;

            switch (bExpression.NodeType)
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
                    condition = "Not Supported";
                    break;
            }

            if (condition == " AND " || condition == " OR ")
                sqlQuery.Append(condition);
            else
            {
                MemberExpression memberExp = null;
                object exVal = null;

                if (bExpression.Right.NodeType == ExpressionType.MemberAccess)
                    memberExp = bExpression.Right as MemberExpression;
                else if (bExpression.Right.NodeType == ExpressionType.Convert)
                    memberExp = (bExpression.Right as UnaryExpression)?.Operand as MemberExpression;
                else if (bExpression.Right.NodeType == ExpressionType.Constant)
                    exVal = (bExpression.Right as ConstantExpression)?.Value;

                exVal = exVal ?? GetExpressionValue(memberExp);

                var finalVal = exVal ?? Regex.Replace(Regex.Replace(bExpression.Right.ToString(), "^\"|\"$", ""), @"^Convert\(|\)$", "");

                if (bExpression.Right.Type == typeof(string))
                    sqlQuery.AppendFormat("{0}{1}'{2}'", Regex.Replace(bExpression.Left.ToString(), @"^Convert\(|\)$", ""), condition, finalVal);
                else
                    sqlQuery.AppendFormat("{0}{1}{2}", Regex.Replace(bExpression.Left.ToString(), @"^Convert\(|\)$", ""), condition, finalVal);
            }

            Visit(bExpression.Right);

            sqlQuery.Append(")");

            return bExpression;
        }

        protected internal object GetExpressionValue(MemberExpression mExpression, BinaryExpression bExpression = null)
        {
            MemberExpression memberExp = mExpression;

            if (memberExp != null)
            {
                var memberExpressionStore = new List<MemberExpression>();

                while (memberExp.Expression is MemberExpression)
                {
                    memberExpressionStore.Add(memberExp);

                    memberExp = (MemberExpression)memberExp.Expression;
                }

                var constExpression = (memberExp.Expression as ConstantExpression);

                if (constExpression != null)
                {
                    var info = constExpression.Value.GetType().GetRuntimeFields().SingleOrDefault(fi => fi.Name == memberExp.Member.Name);
                    var objReference = info?.GetValue(constExpression.Value);

                    if (objReference != null)
                    {
                        if (objReference.GetType().IsPrimitive || objReference.GetType() == typeof(string))
                            return objReference;

                        for (var i = memberExpressionStore.Count; i > 1; i--)
                        {
                            objReference = (memberExpressionStore[i - 1].Member as PropertyInfo).GetValue(objReference);

                            memberExpressionStore.RemoveAt(i - 1);
                        }

                        object val = null;

                        var memberInfo = mExpression;

                        if (memberInfo != null)
                        {
                            var fieldInfo = memberInfo.Member as FieldInfo;
                            var propertyInfo = memberInfo.Member as PropertyInfo;

                            if (fieldInfo != null)
                                val = fieldInfo.GetValue(objReference).ChangeType(fieldInfo.FieldType);
                            else if (propertyInfo != null)
                                val = propertyInfo.GetValue(objReference).ChangeType(propertyInfo.PropertyType);
                        }

                        if (bExpression != null && val == null)
                        {
                            memberInfo = (bExpression.Right as UnaryExpression)?.Operand as MemberExpression;

                            if (memberInfo != null)
                            {
                                var fieldInfo = memberInfo.Member as FieldInfo;
                                var propertyInfo = memberInfo.Member as PropertyInfo;

                                if (fieldInfo != null)
                                    val = fieldInfo.GetValue(objReference).ChangeType(fieldInfo.FieldType);
                                else if (propertyInfo != null)
                                    val = propertyInfo.GetValue(objReference).ChangeType(propertyInfo.PropertyType);
                            }
                        }

                        if ((val == null) && bExpression == null || (val == null) && (!(bExpression.Right is MemberExpression) || objReference.GetType() == (bExpression.Left as MemberExpression)?.Type))
                            return objReference;

                        return val;
                    }
                }
            }

            return null;
        }
    }
}
