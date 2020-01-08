// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Lappa.ORM.Logging;
using Lappa.ORM.Misc;
using static Lappa.ORM.Misc.Helper;

namespace Lappa.ORM
{
    public partial class Database
    {
        #region Select
        public TEntity[] Select<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return RunSync(() => SelectAsync(newExpression));
        }

        public async ValueTask<TEntity[]> SelectAsync<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(Connector.Query, properties, members);

            if (members != null)
                builder.BuildSelect(members);
            else
                builder.BuildSelectAll();

            var rowData = await SelectAsync(builder);

            return entityBuilder.CreateEntities(rowData, builder);
        }

        public Dictionary<TKey, TEntity> Select<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return RunSync(() => SelectAsync(func, newExpression));
        }

        public async ValueTask<Dictionary<TKey, TEntity>> SelectAsync<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return (await SelectAsync(newExpression)).AsDictionary(func);
        }
        #endregion

        #region SelectWhere
        public TEntity[] Where<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return RunSync(() => WhereAsync(condition, newExpression));
        }

        public async ValueTask<TEntity[]> WhereAsync<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(Connector.Query, properties, members);

            if (members != null)
                builder.BuildWhere(condition.Body, members);
            else
                builder.BuildWhereAll(condition.Body);

            var rowData = await SelectAsync(builder);

            return entityBuilder.CreateEntities(rowData, builder);
        }
        #endregion

        #region Single
        public TEntity Single<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return RunSync(() => SingleAsync(condition, newExpression));
        }

        public async ValueTask<TEntity> SingleAsync<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(Connector.Query, properties, members);

            if (members != null)
                builder.BuildWhere(condition.Body, members);
            else
                builder.BuildWhereAll(condition.Body);

            var rowData = await SelectAsync(builder);
            var objList = entityBuilder.CreateEntities(rowData, builder);

            if (objList.Length > 1)
                Log.Message(LogTypes.Warning, "Result contains more than 1 element.");

            return objList.Length == 0 ? null : objList[0];
        }
        #endregion

        #region Other
        public bool Any<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            return RunSync(() => AnyAsync(condition));
        }

        public async ValueTask<bool> AnyAsync<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(Connector.Query);

            builder.BuildWhereAll(condition.Body);

            var rowData = await SelectAsync(builder);

            return rowData[0]?.Length > 0;
        }

        public long Count<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            return RunSync(() => CountAsync(condition));
        }

        public async ValueTask<long> CountAsync<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var builder = new QueryBuilder<TEntity>(Connector.Query, properties);

            if (condition != null)
                builder.BuildWhereCount(condition.Body);
            else
                builder.BuildSelectCount();

            var rowData = await SelectAsync(builder);

            return Convert.ToInt64(rowData[0]?[0] ?? -1);
        }
        #endregion
    }
}
