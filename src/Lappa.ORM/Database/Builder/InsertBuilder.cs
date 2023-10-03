// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    internal partial class QueryBuilder<T>
    {
        internal void BuildInsert(Dictionary<string, (object ColumnValue, bool IsArrayGroup)> values)
        {
            SqlQuery.AppendFormat(numberFormat, "INSERT INTO " + connectorQuery.Table + " (", PluralizedEntityName);

            foreach (var name in values.Keys)
                SqlQuery.AppendFormat(numberFormat, connectorQuery.Table + ",", name);

            SqlQuery.Append(") VALUES (");

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
                            SqlQuery.AppendFormat(numberFormat, $"@{kp.Key},");
                            SqlParameters.Add($"@{kp.Key}", arr.GetValue(i).ChangeTypeSet(valType));
                        }
                    }
                    else
                    {
                        SqlQuery.AppendFormat(numberFormat, $"@{kp.Key},");
                        SqlParameters.Add($"@{kp.Key}", kp.Value.ColumnValue.ChangeTypeSet(valType));
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
    }
}
