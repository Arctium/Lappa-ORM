// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    public partial class Database
    {
        #region Insert
        /// <summary>
        /// Inserts the given entity to the database.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entity">The TEntity object.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Insert<TEntity>(TEntity entity) where TEntity : Entity, new() => InsertAsync(entity).GetAwaiter().GetResult();

        public async Task<bool> InsertAsync<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var values = new Dictionary<string, object>(properties.Length);
            var query = new QueryBuilder<TEntity>(connectorQuery, properties);

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType.IsArray)
                {
                    var arr = (query.PropertyGetter[i](entity) as Array);
                    var arrElementType = arr.GetType().GetElementType();

                    for (var j = 0; j < arr.Length; j++)
                        values.Add(properties[i].GetName() + j, arr.GetValue(j).ChangeTypeGet(arrElementType));
                }
                else if (!properties[i].HasAttribute<AutoIncrementAttribute>())
                {
                    values.Add(properties[i].GetName(), query.PropertyGetter[i](entity));
                }
            }

            return await ExecuteAsync(query.BuildInsert(values));
        }

        /// <summary>
        /// Inserts a 'list' of given entities to the database.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entities">A 'list' of TEntity objects.</param>
        public bool Insert<TEntity>(TEntity[] entities) where TEntity : Entity, new() => InsertAsync(entities).GetAwaiter().GetResult();

        public async Task<bool> InsertAsync<TEntity>(TEntity[] entities) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var query = new QueryBuilder<TEntity>(connectorQuery, properties);
            var queries = query.BuildBulkInsert(properties, entities);

            for (var i = 0; i < queries.Count; i++)
                if (!await ExecuteAsync(queries[i]))
                    return false;

            return true;
        }

        /// <summary>
        /// <see cref="Add{TEntity}(TEntity[])"/>
        /// </summary>
        public void Insert<TEntity>(IEnumerable<TEntity> entities) where TEntity : Entity, new()
        {
            Insert(entities.ToArray());
        }

        /// <summary>
        /// <see cref="Add{TEntity}(TEntity[])"/>
        /// </summary>
        public void Insert<TEntity>(List<TEntity> entities) where TEntity : Entity, new()
        {
            Insert(entities.ToArray());
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates the database with the given entity using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The updated object of type TEntity</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Update<TEntity>(TEntity entity) where TEntity : Entity, new() => UpdateAsync(entity).GetAwaiter().GetResult();

        public async Task<bool> UpdateAsync<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var type = typeof(TEntity);
            var properties = type.GetReadWriteProperties();
            var primaryKeys = type.GetTypeInfo().DeclaredProperties.Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.GetName() == "Id" || p.GetName() == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>(connectorQuery);
            var query = builder.BuildUpdate(entity, properties, primaryKeys);

            return await ExecuteAsync(query);
        }

        /// <summary>
        /// Updates the database with the given properties using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="setExpressions">The properties to be updated using the <see cref="PublicExtensions.Set{T}(T,T)"/> extension method.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool UpdateAll<TEntity>(params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new() => UpdateAllAsync(setExpressions).GetAwaiter().GetResult();

        public async Task<bool> UpdateAllAsync<TEntity>(params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(connectorQuery);
            var expressions = from c in setExpressions select ((c.Body as UnaryExpression)?.Operand as MethodCallExpression) ?? c.Body as MethodCallExpression;
            var query = builder.BuildUpdate(expressions.ToArray(), false);

            return await ExecuteAsync(query);
        }

        /// <summary>
        /// Updates the database with the given properties using the given conditions.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="condition">The used condition to identify the to be updated entity.</param>
        /// <param name="setExpressions">The properties to be updated using the <see cref="PublicExtensions.Set{T}(T,T)"/> extension method.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Update<TEntity>(Expression<Func<TEntity, bool>> condition, params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new() => UpdateAsync(condition, setExpressions).GetAwaiter().GetResult();

        public async Task<bool> UpdateAsync<TEntity>(Expression<Func<TEntity, bool>> condition, params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(connectorQuery);

            var expressions = from c in setExpressions select ((c.Body as UnaryExpression)?.Operand as MethodCallExpression) ?? c.Body as MethodCallExpression;
            var query = builder.BuildUpdate(expressions.ToArray(), true);

            query = builder.BuildUpdate(condition);

            return await ExecuteAsync(query);
        }
        #endregion

        #region Delete
        /// <summary>
        /// Deletes an entity from the database using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entity">The to be deleted entity</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Delete<TEntity>(TEntity entity) where TEntity : Entity, new() => DeleteAsync(entity).GetAwaiter().GetResult();

        public async Task<bool> DeleteAsync<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var type = typeof(TEntity);
            var primaryKeys = type.GetTypeInfo().DeclaredProperties.Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.GetName() == "Id" || p.GetName() == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>(connectorQuery);
            var query = builder.BuildDelete(entity, primaryKeys);

            return await ExecuteAsync(query);
        }

        /// <summary>
        /// Deletes one or multiple entities from the database using the given conditions.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="condition">The used condition to identify the to be updated entity.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Delete<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new() => DeleteAsync(condition).GetAwaiter().GetResult();

        public async Task<bool> DeleteAsync<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(connectorQuery);
            var query = builder.BuildDelete(condition.Body);

            return await ExecuteAsync(query);
        }
        #endregion
    }
}
