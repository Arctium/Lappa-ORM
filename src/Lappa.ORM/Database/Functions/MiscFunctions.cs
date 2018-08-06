// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Lappa.ORM.Logging;
using Lappa.ORM.Misc;
using static Lappa.ORM.Misc.Helper;

namespace Lappa.ORM
{
    public partial class Database
    {
        public TReturn GetAutoIncrementValue<TEntity, TReturn>() => RunSync(() => GetAutoIncrementValueAsync<TEntity, TReturn>());

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<TReturn> GetAutoIncrementValueAsync<TEntity, TReturn>()
        {
            var tableName = Pluralize<TEntity>();

            using (var dataReader = await SelectAsync($"SHOW TABLE STATUS LIKE ?", tableName))
            {
                if (!await dataReader.ReadAsync())
                {
                    Log.Message(LogTypes.Warning, $"Can't get auto increment value for '{tableName}' table.");

                    return default(TReturn);
                }

                return dataReader["Auto_increment"].ChangeTypeGet<TReturn>();
            }
        }

        public bool Exists<TEntity>() => RunSync(() => ExistsAsync<TEntity>());

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<bool> ExistsAsync<TEntity>()
        {
            var tableName = Pluralize<TEntity>();

            using (var connection = await CreateConnectionAsync())
            using (var dataReader = await SelectAsync("SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = ? AND table_name = ?", connection.Database, tableName))
            {
                if (!await dataReader.ReadAsync())
                {
                    Log.Message(LogTypes.Warning, $"Can't check if '{tableName}' table exists, no schema info.");

                    return false;
                }

                return Convert.ToBoolean(dataReader["ct"]);
            }
        }
    }
}
