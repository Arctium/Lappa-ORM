// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using LapapORM.Misc;
using Microsoft.Extensions.DependencyModel;

namespace Lappa.ORM.Managers
{
    internal class CacheManager : Singleton<CacheManager>
    {
        readonly ConcurrentDictionary<MemberInfo, DBFieldAttribute> dbFieldCache;

        CacheManager()
        {
            dbFieldCache = new ConcurrentDictionary<MemberInfo, DBFieldAttribute>();

            CacheDBFieldAttributes();
        }

        void CacheDBFieldAttributes()
        {
            var assemblyNames = DependencyContext.Default.GetDefaultAssemblyNames();

            foreach (var asm in assemblyNames)
            {
                var entityTypes = Assembly.Load(asm).DefinedTypes.Where(t => t.IsSubclassOf(typeof(Entity)));

                foreach (var t in entityTypes)
                {
                    foreach (var p in t.DeclaredProperties)
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
        }

        public DBFieldAttribute GetDBField(MemberInfo memberInfo)
        {
            if (dbFieldCache.TryGetValue(memberInfo, out DBFieldAttribute dbFieldAttribute))
                return dbFieldAttribute;

            return new DBFieldAttribute { Name = memberInfo.Name };
        }
    }
}
