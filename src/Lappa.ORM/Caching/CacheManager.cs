// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Lappa.ORM.Misc;

namespace Lappa.ORM.Caching
{
    internal class CacheManager : Singleton<CacheManager>
    {
        readonly ConcurrentDictionary<MemberInfo, DBFieldAttribute> dbFieldCache;
        readonly Dictionary<string, IQueryBuilder> queryBuilderCache;

        CacheManager()
        {
            dbFieldCache = new ConcurrentDictionary<MemberInfo, DBFieldAttribute>();
            queryBuilderCache = new Dictionary<string, IQueryBuilder>();

            CacheDBFieldAttributes();
        }

        void CacheDBFieldAttributes()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var entityTypes = asm.DefinedTypes.Where(t => t.IsSubclassOf(typeof(Entity)));

                    foreach (var t in entityTypes)
                    {
                        foreach (var p in t.GetTypeInfo().DeclaredProperties)
                        {
                            var dbFieldAttribute = p.GetCustomAttribute<DBFieldAttribute>();

                            if (dbFieldAttribute != null)
                            {
                                // Use the property name if no DBField name is set.
                                if (string.IsNullOrEmpty(dbFieldAttribute.Name))
                                    dbFieldAttribute.Name = p.Name;

                                dbFieldCache.TryAdd(p, dbFieldAttribute);
                            }
                            else
                            {
                                // Also add a default DBFieldAttribute for all properties.
                                dbFieldCache.TryAdd(p, p.GetCustomAttribute<DBFieldAttribute>() ?? new DBFieldAttribute { Name = p.Name });
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Just swallow the exception. No special handling required.
                }
            }
        }

        public void CacheQueryBuilders(Connector connector)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var entityTypes = asm.DefinedTypes.Where(t => t.IsSubclassOf(typeof(Entity)));

                    foreach (var t in entityTypes)
                    {
                        var builderType = typeof(QueryBuilder<>).MakeGenericType(t);
                        var parameters = new object[] { connector.Query, t.GetReadWriteProperties(), null };
                        var builderInstance = Activator.CreateInstance(builderType, BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, CultureInfo.InvariantCulture);

                        queryBuilderCache.Add(t.Name, builderInstance as IQueryBuilder);
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Just swallow the exception. No special handling required.
                }
            }
        }

        public DBFieldAttribute GetDBField(MemberInfo memberInfo)
        {
            if (dbFieldCache.TryGetValue(memberInfo, out var dbFieldAttribute))
                return dbFieldAttribute;

            return new DBFieldAttribute { Name = memberInfo.Name };
        }

        public IQueryBuilder GetQueryBuilder(string entityName)
        {
            queryBuilderCache.TryGetValue(entityName, out var queryBuilder);

            return queryBuilder;
        }
    }
}
