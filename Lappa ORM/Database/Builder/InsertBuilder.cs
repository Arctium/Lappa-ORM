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
    }
}
