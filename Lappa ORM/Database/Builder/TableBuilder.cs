// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    internal partial class QueryBuilder<T>
    {
        internal string BuildTableCreate(Dictionary<string, PropertyInfo> fields, MySqlEngine dbEngine)
        {
            var pluralized = Helper.Pluralize<T>();

            sqlQuery.Append($"DROP TABLE IF EXISTS `{pluralized}`;");
            sqlQuery.Append($"CREATE TABLE `{pluralized}` (");

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
                        typeDefinition = $"tinyint({fieldSize})";
                        break;
                    case "SByte":
                        typeDefinition = $"tinyint({fieldSize})";
                        break;
                    case "Byte":
                        typeDefinition = $"tinyint({fieldSize}) unsigned";
                        break;
                    case "Int16":
                        typeDefinition = $"smallint({fieldSize})";
                        break;
                    case "UInt16":
                        typeDefinition = $"smallint({fieldSize}) unsigned";
                        break;
                    case "Int32":
                        typeDefinition = $"int({fieldSize})";
                        break;
                    case "UInt32":
                        typeDefinition = $"int({fieldSize}) unsigned";
                        break;
                    case "Int64":
                        typeDefinition = $"bigint({fieldSize})";
                        break;
                    case "UInt64":
                        typeDefinition = $"bigint({fieldSize}) unsigned";
                        break;
                    case "Single":
                        typeDefinition = $"float({fieldSize})";
                        break;
                    case "Double":
                        typeDefinition = $"double({fieldSize})";
                        break;
                    case "String":
                        nullAllowed = true;

                        if (fieldSize <= 255)
                            typeDefinition = $"varchar({fieldSize})";
                        else
                            typeDefinition = "text(0)";
                        break;
                    default:
                        break;
                }

                if (!nullAllowed)
                    typeDefinition += " NOT NULL";

                typeDefinition += $" DEFAULT '{defaultValue}'";

                if (isAutoIncrement)
                    typeDefinition += " AUTO_INCREMENT";

                sqlQuery.Append($"  `{f.Key}` {typeDefinition},");
            }

            if (primaryKeys.Length > 0)
                sqlQuery.Append("PRIMARY KEY (");

            for (var i = 0; i < primaryKeys.Length; i++)
            {
                if (i == primaryKeys.Length - 1)
                    sqlQuery.Append($"`{primaryKeys[i].Name}`)");
                else
                    sqlQuery.Append($"`{primaryKeys[i].Name}`,");
            }

            sqlQuery.Append($") ENGINE={Enum.GetName(typeof(MySqlEngine), dbEngine)} DEFAULT CHARSET=utf8;");
            sqlQuery.Replace("', ),", "'),");
            sqlQuery.Replace("', );", "');");
            sqlQuery.Replace("',)", "')");

            return sqlQuery.ToString();
        }
    }
}
