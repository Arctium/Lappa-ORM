// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lappa_ORM.Misc;
using static Lappa_ORM.Misc.Helper;

namespace Lappa_ORM
{
    internal class MySqlDatabase : IDatabase
    {
        Database parentDb;

        public MySqlDatabase(Database parent)
        {
            parentDb = parent;
        }

        public TEntity[] CreateEntities<TEntity>(DataTable data, QueryBuilder<TEntity> builder) where TEntity : Entity, new()
        {
            var fieldCount = builder.PropertySetter.Length;
            var arrayFieldCount = 0;
            var classFieldCount = 0;
            var structFieldCount = 0;

            if (data == null || data.Rows.Count == 0)
                return new TEntity[0];

            var entity = new TEntity();

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

            if (data.Columns.Count != (fieldCount + arrayFieldCount + classFieldCount + structFieldCount))
            {
                Trace.TraceError(string.Format("Table '{0}' (Column/Property count mismatch)", Pluralize<TEntity>()));
                Trace.TraceError(string.Format("Columns '{0}'", data.Columns.Count));
                Trace.TraceError(string.Format("Properties '{0}'", fieldCount + arrayFieldCount + classFieldCount + structFieldCount));
                Trace.WriteLine("Press a key to continue loading.");

                Console.ReadKey(true);

                return new TEntity[0];
            }

            for (var i = 0; i < fieldCount; i++)
            {
                if (builder.Properties[i].PropertyType == typeof(bool) || builder.Properties[i].PropertyType.IsArray || builder.Properties[i].PropertyType.IsCustomClass() || builder.Properties[i].PropertyType.IsCustomStruct())
                    continue;

                // Return an empty list if any column/property type mismatches
                if (!data.Columns[i].DataType.IsEquivalentTo(builder.Properties[i].PropertyType.IsEnum ?
                    builder.Properties[i].PropertyType.GetEnumUnderlyingType() : builder.Properties[i].PropertyType))
                {
                    Trace.TraceError(string.Format("Table '{0}' (Column/Property type mismatch)", Pluralize<TEntity>()));
                    Trace.TraceError(string.Format("Column '{0}' ({1})", data.Columns[i].ColumnName, data.Columns[i].DataType));
                    Trace.TraceError(string.Format("Property '{0}' ({1})", builder.Properties[i].Name, builder.Properties[i].PropertyType.IsEnum ?
                                    builder.Properties[i].PropertyType.GetEnumUnderlyingType() : builder.Properties[i].PropertyType));

                    Trace.WriteLine("Press a key to continue loading.");

                    Console.ReadKey(true);

                    return new TEntity[0];
                }
            }

            var entities = new TEntity[data.Rows.Count];
            var datapPartitioner = Partitioner.Create(0, data.Rows.Count);

            // Create one test object for foreign key assignment check
            var foreignKeys = typeof(TEntity).GetProperties().Where(p => p.GetMethod.IsVirtual).ToArray();

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
                    {
                        groups[lastGroupStartIndex] += 1;
                    }
                    else
                    {
                        lastGroupName = group.Name;
                        lastGroupStartIndex = i;

                        groups.TryAdd(lastGroupStartIndex, 1);
                    }
                }
            }

