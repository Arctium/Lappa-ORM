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
    public partial class Database<T>
    {
        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async ValueTask<int> GetAutoIncrementValue<TEntity>() where TEntity : IEntity, new()
        {
            if (Connector.Settings.DatabaseType != DatabaseType.MySql)
                throw new NotImplementedException($"GetAutoIncrementValue not implemented for {Connector.Settings.DatabaseType}.");

            // TODO: Fix for api mode.
            var tableInfo = await GetTableInfo(t => t.TableSchema == Connector.Settings.DatabaseName && t.TableName == Pluralize<TEntity>());

            return tableInfo?.AutoIncrement ?? -1;
        }

        // MySql only.
        // TODO: Fix for MSSql & SQLite
        public async ValueTask<bool> Exists<TEntity>() where TEntity : IEntity, new()
        {
            if (Connector.Settings.DatabaseType != DatabaseType.MySql)
                throw new NotImplementedException($"Exists not implemented for {Connector.Settings.DatabaseType}.");

            var tableInfo = await GetTableInfo(t => t.TableSchema == Connector.Settings.DatabaseName && t.TableName == Pluralize<TEntity>());

            return tableInfo != null;
        }

        async ValueTask<InformationSchemaTable> GetTableInfo(Expression<Func<InformationSchemaTable, object>> condition)
        {
            var properties = typeof(InformationSchemaTable).GetReadWriteProperties();
            var builder = new QueryBuilder<InformationSchemaTable>(Connector.Query, properties);

            builder.BuildWhereAll(condition.Body);

            // Add the database name for this query.
            builder.SqlQuery.Replace($"FROM `{builder.PluralizedEntityName}`", $"FROM `information_schema`.`tables`");

            var rowData = await Select(builder);
            var entityList = entityBuilder.CreateEntities(rowData, builder);

            return entityList.Length == 0 ? null : entityList[0];
        }
    }
}
