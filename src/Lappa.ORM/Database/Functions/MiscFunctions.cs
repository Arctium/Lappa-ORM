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
        public TReturn GetAutoIncrementValue<TEntity, TReturn>() where TEntity : Entity, new() => RunSync(() => GetAutoIncrementValueAsync<TEntity, TReturn>());

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<TReturn> GetAutoIncrementValueAsync<TEntity, TReturn>() where TEntity : Entity, new()
        {
            var tableName = Pluralize<TEntity>();
            var rowData = await SelectAsync($"SHOW TABLE STATUS LIKE ?", null, tableName);

            if (rowData.Length == 0)
            {
                Log.Message(LogTypes.Warning, $"Can't get auto increment value for '{tableName}' table.");

                return default(TReturn);
            }

            // Row: 0, Column: 10 (Auto_increment)
            return rowData[0][10].ChangeTypeGet<TReturn>();
        }

        public bool Exists<TEntity>()  where TEntity : Entity, new()=> RunSync(() => ExistsAsync<TEntity>());

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<bool> ExistsAsync<TEntity>() where TEntity : Entity, new()
        {
            var tableName = Pluralize<TEntity>();
            var rowData = await SelectAsync("SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = ? AND table_name = ?", null, Connector.Settings.DatabaseName, tableName);

            if (rowData.Length == 0)
            {
                Log.Message(LogTypes.Warning, $"Can't check if '{tableName}' table exists, no schema info.");

                return false;
            }

            // Row: 0, Column: 0 (ct)
            return Convert.ToBoolean(rowData[0][0]);
        }
    }
}
