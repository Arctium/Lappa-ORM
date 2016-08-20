// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            var tableName = Pluralize<TEntity>();
            var result = Select($"SHOW TABLE STATUS LIKE '{tableName}';", tableName);

            if (result.Read())
                return result["Auto_increment"].ChangeTypeGet<TReturn>();

            return default(TReturn);
        }

        // MySQL only.
        public bool Exists<TEntity>()
        {
            var tableName = Pluralize<TEntity>();

            DbDataReader result;

            using (var connection = CreateConnection())
                result = Select(string.Format("SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = '{0}' AND table_name = '{1}'"), connection.Database, tableName, tableName);

            return result.Read() ? result.GetInt32(0) == 1 : false;
        }
    }
}
