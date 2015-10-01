// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Lappa_ORM.Misc;
using static Lappa_ORM.Misc.Helper;

namespace Lappa_ORM
{
    public partial class Database
    {
        #region Select
        public TEntity[] SelectArray<TEntity>(Expression<Func<TEntity, object>> newExpression) where TEntity : Entity, new()
        {
            return Task.Run(() => SelectArrayAsync(newExpression)).Result;
        }

        public async Task<TEntity[]> SelectArrayAsync<TEntity>(Expression<Func<TEntity, object>> newExpression) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(querySettings, properties, members);
            var data = await SelectAsync(members != null ? builder.BuildSelect(members) : builder.BuildSelectAll(), Pluralize<TEntity>());

            return db.CreateEntities(data, builder);
        }

        public List<TEntity> Select<TEntity>(Expression<Func<TEntity, object>> newExpression) where TEntity : Entity, new()
        {
            return Task.Run(() => SelectAsync(newExpression)).Result;
        }

        public async Task<List<TEntity>> SelectAsync<TEntity>(Expression<Func<TEntity, object>> newExpression) where TEntity : Entity, new()
        {
            return (await SelectArrayAsync(newExpression)).ToList();
        }

        public Dictionary<TKey, TEntity> Select<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression) where TEntity : Entity, new()
        {
            return Task.Run(() => SelectAsync(func, newExpression)).Result;
        }

        public async Task<Dictionary<TKey, TEntity>> SelectAsync<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression) where TEntity : Entity, new()
        {
            return (await SelectArrayAsync(newExpression)).AsDictionary(func);
        }
        #endregion

        #region SelectWhere
        public List<TEntity> Where<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return Task.Run(() => WhereAsync(condition, newExpression)).Result;
        }

        public async Task<List<TEntity>> WhereAsync<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var query = "";
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(querySettings, properties, members);

            if (members != null)
                query = builder.BuildWhere(condition.Body, members);
            else
                query = builder.BuildWhereAll(condition.Body);

            var data = await SelectAsync(query, Pluralize<TEntity>());

            return db.CreateEntities(data, builder).ToList();
        }
        #endregion

        #region Single
        public TEntity Single<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return Task.Run(() => SingleAsync(condition, newExpression)).Result;
        }

        public async Task<TEntity> SingleAsync<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var query = "";
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(querySettings, properties, members);

            if (members != null)
                query = builder.BuildWhere(condition.Body, members);
            else
                query = builder.BuildWhereAll(condition.Body);

            var data = await SelectAsync(query, Pluralize<TEntity>());

            if (data != null)
            {
                if (data.Rows.Count > 1)
                    Trace.TraceWarning("Result contains more than 1 element.");

                var objList = db.CreateEntities(data, builder);

                return objList.Length == 0 ? null : objList[0];
            }

            return null;
        }
        #endregion

        #region Other
        public bool Any<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(querySettings);
            var query = builder.BuildWhereAll(condition.Body);

            return Select(query, Pluralize<TEntity>()).Rows.Count > 0;
        }
        #endregion
    }
}
