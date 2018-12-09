// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Lappa.ORM.Misc;
using static Lappa.ORM.Misc.Helper;
using System.Linq.Expressions;
using Lappa.ORM.Constants;

namespace Lappa.ORM
{
    public partial class Database
    {
        public int GetAutoIncrementValue<TEntity>() where TEntity : Entity, new() => RunSync(() => GetAutoIncrementValueAsync<TEntity>());

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<int> GetAutoIncrementValueAsync<TEntity>() where TEntity : Entity, new()
        {
            if (Connector.Settings.DatabaseType != DatabaseType.MySql)
                throw new NotImplementedException($"GetAutoIncrementValue not implemented for {Connector.Settings.DatabaseType}.");

            Connector.Settings.DatabaseName = "AuthDB";

            var tableInfo = await GetTableInfoAsync(t => t.TableSchema == Connector.Settings.DatabaseName && t.TableName == Pluralize<TEntity>());

            return tableInfo?.AutoIncrement ?? -1;
        }

        public bool Exists<TEntity>() where TEntity : Entity, new()=> RunSync(() => ExistsAsync<TEntity>());

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async Task<bool> ExistsAsync<TEntity>() where TEntity : Entity, new()
        {
            if (Connector.Settings.DatabaseType != DatabaseType.MySql)
                throw new NotImplementedException($"Exists not implemented for {Connector.Settings.DatabaseType}.");

            var tableInfo = await GetTableInfoAsync(t => t.TableSchema == Connector.Settings.DatabaseName && t.TableName == Pluralize<TEntity>());

            return tableInfo != null;
        }

        public InformationSchemaTable GetTableInfo(Expression<Func<InformationSchemaTable, object>> condition) => RunSync(() => GetTableInfoAsync(condition));

        async Task<InformationSchemaTable> GetTableInfoAsync(Expression<Func<InformationSchemaTable, object>> condition)
        {
            var properties = typeof(InformationSchemaTable).GetReadWriteProperties();
            var builder = new QueryBuilder<InformationSchemaTable>(Connector.Query, properties);

            builder.BuildWhereAll(condition.Body);

            // Add the database name for this query.
            builder.SqlQuery.Replace($"FROM `{builder.PluralizedEntityName}`", $"FROM `information_schema`.`tables`");

            var rowData = await SelectAsync(builder);
            var entityList = entityBuilder.CreateEntities(rowData, builder);

            return entityList.Length == 0 ? null : entityList[0];
        }
    }
}
