// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<TEntity[]> SelectAsync<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties, members);

            using (var dataReader = await SelectAsync(members != null ? builder.BuildSelect(members) : builder.BuildSelectAll()))
                return entityBuilder.CreateEntities(dataReader, builder);
        }

        public Dictionary<TKey, TEntity> Select<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return RunSync(() => SelectAsync(func, newExpression));
        }

        public async Task<Dictionary<TKey, TEntity>> SelectAsync<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return (await SelectAsync(newExpression)).AsDictionary(func);
        }
        #endregion

        #region SelectWhere
        public TEntity[] Where<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return RunSync(() => WhereAsync(condition, newExpression));
        }

        public async Task<TEntity[]> WhereAsync<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var query = "";
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties, members);

            if (members != null)
                query = builder.BuildWhere(condition.Body, members);
            else
                query = builder.BuildWhereAll(condition.Body);

            using (var dataReader = await SelectAsync(query))
                return entityBuilder.CreateEntities(dataReader, builder);
        }
        #endregion

        #region Single
        public TEntity Single<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return RunSync(() => SingleAsync(condition, newExpression));
        }

        public async Task<TEntity> SingleAsync<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var query = "";
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties, members);

            if (members != null)
                query = builder.BuildWhere(condition.Body, members);
            else
                query = builder.BuildWhereAll(condition.Body);

            using (var dataReader = await SelectAsync(query))
            {
                if (dataReader != null)
                {
                    var objList = entityBuilder.CreateEntities(dataReader, builder);

                    if (objList.Length > 1)
                        Log.Message(LogTypes.Warning, "Result contains more than 1 element.");

                    return objList.Length == 0 ? null : objList[0];
                }
            }

            return null;
        }
        #endregion

        #region Other
        public bool Any<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            return RunSync(() => AnyAsync(condition));
        }

        public async Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(connectorQuery);
            var query = builder.BuildWhereAll(condition.Body);

            using (var dataReader = await SelectAsync(query))
                return dataReader.HasRows;
        }

        public long Count<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            return RunSync(() => CountAsync(condition));
        }

        public async Task<long> CountAsync<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties);
            var query = condition != null ? builder.BuildWhereCount(condition.Body) : builder.BuildSelectCount();

            using (var dataReader = await SelectAsync(query))
            {
                // Read the first row.
                await dataReader?.ReadAsync();

                // Return -1 if row data are null.
                return Convert.ToInt64(dataReader?[0] ?? -1);
            }
        }
        #endregion
    }
}
