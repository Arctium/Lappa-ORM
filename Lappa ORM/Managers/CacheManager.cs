// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using LapapORM.Misc;

namespace LappaORM.Managers
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
            var entityTypes = Assembly.GetEntryAssembly().DefinedTypes.Where(t => t.IsSubclassOf(typeof(Entity)));

            foreach (var t in entityTypes)
            {
                foreach (var p in t.DeclaredProperties)
                {
                    dbFieldCache.TryAdd(p, p.GetCustomAttribute<DBFieldAttribute>() ?? new DBFieldAttribute { Name = p.Name });
                }
            }
        }

        public DBFieldAttribute GetDBField(MemberInfo memberInfo) => dbFieldCache[memberInfo];
    }
}
