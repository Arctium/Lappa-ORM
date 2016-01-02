// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Lappa_ORM.Misc;
using static Lappa_ORM.Misc.Helper;

namespace Lappa_ORM
{
    public partial class Database
    {
        public TReturn GetAutoIncrementValue<TEntity, TReturn>()
        {
            var tableName = Pluralize<TEntity>();
            var data = Select($"SHOW TABLE STATUS LIKE '{tableName}';", tableName);

            if (data?.Rows.Count == 1)
                return data.Rows[0]["Auto_increment"].ChangeTypeGet<TReturn>();

            return default(TReturn);
        }

        public bool Exists<TEntity>()
        {
            var tableName = Pluralize<TEntity>();

            DataTable result;

            using (var connection = CreateConnection())
                result = Select(string.Format("SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = '{0}' AND table_name = '{1}'"), connection.Database, tableName, tableName);

            return (int)result?.Rows[0]["ct"] == 1;
        }
    }
}
