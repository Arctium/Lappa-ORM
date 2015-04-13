// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildInsert(Dictionary<string, object> values)
        {
            sqlQuery.AppendFormat("INSERT INTO " + QuerySettings.Part0 + " (", typeof(T).Name.Pluralize());

            foreach (var name in values.Keys)
                sqlQuery.AppendFormat(QuerySettings.Part0 + ",", name);

            sqlQuery.Append(") VALUES (");

            foreach (var val in values.Values)
            {
                if (val != null)
                {
                    Type valType;

                    if ((valType = val.GetType()).IsArray)
                    {
                        valType = valType.GetElementType();

                        var arr = val as Array;

                        for (var i = 0; i < arr.Length; i++)
                            sqlQuery.AppendFormat(numberFormat, "'{0}',", arr.GetValue(i).ChangeTypeSet(valType));
                    }
                    else
                    {
                        var value = val.ChangeTypeSet(valType);

                        if (value is string)
                            value = ((string)value).Replace("\"", "\"\"").Replace("'", @"\'");

                        sqlQuery.AppendFormat(numberFormat, "'{0}',", value);
                    }
                }
                else
                    sqlQuery.AppendFormat("'',");
            }

            sqlQuery.Append(")");
            sqlQuery.Replace(",)", ")");

            return sqlQuery.ToString();
        }

        internal List<string> BuildBulkInsert(PropertyInfo[] properties, IEnumerable<T> entities)
        {
            var queries = new List<string>();
            var values = new Dictionary<string, object>(properties.Length);
            var typeName = typeof(T).Name.Pluralize();

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType.IsArray)
                {
                    var arr = (PropertyGetter[i].GetValue(new T()) as Array);

                    for (var j = 1; j <= arr.Length; j++)
                        values.Add(properties[i].Name + j, null);
                }
                else
                    values.Add(properties[i].Name, null);
            }

            sqlQuery.AppendFormat("INSERT INTO " + QuerySettings.Part0 + " (", typeName);

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

                    sqlQuery.AppendFormat("INSERT INTO " + QuerySettings.Part0 + " (", typeName);

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
                        var arrElementType = arr.GetType().GetElementType();

                        for (var j = 1; j <= arr.Length; j++)
                            values[properties[i].Name + j] = arr.GetValue(j - 1).ChangeTypeGet(arrElementType);
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
                    if (val != null)
                    {
                        var valType = val.GetType();

                        if (valType.IsArray)
                        {
                            valType = valType.GetElementType();

                            var arr = val as Array;

                            for (var i = 0; i < arr.Length; i++)
                                sqlQuery.AppendFormat(numberFormat, "'{0}',", arr.GetValue(i).ChangeTypeSet(valType));
                        }
                        else
                            sqlQuery.AppendFormat(numberFormat, "'{0}',", val.ChangeTypeSet(valType));
                    }
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
    }
}
