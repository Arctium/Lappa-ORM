// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) BuildInsert(Dictionary<string, (object ColumnValue, bool IsArrayGroup)> values)
        {
            var sqlQuery = new StringBuilder();
            var sqlParameters = new Dictionary<string, object>();

            sqlQuery.AppendFormat(NumberFormat, "INSERT INTO " + connectorQuery.Table + " (", PluralizedEntityName);

            foreach (var name in values.Keys)
                sqlQuery.AppendFormat(NumberFormat, connectorQuery.Table + ",", name);

            sqlQuery.Append(") VALUES (");

            foreach (var kp in values)
            {
                if (kp.Value.ColumnValue != null)
                {
                    var valType = kp.Value.ColumnValue.GetType();

                    if (kp.Value.IsArrayGroup && valType.IsArray)
                    {
                        valType = valType.GetElementType();

                        var arr = kp.Value.ColumnValue as Array;

                        for (var i = 0; i < arr.Length; i++)
                        {
                            sqlQuery.AppendFormat(NumberFormat, $"@{kp.Key},");
                            sqlParameters.Add($"@{kp.Key}", arr.GetValue(i).ChangeTypeSet(valType));
                        }
                    }
                    else
                    {
                        sqlQuery.AppendFormat(NumberFormat, $"@{kp.Key},");
                        sqlParameters.Add($"@{kp.Key}", kp.Value.ColumnValue.ChangeTypeSet(valType));
                    }
                }
                else
                {
                    sqlQuery.AppendFormat(NumberFormat, $"@{kp.Key},");
                    sqlParameters.Add($"@{kp.Key}", "");
                }
            }

            sqlQuery.Append(")");
            sqlQuery.Replace(",)", ")");

            return (sqlQuery, sqlParameters);
        }
    }
}
