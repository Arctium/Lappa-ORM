// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using LappaORM.Misc;
using static LappaORM.Misc.Helper;

namespace LappaORM
{
    public partial class Database
    {
        // MySQL only.
        public TReturn GetAutoIncrementValue<TEntity, TReturn>()
        {
            var result = Select($"SHOW TABLE STATUS LIKE ?", Pluralize<TEntity>());

            // Get the first result set.
            result.Read();

            return result["Auto_increment"].ChangeTypeGet<TReturn>();
        }

        // MySQL only.
        public bool Exists<TEntity>()
        {
            var tableName = Pluralize<TEntity>();

            DbDataReader result;

            using (var connection = CreateConnection())
                result = Select("SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = ? AND table_name = ?", connection.Database, tableName);

            // Get the first result set.
            result.Read();

            return Convert.ToBoolean(result["ct"]);
        }
    }
}
