// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lappa.ORM.Constants;
using Lappa.ORM.Logging;
using Lappa.ORM.Misc;
using static Lappa.ORM.Misc.Helper;

namespace Lappa.ORM
{
    internal class EntityBuilder
    {
        Database database;

        public EntityBuilder(Database database)
        {
            this.database = database;
        }

        public object[][] VerifyDatabaseSchema<TEntity>(DbDataReader dataReader, QueryBuilder<TEntity> builder) where TEntity : Entity, new()
        {
            // Return false if the used DbDataReader is null or doesn't contain any rows.
            if (dataReader?.Read() == false)
                return new object[1][];

            // Some custom queries do not use a query builder.
            // This results in no column/property verification.
            if (builder != null)
            {
                var fieldCount = builder.PropertySetter.Length;
                var arrayFieldCount = 0;
                var classFieldCount = 0;
                var structFieldCount = 0;
                var pluralizedEntityName = Pluralize<TEntity>();

                for (var i = 0; i < builder.Properties.Count; i++)
                {
                    if (builder.Properties[i].InfoCache.IsArray)
                    {
                        var arr = builder.Properties[i].Info.GetValue(new TEntity()) as Array;

                        arrayFieldCount += arr.Length - 1;
                    }
                    else if (builder.Properties[i].InfoCache.IsCustomClass)
                        classFieldCount += builder.Properties[i].Info.PropertyType.GetReadWriteProperties().Length - 1;
                    else if (builder.Properties[i].InfoCache.IsCustomStruct)
                        structFieldCount += builder.Properties[i].Info.PropertyType.GetTypeInfo().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length - 1;
                }

                // Workaround for types like Datetime.
                if (structFieldCount < 0)
                    structFieldCount = 0;

                var totalFieldCount = fieldCount + arrayFieldCount + classFieldCount + structFieldCount;

                if (dataReader.FieldCount != totalFieldCount)
                {
                    database.Log.Message(LogTypes.Error, $"Table '{pluralizedEntityName}' (Column/Property count mismatch)\nColumns '{dataReader.FieldCount}'\nProperties '{totalFieldCount}'");

                    return new object[1][];
                }

                // TODO:
                // - Move this check to a new Manager class, so it's done one time on initialization.
                // - Fix array field type checks.
                // - Optimize?!
                // Strict types (signed/unsigned) only used in MySql databases.
                if (database.Connector.Settings.DatabaseType == DatabaseType.MySql)
                {
                    var hasMismatches = false;

                    for (var i = 0; i < fieldCount; i++)
                    {
                        if (builder.Properties[i].Info.PropertyType == typeof(bool) || builder.Properties[i].InfoCache.IsArray ||
                            builder.Properties[i].InfoCache.IsCustomClass || builder.Properties[i].InfoCache.IsCustomStruct)
                            continue;

                        if (!dataReader.GetFieldType(i).GetTypeInfo().IsEquivalentTo(builder.Properties[i].Info.PropertyType.GetTypeInfo().IsEnum ?
                            builder.Properties[i].Info.PropertyType.GetTypeInfo().GetEnumUnderlyingType() : builder.Properties[i].Info.PropertyType))
                        {
                            var propertyType = builder.Properties[i].Info.PropertyType.GetTypeInfo().IsEnum ? builder.Properties[i].Info.PropertyType.GetTypeInfo().GetEnumUnderlyingType() : builder.Properties[i].Info.PropertyType;

                            database.Log.Message(LogTypes.Error, $"Table '{pluralizedEntityName}' (Column/Property type mismatch)");
                            database.Log.Message(LogTypes.Error, $"{dataReader.GetName(i)}: {dataReader.GetFieldType(i)}/{propertyType}");

                            hasMismatches = true;
                        }
                    }

                    // Return an empty list if any column/property type mismatches
                    if (hasMismatches)
                    {
                        database.Log.Message(LogTypes.Warning, $"Returning no data for table {pluralizedEntityName}.");

                        return new object[1][];
                    }
                }
            }

            var entities = new ConcurrentBag<object[]>();

            // Create the row array
            do
            {
                var row = new object[dataReader.FieldCount];

                dataReader.GetValues(row);

                entities.Add(row);
            } while (dataReader.Read());

            return new object[1][];
        }

        public TEntity[] CreateEntities<TEntity>(object[][] data, QueryBuilder<TEntity> builder) where TEntity : Entity, new()
        {
            var fieldCount = builder.PropertySetter.Length;
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
                var group = builder.Properties[i].Info.GetCustomAttribute<GroupAttribute>();

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

            // Disabled until the rewrite is finished.
            //var assignForeignKeys = new TEntity().LoadForeignKeys && foreignKeys.Length > 0 && groups.Count == 0;

            Parallel.For(0, data.Length, i =>
            {
                var row = data[i];
                var entity = new TEntity();

                for (int j = 0, a = 0; j < row.Length; j++)
                {
                    if (!builder.Properties[j].InfoCache.IsArray)
                    {
                        if (builder.Properties[j].InfoCache.IsCustomClass)
                        {
                            var instanceFields = builder.Properties[j].Info.PropertyType.GetReadWriteProperties();
                            var instance = Activator.CreateInstance(builder.Properties[j].Info.PropertyType);

                            for (var f = 0; f < instanceFields.Length; f++)
                                instanceFields[f].SetValue(instance, row[j + f]);

                            builder.PropertySetter[j](entity, instance);
                        }
                        else if (builder.Properties[j].InfoCache.IsCustomStruct)
                        {
                            var instanceFields = builder.Properties[j].Info.PropertyType.GetTypeInfo().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();

                            // Workaround for types like Datetime.
                            if (instanceFields.Length > 0)
                            {
                                var instance = Activator.CreateInstance(builder.Properties[j].Info.PropertyType);

                                for (var f = 0; f < instanceFields.Length; f++)
                                    instanceFields[f].SetValue(instance, row[j + f].ChangeTypeGet(builder.Properties[j + f].Info.PropertyType));

                                builder.PropertySetter[j](entity, instance);
                            }
                            else
                                builder.PropertySetter[j](entity, row[j].ChangeTypeGet(builder.Properties[j].Info.PropertyType));
                        }
                        else
                            builder.PropertySetter[j](entity, row[j]);
                    }
                    else
                    {
                        if (groups.TryGetValue(j, out int groupCount))
                        {
                            for (var c = 0; c < groupCount; c++, j++)
                            {
                                var arr = builder.Properties[j].Info.GetValue(entity) as Array;

                                for (var k = 0; k < arr.Length; k++, a++)
                                    arr.SetValue(row[j + (k * groupCount)], k);

                                builder.PropertySetter[j](entity, arr);

                                // TODO: Test field groups.
                                // -1 for each new array field.
                                --a;
                            }
                        }
                        else
                        {
                            var arr = builder.Properties[j].Info.GetValue(entity) as Array;

                            for (var k = 0; k < arr.Length; k++, a++)
                                arr.SetValue(row[j + a], k);

                            builder.PropertySetter[j](entity, arr);

                            // -1 for each new array field.
                            --a;
                        }
                    }
                }

                // TODO Fix group assignment in foreign keys.
                // Disabled until the rewrite is finished.
                //if (assignForeignKeys)
                //    database.AssignForeignKeyData(entity, foreignKeys, groups);

                entity.InitializeNonTableProperties();

                entities.Add(entity);
            });

            return entities.ToArray();
        }
    }
}
