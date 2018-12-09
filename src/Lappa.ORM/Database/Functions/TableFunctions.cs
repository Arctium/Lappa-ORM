// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Lappa.ORM.Constants;
using Lappa.ORM.Misc;
using static Lappa.ORM.Misc.Helper;

namespace Lappa.ORM
{
    public partial class Database
    {
        public bool Create<TEntity>(MySqlEngine dbEngine = MySqlEngine.MyISAM, bool replaceTable = false) where TEntity : Entity, new()
        {
            return RunSync(() => CreateAsync<TEntity>(dbEngine, replaceTable));
        }

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<bool> CreateAsync<TEntity>(MySqlEngine dbEngine = MySqlEngine.MyISAM, bool replaceTable = false) where TEntity : Entity, new()
        {
            if (Connector.Settings.DatabaseType != DatabaseType.MySql)
                return false;

            // Check if table exists or is allowed to be replaced.
            if (!await ExistsAsync<TEntity>() || replaceTable)
            {
                // Exclude foreign key and non db related properties.
                var properties = typeof(TEntity).GetReadWriteProperties();
                var fields = new Dictionary<string, PropertyInfo>();
                var query = new QueryBuilder<TEntity>(Connector.Query, properties);
                var entity = new TEntity();

                // Key: GroupStartIndex, Value: GroupCount
                var groups = new ConcurrentDictionary<int, int>();
                var lastGroupName = "";
                var lastGroupStartIndex = 0;

                // Get Groups
                for (var i = 0; i < properties.Length; i++)
                {
                    var group = properties[i].GetCustomAttribute<GroupAttribute>();

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

                for (var i = 0; i < properties.Length; i++)
                {
                    var groupCount = 0;

                    if (!properties[i].PropertyType.IsArray)
                        fields.Add(properties[i].GetName(), properties[i]);
                    else
                    {
                        if (groups.TryGetValue(i, out groupCount))
                        {
                            var arr = properties[i].GetValue(Activator.CreateInstance(typeof(TEntity))) as Array;

                            for (var k = 1; k <= arr.Length; k++)
                            {
                                for (var j = 0; j < groupCount; j++)
                                {
                                    fields.Add(properties[i + j].GetName() + k, properties[i + j]);
                                }
                            }

                            i += groupCount - 1;
                        }
                        else
                        {
                            var arr = (query.PropertyGetter[i](entity) as Array);

                            for (var j = 1; j <= arr.Length; j++)
                                fields.Add(properties[i].GetName() + j, properties[i]);
                        }
                    }
                }

                return await ExecuteAsync(null);// query.BuildTableCreate(fields, dbEngine));
            }

            return false;
        }
    }
}
