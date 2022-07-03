// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    public partial class Database<T>
    {
        public ValueTask<int> GetAutoIncrementValue<TEntity>() where TEntity : IEntity, new()
        {
            throw new NotImplementedException($"GetAutoIncrementValue not implemented for {Connector.Settings.DatabaseType}.");
        }

        public ValueTask<bool> Exists<TEntity>() where TEntity : IEntity, new()
        {
            throw new NotImplementedException($"Exists not implemented for {Connector.Settings.DatabaseType}.");
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
