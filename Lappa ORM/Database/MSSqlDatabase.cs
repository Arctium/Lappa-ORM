// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
            var arrayFieldCount = 0;
            var fieldCount = builder.PropertySetter.Length;

            if (data == null || data.Rows.Count == 0)
                return new T[0];

            var entity = new T();

            for (var i = 0; i < builder.Properties.Length; i++)
            {
                if (builder.Properties[i].PropertyType.IsArray)
                {
                    var arr = builder.Properties[i].GetValue(Activator.CreateInstance(typeof(T))) as Array;

                    arrayFieldCount = arr.Length - 1;
                }
            }

            if (data.Columns.Count != (fieldCount + arrayFieldCount))
            {
                Trace.TraceError(string.Format("Table '{0}' (Column/Property count mismatch)", typeof(T).Name.Pluralize()));
                Trace.TraceError(string.Format("Columns '{0}'", data.Columns.Count));
                Trace.TraceError(string.Format("Properties '{0}'", fieldCount + arrayFieldCount));
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
                                builder.PropertySetter[j].SetValue(entities[i], Convert.IsDBNull(data.Rows[i][j]) ? "" : data.Rows[i][j].ChangeType(builder.Properties[j].PropertyType));
                            else
                            {
                                var arr = builder.Properties[j].GetValue(Activator.CreateInstance(typeof(T))) as Array;
                                var elementType = arr.GetType().GetElementType();

                                for (var k = 0; k < arrayFieldCount + 1; k++)
                                    arr.SetValue(data.Rows[i][j + k].ChangeType(elementType), k);

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
                                builder.PropertySetter[j].SetValue(entities[i], Convert.IsDBNull(data.Rows[i][j]) ? "" : data.Rows[i][j].ChangeType(builder.Properties[j].PropertyType));
                            else
                            {
                                var arr = builder.Properties[j].GetValue(Activator.CreateInstance(typeof(T))) as Array;
                                var elementType = arr.GetType().GetElementType();

                                for (var k = 0; k < arrayFieldCount + 1; k++)
                                    arr.SetValue(data.Rows[i][j + k].ChangeType(elementType), k);

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
