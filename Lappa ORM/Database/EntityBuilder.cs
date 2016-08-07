// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using LappaORM.Logging;
using LappaORM.Misc;
using static LappaORM.Misc.Helper;

namespace LappaORM
{
    internal class EntityBuilder
    {
        Database parentDb;

        public EntityBuilder(Database parent)
        {
            parentDb = parent;
        }

        public TEntity[] CreateEntities<TEntity>(DbDataReader reader, QueryBuilder<TEntity> builder) where TEntity : Entity, new()
        {
            var fieldCount = builder.PropertySetter.Length;
            var arrayFieldCount = 0;
            var classFieldCount = 0;
            var structFieldCount = 0;

            if (reader == null || !reader.HasRows)
                return new TEntity[0];

            var pluralizedEntityName = Pluralize<TEntity>();

            for (var i = 0; i < builder.Properties.Length; i++)
            {
                if (builder.Properties[i].PropertyType.IsArray)
                {
                    var arr = builder.Properties[i].GetValue(new TEntity()) as Array;

                    arrayFieldCount += arr.Length - 1;
                }
                else if (builder.Properties[i].PropertyType.IsCustomClass())
                    classFieldCount += builder.Properties[i].PropertyType.GetReadWriteProperties().Length - 1;
                else if (builder.Properties[i].PropertyType.IsCustomStruct())
                    structFieldCount += builder.Properties[i].PropertyType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length - 1;
            }

            var totalFieldCount = fieldCount + arrayFieldCount + classFieldCount + structFieldCount;

            if (reader.FieldCount != totalFieldCount)
            {
                Helper.Log.Message(LogTypes.Error, $"Table '{pluralizedEntityName}' (Column/Property count mismatch)\nColumns '{reader.FieldCount}'\nProperties '{totalFieldCount}'");

                return new TEntity[0];
            }

            // No MySQL support for now.
            // Strict types only used in MySQL databases.
            /*if (parentDb.Type == DatabaseType.MySql)
            {
                for (var i = 0; i < fieldCount; i++)
                {
                    if (builder.Properties[i].PropertyType == typeof(bool) || builder.Properties[i].PropertyType.IsArray ||
                        builder.Properties[i].PropertyType.IsCustomClass() || builder.Properties[i].PropertyType.IsCustomStruct())
                        continue;

                    // Return an empty list if any column/property type mismatches
                    if (!data.Columns[i].DataType.IsEquivalentTo(builder.Properties[i].PropertyType.IsEnum ?
                        builder.Properties[i].PropertyType.GetEnumUnderlyingType() : builder.Properties[i].PropertyType))
                    {
                        var propertyType = builder.Properties[i].PropertyType.IsEnum ? builder.Properties[i].PropertyType.GetEnumUnderlyingType() : builder.Properties[i].PropertyType;

                        Helper.Log.Message(LogTypes.Error, $"Table '{pluralizedEntityName}' (Column/Property type mismatch)");
                        Helper.Log.Message(LogTypes.Error, $"{data.Columns[i].ColumnName}: {data.Columns[i].DataType}/{propertyType}");

                        return new TEntity[0];
                    }
                }
            }*/

            var entities = new ConcurrentBag<TEntity>();

            // Create one test object for foreign key assignment check
            var foreignKeys = typeof(TEntity).GetTypeInfo().DeclaredProperties.Where(p => p.GetMethod.IsVirtual).ToArray();

            // Key: GroupStartIndex, Value: GroupCount
            var groups = new ConcurrentDictionary<int, int>();
            var lastGroupName = "";
            var lastGroupStartIndex = 0;

            // Get Groups
            for (var i = 0; i < fieldCount; i++)
            {
                var group = builder.Properties[i].GetCustomAttribute<GroupAttribute>();

                if (group != null)
                {
                    if (group.Name == lastGroupName)
                        ++groups[lastGroupStartIndex];
                    else
                    {
                        lastGroupName = group.Name;
                        lastGroupStartIndex = i;

                        groups.TryAdd(lastGroupStartIndex, 1);
                    }
                }
            }

            var assignForeignKeys = new TEntity().LoadForeignKeys && foreignKeys.Length > 0 && groups.Count == 0;

            while (reader.Read())
            {
                var entity = new TEntity();
                var row = new object[fieldCount];

                reader.GetValues(row);

                for (var j = 0; j < fieldCount; j++)
                {
                    if (!builder.Properties[j].PropertyType.IsArray)
                    {
                        if (builder.Properties[j].PropertyType.IsCustomClass())
                        {
                            var instanceFields = builder.Properties[j].PropertyType.GetReadWriteProperties();
                            var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                            for (var f = 0; f < instanceFields.Length; f++)
                                instanceFields[f].SetValue(instance, reader.IsDBNull(j + f) ? "" : row[j + f]);

                            builder.PropertySetter[j].SetValue(entity, instance);
                        }
                        else if (builder.Properties[j].PropertyType.IsCustomStruct())
                        {
                            var instanceFields = builder.Properties[j].PropertyType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();
                            var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                            for (var f = 0; f < instanceFields.Length; f++)
                                instanceFields[f].SetValue(instance, reader.IsDBNull(j + f) ? "" : row[j + f].ChangeTypeGet(builder.Properties[j + f].PropertyType));

                            builder.PropertySetter[j].SetValue(entity, instance);
                        }
                        else
                            builder.PropertySetter[j].SetValue(entity, reader.IsDBNull(j) ? "" : row[j].ChangeTypeGet(builder.Properties[j].PropertyType));
                    }
                    else
                    {
                        var groupCount = 0;

                        if (groups.TryGetValue(j, out groupCount))
                        {
                            for (var c = 0; c < groupCount; c++, j++)
                            {
                                var arr = builder.Properties[j].GetValue(entity) as Array;

                                for (var k = 0; k < arr.Length; k++)
                                    arr.SetValue(row[j + (k * groupCount)], k);

                                builder.PropertySetter[j].SetValue(entity, arr);
                            }
                        }
                        else
                        {
                            var arr = builder.Properties[j].GetValue(entity) as Array;

                            for (var k = 0; k < arr.Length; k++)
                                arr.SetValue(row[j + k], k);

                            builder.PropertySetter[j].SetValue(entity, arr);
                        }
                    }

                    // TODO Fix group assignment in foreign keys.
                    if (assignForeignKeys)
                        parentDb.AssignForeignKeyData(entity, foreignKeys, groups);

                    entity.InitializeNonTableProperties();

                    entities.Add(entity);
                }
            };

            return entities.ToArray();
        }
    }
}