            if (entity.AutoAssignForeignKeys && foreignKeys.Length > 0)
            {
                // TODO: Optimize foreign key assignment (slow atm...)
                Parallel.ForEach(datapPartitioner, (dataRange, loopState) =>
                {
                    for (var i = dataRange.Item1; i < dataRange.Item2; i++)
                    {
                        entities[i] = new TEntity();

                        for (var j = 0; j < fieldCount; j++)
                        {
                            if (!builder.Properties[j].PropertyType.IsArray)
                            {
                                if (builder.Properties[j].PropertyType.IsCustomClass())
                                {
                                    var instanceFields = builder.Properties[j].PropertyType.GetReadWriteProperties();
                                    var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                                    for (var f = 0; f < instanceFields.Length; f++)
                                        instanceFields[f].SetValue(instance, Convert.IsDBNull(data.Rows[i][j + f]) ? "" : data.Rows[i][j + f].ChangeTypeGet(builder.Properties[j].PropertyType));

                                    builder.PropertySetter[j].SetValue(entities[i], instance);
                                }
                                else if (builder.Properties[j].PropertyType.IsCustomStruct())
                                {
                                    var instanceFields = builder.Properties[j].PropertyType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();
                                    var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                                    for (var f = 0; f < instanceFields.Length; f++)
                                        instanceFields[f].SetValue(instance, Convert.IsDBNull(data.Rows[i][j + f]) ? "" : data.Rows[i][j + f].ChangeTypeGet(builder.Properties[j + f].PropertyType));

                                    builder.PropertySetter[j].SetValue(entities[i], instance);
                                }
                                else
                                    builder.PropertySetter[j].SetValue(entities[i], Convert.IsDBNull(data.Rows[i][j]) ? "" : data.Rows[i][j].ChangeTypeGet(builder.Properties[j].PropertyType));
                            }
                            else
                            {
                                var groupCount = 0;

                                if (groups.TryGetValue(j, out groupCount))
                                {
                                    for (var c = 0; c < groupCount; c++, j++)
                                    {
                                        var arr = builder.Properties[j].GetValue(new TEntity()) as Array;

                                        for (var k = 0; k < arr.Length; k++)
                                            arr.SetValue(data.Rows[i][j + (k * groupCount)], k);

                                        builder.PropertySetter[j].SetValue(entities[i], arr);
                                    }
                                }
                                else
                                {
                                    var arr = builder.Properties[j].GetValue(new TEntity()) as Array;

                                    for (var k = 0; k < arr.Length; k++)
                                        arr.SetValue(data.Rows[i][j + k], k);

                                    builder.PropertySetter[j].SetValue(entities[i], arr);
                                }
                            }
                        }

                        // TODO Fix group assignment in foreign keys.
                        if (groups.Count == 0)
                            parentDb.AssignForeignKeyData(entities[i], foreignKeys, groups);

                        entities[i].InitializeNonTableProperties();
                    }
                });
            }
            else
            {
                Parallel.ForEach(datapPartitioner, (dataRange, loopState) =>
                {
                    for (var i = dataRange.Item1; i < dataRange.Item2; i++)
                    {
                        entities[i] = new TEntity();

                        for (var j = 0; j < fieldCount; j++)
                        {
                            if (!builder.Properties[j].PropertyType.IsArray)
                            {
                                if (builder.Properties[j].PropertyType.IsCustomClass())
                                {
                                    var instanceFields = builder.Properties[j].PropertyType.GetReadWriteProperties();
                                    var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                                    for (var f = 0; f < instanceFields.Length; f++)
                                        instanceFields[f].SetValue(instance, Convert.IsDBNull(data.Rows[i][j + f]) ? "" : data.Rows[i][j + f]);

                                    builder.PropertySetter[j].SetValue(entities[i], instance);
                                }
                                else if (builder.Properties[j].PropertyType.IsCustomStruct())
                                {
                                    var instanceFields = builder.Properties[j].PropertyType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();
                                    var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                                    for (var f = 0; f < instanceFields.Length; f++)
                                        instanceFields[f].SetValue(instance, Convert.IsDBNull(data.Rows[i][j + f]) ? "" : data.Rows[i][j + f].ChangeTypeGet(builder.Properties[j + f].PropertyType));

                                    builder.PropertySetter[j].SetValue(entities[i], instance);
                                }
                                else
                                    builder.PropertySetter[j].SetValue(entities[i], Convert.IsDBNull(data.Rows[i][j]) ? "" : data.Rows[i][j].ChangeTypeGet(builder.Properties[j].PropertyType));
                            }
                            else
                            {
                                var groupCount = 0;

                                if (groups.TryGetValue(j, out groupCount))
                                {
                                    for (var c = 0; c < groupCount; c++, j++)
                                    {
                                        var arr = builder.Properties[j].GetValue(entities[i]) as Array;

                                        for (var k = 0; k < arr.Length; k++)
                                            arr.SetValue(data.Rows[i][j + (k * groupCount)], k);

                                        builder.PropertySetter[j].SetValue(entities[i], arr);
                                    }
                                }
                                else
                                {
                                    var arr = builder.Properties[j].GetValue(entities[i]) as Array;

                                    for (var k = 0; k < arr.Length; k++)
                                        arr.SetValue(data.Rows[i][j + k], k);

                                    builder.PropertySetter[j].SetValue(entities[i], arr);
                                }
                            }
                        }

                        entities[i].InitializeNonTableProperties();
                    }
                });
            }

            return entities;
        }

        public List<T> GetEntityList<T>(DataTable data, QueryBuilder<T> builder) where T : Entity, new()
        {
            return CreateEntities(data, builder).ToList();
        }

        public Dictionary<TKey, TValue> GetEntityDictionary<TKey, TValue>(DataTable data, QueryBuilder<TValue> builder, Func<TValue, TKey> func) where TValue : Entity, new()
        {
            return CreateEntities(data, builder).AsDictionary(func);
        }
    }
}
