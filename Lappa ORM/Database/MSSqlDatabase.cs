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

namespace Lappa_ORM
{
    internal class MSSqlDatabase : IDatabase
    {
        Database parentDb;

        public MSSqlDatabase(Database parent)
        {
            parentDb = parent;
        }

        public T[] CreateEntities<T>(DataTable data, QueryBuilder<T> builder) where T : Entity, new()
        {
            var fieldCount = builder.PropertySetter.Length;
            var arrayFieldCount = 0;
            var classFieldCount = 0;
            var structFieldCount = 0;

            if (data == null || data.Rows.Count == 0)
                return new T[0];

            var entity = new T();

            for (var i = 0; i < builder.Properties.Length; i++)
            {
                if (builder.Properties[i].PropertyType.IsArray)
                {
                    var arr = builder.Properties[i].GetValue(new T()) as Array;

                    arrayFieldCount = arr.Length - 1;
                }

                if (builder.Properties[i].PropertyType.IsClass)
                    classFieldCount += builder.Properties[i].PropertyType.GetReadWriteProperties().Length - 1;

                if (builder.Properties[i].PropertyType.IsStruct())
                    structFieldCount += builder.Properties[i].PropertyType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length - 1;
            }

            if (data.Columns.Count != (fieldCount + arrayFieldCount + classFieldCount + structFieldCount))
            {
                Trace.TraceError(string.Format("Table '{0}' (Column/Property count mismatch)", typeof(T).Name.Pluralize()));
                Trace.TraceError(string.Format("Columns '{0}'", data.Columns.Count));
                Trace.TraceError(string.Format("Properties '{0}'", fieldCount + arrayFieldCount + classFieldCount + structFieldCount));
                Trace.WriteLine("Press a key to continue loading.");

                Console.ReadKey(true);

                return new T[0];
            }

            var entities = new T[data.Rows.Count];
            var datapPartitioner = Partitioner.Create(0, data.Rows.Count);

            // Create one test object for foreign key assignment check
            var foreignKeys = typeof(T).GetProperties().Where(p => p.GetMethod.IsVirtual).ToArray();

            if (entity.AutoAssignForeignKeys && foreignKeys.Length > 0)
            {
                // TODO: Optimize foreign key assignment (slow atm...)
                Parallel.ForEach(datapPartitioner, (dataRange, loopState) =>
                {
                    for (int i = dataRange.Item1; i < dataRange.Item2; i++)
                    {
                        entities[i] = new T();

                        for (var j = 0; j < fieldCount; j++)
                        {
                            if (!builder.Properties[j].PropertyType.IsArray)
                            {
                                if (builder.Properties[j].PropertyType.IsClass)
                                {
                                    var instanceFields = builder.Properties[j].PropertyType.GetReadWriteProperties();
                                    var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                                    for (var f = 0; f < instanceFields.Length; f++)
                                        instanceFields[f].SetValue(instance, Convert.IsDBNull(data.Rows[i][j + f]) ? "" : data.Rows[i][j + f].ChangeTypeGet(builder.Properties[j + f].PropertyType));

                                    builder.PropertySetter[j].SetValue(entities[i], instance);
                                }
                                else if (builder.Properties[j].PropertyType.IsStruct())
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
                                var arr = builder.Properties[j].GetValue(new T()) as Array;
                                var elementType = arr.GetType().GetElementType();

                                for (var k = 0; k < arrayFieldCount + 1; k++)
                                    arr.SetValue(data.Rows[i][j + k].ChangeTypeGet(elementType), k);

                                builder.PropertySetter[j].SetValue(entities[i], arr);
                            }
                        }

                        parentDb.AssignForeignKeyData(entities[i], foreignKeys, null);

                        entities[i].InitializeNonTableProperties();
                    }
                });
            }
            else
            {
                Parallel.ForEach(datapPartitioner, (dataRange, loopState) =>
                {
                    for (int i = dataRange.Item1; i < dataRange.Item2; i++)
                    {
                        entities[i] = new T();

                        for (var j = 0; j < fieldCount; j++)
                        {
                            if (!builder.Properties[j].PropertyType.IsArray)
                            {
                                if (builder.Properties[j].PropertyType.IsClass)
                                {
                                    var instanceFields = builder.Properties[j].PropertyType.GetReadWriteProperties();
                                    var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                                    for (var f = 0; f < instanceFields.Length; f++)
                                        instanceFields[f].SetValue(instance, Convert.IsDBNull(data.Rows[i][j + f]) ? "" : data.Rows[i][j + f]);

                                    builder.PropertySetter[j].SetValue(entities[i], instance);
                                }
                                else if (builder.Properties[j].PropertyType.IsStruct())
                                {
                                    var instanceFields = builder.Properties[j].PropertyType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();
                                    var instance = Activator.CreateInstance(builder.Properties[j].PropertyType);

                                    for (var f = 0; f < instanceFields.Length; f++)
                                        instanceFields[f].SetValue(instance, Convert.IsDBNull(data.Rows[i][j + f]) ? "" : data.Rows[i][j + f]);

                                    builder.PropertySetter[j].SetValue(entities[i], instance);
                                }
                                else
                                    builder.PropertySetter[j].SetValue(entities[i], Convert.IsDBNull(data.Rows[i][j]) ? "" : data.Rows[i][j].ChangeTypeGet(builder.Properties[j].PropertyType));
                            }
                            else
                            {
                                var arr = builder.Properties[j].GetValue(new T()) as Array;
                                var elementType = arr.GetType().GetElementType();

                                for (var k = 0; k < arrayFieldCount + 1; k++)
                                    arr.SetValue(data.Rows[i][j + k].ChangeTypeGet(elementType), k);

                                builder.PropertySetter[j].SetValue(entities[i], arr);
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

        // Code duplication needed!
        public Dictionary<TKey, TValue> GetEntityDictionary<TKey, TValue>(DataTable data, QueryBuilder<TValue> builder, Func<TValue, TKey> func) where TValue : Entity, new()
        {
            return CreateEntities(data, builder).AsDictionary(func);
        }
    }
}
