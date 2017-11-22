// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Lappa.ORM.Logging;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    public partial class Database
    {
        #region Select
        public TEntity[] SelectArray<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return SelectArrayAsync(newExpression).GetAwaiter().GetResult();
        }

        public async Task<TEntity[]> SelectArrayAsync<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var members = (newExpression?.Body as NewExpression)?.Members;
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties, members);

            using (var dataReader = await SelectAsync(members != null ? builder.BuildSelect(members) : builder.BuildSelectAll()))
                return entityBuilder.CreateEntities(dataReader, builder);
        }

        public List<TEntity> Select<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return SelectAsync(newExpression).GetAwaiter().GetResult();
        }

        public async Task<List<TEntity>> SelectAsync<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return (await SelectArrayAsync(newExpression)).ToList();
        }

        public Dictionary<TKey, TEntity> Select<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return SelectAsync(func, newExpression).GetAwaiter().GetResult();
        }

        public async Task<Dictionary<TKey, TEntity>> SelectAsync<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return (await SelectArrayAsync(newExpression)).AsDictionary(func);
        }
        #endregion

        #region SelectWhere
        public List<TEntity> Where<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return WhereAsync(condition, newExpression).GetAwaiter().GetResult();
        }

        public async Task<List<TEntity>> WhereAsync<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
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
                return entityBuilder.CreateEntities(dataReader, builder).ToList();
        }
        #endregion

        #region Single
        public TEntity Single<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : Entity, new()
        {
            return SingleAsync(condition, newExpression).GetAwaiter().GetResult();
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
            return AnyAsync(condition).GetAwaiter().GetResult();
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
            return CountAsync(condition).GetAwaiter().GetResult();
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

        public long Max<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new() 
        {
            return MaxAsync(condition).GetAwaiter().GetResult();
        }

        public async Task<long> MaxAsync<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties);
            var query = condition != null ? builder.BuildWhereMax(condition.Body) : builder.BuildSelectMax();

            using (var dataReader = await SelectAsync(query))
            {
                // Read the first row.
                await dataReader?.ReadAsync();

                // Return -1 if row data are null.
                return Convert.ToInt64(dataReader?[0] ?? -1);
            }
        }

        public long Min<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            return MinAsync(condition).GetAwaiter().GetResult();
        }

        public async Task<long> MinAsync<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties);
            var query = condition != null ? builder.BuildWhereMin(condition.Body) : builder.BuildSelectMin();

            using (var dataReader = await SelectAsync(query))
            {
                // Read the first row.
                await dataReader?.ReadAsync();

                // Return -1 if row data are null.
                return Convert.ToInt64(dataReader?[0] ?? -1);
            }
        }

        public long Sum<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            return SumAsync(condition).GetAwaiter().GetResult();
        }

        public async Task<long> SumAsync<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var builder = new QueryBuilder<TEntity>(connectorQuery, properties);
            var query = condition != null ? builder.BuildWhereSum(condition.Body) : builder.BuildSelectSum();

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
