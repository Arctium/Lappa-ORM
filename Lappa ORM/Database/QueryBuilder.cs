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
    // Only for internal usage.
    // TODO Cleanup (maybe split into partial classes)
    internal class QueryBuilder<T> : ExpressionVisitor where T : Entity, new()
    {
        public PropertyInfo[] Properties { get; private set; }
        public Func<T, object>[] PropertyGetter { get; private set; }
        public Action<T, object>[] PropertySetter { get; private set; }

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

        internal string BuildTableCreate(Dictionary<string, PropertyInfo> fields, MySqlEngine dbEngine)
        {
            var pluralized = typeof(T).Name.Pluralize();

            sqlQuery.Append("DROP TABLE IF EXISTS `\{pluralized}`;");
            sqlQuery.Append("CREATE TABLE `\{pluralized}` (");

            var primaryKeys = fields.Values.Where(p => p.HasAttribute<PrimaryKeyAttribute>()).ToArray();

            foreach (var f in fields)
            {
                var isAutoIncrement = f.Value.HasAttribute<AutoIncrementAttribute>();
                var fieldOptions = f.Value.GetAttribute<FieldAttribute>();
                var fieldType = f.Value.PropertyType.IsArray ? f.Value.PropertyType.GetElementType() : f.Value.PropertyType;
                var fieldSize = fieldOptions?.Size ?? Helper.GetDefaultFieldSize(fieldType);
                var defaultValue = fieldOptions?.Default ?? Helper.GetDefault(fieldType);
                var nullAllowed = fieldOptions?.Null ?? false;
                var typeDefinition = "";

                switch (fieldType.Name)
                {
                    case "Boolean":
                        typeDefinition = "tinyint(\{fieldSize})";
                        break;
                    case "SByte":
                        typeDefinition = "tinyint(\{fieldSize})";
                        break;
                    case "Byte":
                        typeDefinition = "tinyint(\{fieldSize}) unsigned";
                        break;
                    case "Int16":
                        typeDefinition = "smallint(\{fieldSize})";
                        break;
                    case "UInt16":
                        typeDefinition = "smallint(\{fieldSize}) unsigned";
                        break;
                    case "Int32":
                        typeDefinition = "int(\{fieldSize})";
                        break;
                    case "UInt32":
                        typeDefinition = "int(\{fieldSize}) unsigned";
                        break;
                    case "Int64":
                        typeDefinition = "bigint(\{fieldSize})";
                        break;
                    case "UInt64":
                        typeDefinition = "bigint(\{fieldSize}) unsigned";
                        break;
                    case "Single":
                        typeDefinition = "float(\{fieldSize})";
                        break;
                    case "Double":
                        typeDefinition = "double(\{fieldSize})";
                        break;
                    case "String":
                        nullAllowed = true;

                        if (fieldSize <= 255)
                            typeDefinition = "varchar(\{fieldSize})";
                        else
                            typeDefinition = "text(0)";
                        break;
                    default:
                        break;
                }

                if (!nullAllowed)
                    typeDefinition += " NOT NULL";

                typeDefinition += " DEFAULT '\{defaultValue}'";

                if (isAutoIncrement)
                    typeDefinition += " AUTO_INCREMENT";

                sqlQuery.Append("  `\{f.Key}` \{typeDefinition},");
            }

            if (primaryKeys.Length > 0)
                sqlQuery.Append("PRIMARY KEY (");

            for (var i = 0; i < primaryKeys.Length; i++)
            {
                if (i == primaryKeys.Length - 1)
                    sqlQuery.Append("`\{primaryKeys[i].Name}`)");
                else
                    sqlQuery.Append("`\{primaryKeys[i].Name}`,");
            }

            sqlQuery.Append(") ENGINE=\{Enum.GetName(typeof(MySqlEngine), dbEngine)} DEFAULT CHARSET=utf8;");
            sqlQuery.Replace("', ),", "'),");
            sqlQuery.Replace("', );", "');");
            sqlQuery.Replace("',)", "')");

            return sqlQuery.ToString();
        }

        internal string BuildInsert(Dictionary<string, object> values)
        {
            sqlQuery.AppendFormat("INSERT INTO " + QuerySettings.Part0 + " (", typeof(T).Name.Pluralize());

            foreach (var name in values.Keys)
                sqlQuery.AppendFormat(QuerySettings.Part0 + ",", name);

            sqlQuery.Append(") VALUES (");

            foreach (var val in values.Values)
            {
                if (val != null && val.GetType().IsArray)
                {
                    var arr = val as Array;

                    for (var i = 0; i < arr.Length; i++)
                        sqlQuery.AppendFormat("'{0}',", arr.GetValue(i));
                }
                else
                {
                    var value = val?.ChangeType(val.GetType());

                    if (value is string)
                        value = ((string)value).Replace("\"", "\"\"").Replace("'", @"\'");

                    sqlQuery.AppendFormat("'{0}',", value);
                }
            }

            sqlQuery.Append(")");
            sqlQuery.Replace(",)", ")");

            return sqlQuery.ToString();
        }

        internal List<string> BuildBulkInsert(PropertyInfo[] properties, IEnumerable<T> entities)
        {
            var queries = new List<string>();
            var values = new Dictionary<string, object>(properties.Length);

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType.IsArray)
                {
                    var arr = (PropertyGetter[i].GetValue(Activator.CreateInstance<T>()) as Array);

                    for (var j = 1; j <= arr.Length; j++)
                        values.Add(properties[i].Name + j, new object());
                }
                else
                    values.Add(properties[i].Name, new object());
            }

            sqlQuery.AppendFormat("INSERT INTO " + QuerySettings.Part0 + " (", typeof(T).Name.Pluralize());

            foreach (var name in values.Keys)
                sqlQuery.AppendFormat(QuerySettings.Part0 + ",", name);

            sqlQuery.Append(") VALUES ");

            foreach (var entity in entities)
            {
                if (sqlQuery.Length >= 15000)
                {
                    sqlQuery.Append(";");

                    sqlQuery.Replace(",)", ")");
                    sqlQuery.Replace("),;", ");");
                    sqlQuery.Remove(sqlQuery.Length - 1, 1);

                    queries.Add(sqlQuery.ToString());

                    sqlQuery = new StringBuilder();

                    sqlQuery.AppendFormat("INSERT INTO " + QuerySettings.Part0 + " (", typeof(T).Name.Pluralize());

                    foreach (var name in values.Keys)
                        sqlQuery.AppendFormat(QuerySettings.Part0 + ",", name);

                    sqlQuery.Append(") VALUES ");
                }

                sqlQuery.Append("(");

                for (var i = 0; i < properties.Length; i++)
                {
                    if (properties[i].PropertyType.IsArray)
                    {
                        var arr = (PropertyGetter[i].GetValue(entity) as Array);

                        for (var j = 1; j <= arr.Length; j++)
                            values[properties[i].Name + j] = arr.GetValue(j - 1);
                    }
                    else if (!properties[i].HasAttribute<AutoIncrementAttribute>())
                    {
                        var val = PropertyGetter[i].GetValue(entity);

                        if (val is string)
                            val = ((string)val).Replace("\"", "\"\"").Replace("'", @"\'");

                        values[properties[i].Name] = val;
                    }
                }

                foreach (var val in values.Values)
                {
                    if (val.GetType().IsArray)
                    {
                        var arr = val as Array;

                        for (var i = 0; i < arr.Length; i++)
                            sqlQuery.AppendFormat("'{0}',", arr.GetValue(i));
                    }
                    else if (val != null)
                        sqlQuery.AppendFormat("'{0}',", val is bool ? Convert.ToByte(val) : val.ChangeType(val.GetType()));
                    else
                        sqlQuery.AppendFormat("'',");
                }

                sqlQuery.Append("),");
            }

            sqlQuery.Replace(",)", ")");
            sqlQuery.Replace("),;", ");");
            sqlQuery.Remove(sqlQuery.Length - 1, 1);

            queries.Add(sqlQuery.ToString());

            return queries;
        }

        internal string BuildSelectAll()
        {
            sqlQuery.AppendFormat("SELECT * FROM " + QuerySettings.Part0, typeof(T).Name.Pluralize());

            return sqlQuery.ToString();
        }

        internal string BuildSelect(IReadOnlyList<MemberInfo> members)
        {
            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(QuerySettings.Part0 + ", ", members[i].Name);

            sqlQuery.AppendFormat("FROM " + QuerySettings.Part0, typeof(T).Name.Pluralize());

            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(QuerySettings querySettings, T entity, PropertyInfo[] properties, PropertyInfo[] primaryKeys)
        {
            sqlQuery.AppendFormat(querySettings.UpdateQuery, typeof(T).Name.Pluralize(), typeof(T).Name[0]);

            for (var i = 0; i < properties.Length; i++)
                sqlQuery.AppendFormat(querySettings.Equal + ", ", properties[i].Name, properties[i].GetGetter<T>().GetValue(entity));

            sqlQuery.AppendFormat(querySettings.UpdateQueryEnd, typeof(T).Name.Pluralize(), typeof(T).Name[0]);
            sqlQuery.AppendFormat(querySettings.Equal, primaryKeys[0].Name, primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
                sqlQuery.AppendFormat(querySettings.AndEqual, primaryKeys[i].Name, primaryKeys[i].GetGetter<T>().GetValue(entity));

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(QuerySettings querySettings, T entity, PropertyInfo[] primaryKeys, string[] fields)
        {
            sqlQuery.AppendFormat(querySettings.UpdateQuery, typeof(T).Name.Pluralize(), typeof(T).Name[0]);

            for (var i = 0; i < fields.Length; i++)
                sqlQuery.AppendFormat(querySettings.Equal + ", ", fields[i], typeof(T).GetProperty(fields[i]).GetGetter<T>().GetValue(entity));

            sqlQuery.AppendFormat(querySettings.UpdateQueryEnd, typeof(T).Name.Pluralize(), typeof(T).Name[0]);
            sqlQuery.AppendFormat(querySettings.Equal, primaryKeys[0].Name, primaryKeys[0].GetGetter<T>().GetValue(entity));

            for (var i = 1; i < primaryKeys.Length; i++)
                sqlQuery.AppendFormat(querySettings.AndEqual, primaryKeys[i].Name, primaryKeys[i].GetGetter<T>().GetValue(entity));

            sqlQuery.Replace(", WHERE", " WHERE");
            sqlQuery.Replace(", FROM", " FROM");

            return sqlQuery.ToString();
        }

        internal string BuildUpdate(Expression expression, QuerySettings querySettings, T entity, string param, string[] fields)
        {
            sqlQuery.AppendFormat(querySettings.UpdateQuery, typeof(T).Name.Pluralize(), param);

            for (var i = 0; i < fields.Length; i++)
                sqlQuery.AppendFormat(querySettings.Equal + ", ", fields[i], typeof(T).GetProperty(fields[i]).GetGetter<T>().GetValue(entity));

            sqlQuery.AppendFormat(querySettings.UpdateQueryEnd, typeof(T).Name.Pluralize(), param);

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

                sqlQuery.AppendFormat(querySettings.Equal + ", ", member, value);
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

        internal string BuildWhereAll(Expression expression, string param)
        {
            // ToDo: Add support for query more than 1 table
            sqlQuery.AppendFormat("SELECT * FROM " + QuerySettings.Part0 + " " + QuerySettings.Part1 + " WHERE ", typeof(T).Name.Pluralize(), param);

            Visit(expression);

            return sqlQuery.ToString();
        }

        internal string BuildWhere(Expression expression, string param, IReadOnlyList<MemberInfo> members)
        {
            sqlQuery.Append("SELECT ");

            for (var i = 0; i < members.Count; i++)
                sqlQuery.AppendFormat(QuerySettings.Part0 + ", ", members[i].Name);

            sqlQuery.AppendFormat("FROM " + QuerySettings.Part0 + " " + QuerySettings.Part1 + " WHERE ", typeof(T).Name.Pluralize(), param);

            // Fix query
            sqlQuery.Replace(", FROM", " FROM");

            Visit(expression);

            return sqlQuery.ToString();
        }

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
