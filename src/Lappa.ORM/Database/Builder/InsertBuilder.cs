// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal void BuildInsert(Dictionary<string, object> values)
        {
            SqlQuery.AppendFormat(numberFormat, "INSERT INTO " + connectorQuery.Part0 + " (", PluralizedEntityName);

            foreach (var name in values.Keys)
                SqlQuery.AppendFormat(numberFormat, connectorQuery.Part0 + ",", name);

            SqlQuery.Append(") VALUES (");

            foreach (var kp in values)
            {
                if (kp.Value != null)
                {
                    Type valType;

                    if ((valType = kp.Value.GetType()).IsArray)
                    {
                        valType = valType.GetElementType();

                        var arr = kp.Value as Array;

                        for (var i = 0; i < arr.Length; i++)
                        {
                            SqlQuery.AppendFormat(numberFormat, $"@{kp.Key},");
                            SqlParameters.Add($"@{kp.Key}", arr.GetValue(i).ChangeTypeSet(valType));
                        }
                    }
                    else
                    {
                        SqlQuery.AppendFormat(numberFormat, $"@{kp.Key},");
                        SqlParameters.Add($"@{kp.Key}", kp.Value.ChangeTypeSet(valType));
                    }
                }
                else
                {
                    SqlQuery.AppendFormat(numberFormat, $"@{kp.Key},");
                    SqlParameters.Add($"@{kp.Key}", "");
                }
            }

            SqlQuery.Append(")");
            SqlQuery.Replace(",)", ")");
        }

        internal List<string> BuildBulkInsert(PropertyInfo[] properties, IReadOnlyList<T> entities)
        {
            /*var queries = new List<string>();
            var values = new Dictionary<string, object>(properties.Length);
            var typeName = Pluralize<T>();

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType.IsArray)
                {
                    var arr = PropertyGetter[i](new T()) as Array;

                    for (var j = 1; j <= arr.Length; j++)
                        values.Add(properties[i].GetName() + j, null);
                }
                else
                    values.Add(properties[i].GetName(), null);
            }

            sqlQuery.AppendFormat(numberFormat, "INSERT INTO " + connectorQuery.Part0 + " (", typeName);

            foreach (var name in values.Keys)
                sqlQuery.AppendFormat(numberFormat, connectorQuery.Part0 + ",", name);

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

                    sqlQuery.AppendFormat(numberFormat, "INSERT INTO " + connectorQuery.Part0 + " (", typeName);

                    foreach (var name in values.Keys)
                        sqlQuery.AppendFormat(numberFormat, connectorQuery.Part0 + ",", name);

                    sqlQuery.Append(") VALUES ");
                }

                sqlQuery.Append("(");

                for (var i = 0; i < properties.Length; i++)
                {
                    if (properties[i].PropertyType.IsArray)
                    {
                        var arr = (PropertyGetter[i](entity) as Array);
                        var arrElementType = arr.GetType().GetElementType();

                        for (var j = 1; j <= arr.Length; j++)
                            values[properties[i].GetName() + j] = arr.GetValue(j - 1).ChangeTypeGet(arrElementType);
                    }
                    else if (!properties[i].HasAttribute<AutoIncrementAttribute>())
                    {
                        var val = PropertyGetter[i](entity);

                        if (val is string)
                            val = ((string)val).Replace("\"", "\"\"").Replace("'", @"\'");

                        values[properties[i].GetName()] = val;
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

            return queries;*/
            return null;
        }
    }
}
