// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using LappaORM.Logging;
using LappaORM.Misc;
using static LappaORM.Misc.Helper;

namespace LappaORM
{
    public partial class Database
    {
        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public TReturn GetAutoIncrementValue<TEntity, TReturn>()
        {
            var tableName = Pluralize<TEntity>();
            var result = Select($"SHOW TABLE STATUS LIKE ?", tableName);

            if (!result.Read())
            {
                Log.Message(LogTypes.Warning, $"Can't get auto increment value for '{tableName}' table.");

                return default(TReturn);
            }

            return result["Auto_increment"].ChangeTypeGet<TReturn>();
        }

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public bool Exists<TEntity>()
        {
            var tableName = Pluralize<TEntity>();

            using (var connection = CreateConnection())
            {
                var result = Select("SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = ? AND table_name = ?", connection.Database, tableName);

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
