// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Lappa.ORM.Misc;
using Microsoft.Extensions.Logging;

namespace Lappa.ORM
{
    public partial class Database<T>
    {
        public async ValueTask<TEntity[]> Select<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = QueryBuilder<TEntity>.GetOrCreate(Connector.Query, properties, members);
            StringBuilder sqlQuery;

            if (members != null)
                sqlQuery = builder.BuildSelect(members);
            else
                sqlQuery = builder.BuildSelectAll();

            var rowData = await Select(builder, (sqlQuery, []));

            return entityBuilder.CreateEntities(rowData, builder);
        }

        public async ValueTask<Dictionary<TKey, TEntity>> Select<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new()
        {
            return (await Select(newExpression)).AsDictionary(func);
        }

        public async ValueTask<TEntity[]> Where<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = QueryBuilder<TEntity>.GetOrCreate(Connector.Query, properties, members);
            (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) query;

            if (members != null)
                query = builder.BuildWhere(condition.Body, members);
            else
                query = builder.BuildWhereAll(condition.Body);

            var rowData = await Select(builder, query);

            return entityBuilder.CreateEntities(rowData, builder);
        }

        public async ValueTask<TEntity> Single<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = QueryBuilder<TEntity>.GetOrCreate(Connector.Query, properties, members);
            (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) query;

            if (members != null)
                query = builder.BuildWhere(condition.Body, members);
            else
                query = builder.BuildWhereAll(condition.Body);

            var rowData = await Select(builder, query);
            var objList = entityBuilder.CreateEntities(rowData, builder);

            if (objList.Length > 1)
                logger.Log(LogLevel.Warning, "Result contains more than 1 element.");

            return objList.Length == 0 ? default : objList[0];
        }

        public async ValueTask<bool> Any<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : IEntity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var builder = QueryBuilder<TEntity>.GetOrCreate(Connector.Query, properties, []);

            var query = builder.BuildWhereAll(condition.Body);
            var rowData = await Select(builder, query);

            return rowData[0]?.Length > 0;
        }

        public async ValueTask<long> Count<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : IEntity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var builder = QueryBuilder<TEntity>.GetOrCreate(Connector.Query, properties, []);
            (StringBuilder SqlQuery, Dictionary<string, object> SqlParameters) query;

            if (condition != null)
                query = builder.BuildWhereCount(condition.Body);
            else
                query = (builder.BuildSelectCount(), []);

            var rowData = await Select(builder, query);
            
            if (rowData[0].Length == 1)
                return Convert.ToInt64(rowData[0]?[0] ?? -0);

            return 0;
        }
    }
}
