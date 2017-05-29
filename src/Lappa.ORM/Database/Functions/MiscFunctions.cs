// Copyright (C) Arctium Software.
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
        public TReturn GetAutoIncrementValue<TEntity, TReturn>() => GetAutoIncrementValueAsync<TEntity, TReturn>().GetAwaiter().GetResult();

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<TReturn> GetAutoIncrementValueAsync<TEntity, TReturn>()
        {
            var tableName = Pluralize<TEntity>();
            var result = await SelectAsync($"SHOW TABLE STATUS LIKE ?", tableName);

            if (!await result.ReadAsync())
            {
                Log.Message(LogTypes.Warning, $"Can't get auto increment value for '{tableName}' table.");

                return default(TReturn);
            }

            return result["Auto_increment"].ChangeTypeGet<TReturn>();
        }

        public bool Exists<TEntity>() => ExistsAsync<TEntity>().GetAwaiter().GetResult();

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<bool> ExistsAsync<TEntity>()
        {
            var tableName = Pluralize<TEntity>();

            using (var connection = await CreateConnectionAsync())
            {
                var result = await SelectAsync("SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = ? AND table_name = ?", connection.Database, tableName);

                if (!result.Read())
                {
                    Log.Message(LogTypes.Warning, $"Can't check if '{tableName}' table exists, no schema info.");

                    return false;
                }

                return Convert.ToBoolean(result["ct"]);
            }
        }
    }
}
