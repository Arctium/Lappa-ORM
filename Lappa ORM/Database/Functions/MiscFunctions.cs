// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    public partial class Database
    {
        public TReturn GetAutoIncrementValue<TEntity, TReturn>()
        {
            var tableName = typeof(TEntity).Name.Pluralize();
            var data = Select($"SHOW TABLE STATUS LIKE '{tableName}';", tableName);

            if (data?.Rows.Count == 1)
                return data.Rows[0]["Auto_increment"].ChangeType<TReturn>();

            return default(TReturn);
        }

        public bool Exists<TEntity>()
        {
            var tableName = typeof(TEntity).Name.Pluralize();
            var data = Select($"SELECT COUNT(*) as ct FROM information_schema.tables WHERE table_schema = '{connection.Database}' AND table_name = '{tableName}'", tableName);

            return (int)data?.Rows[0]["ct"] == 1 ? true : false;
        }
    }
}
