// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    public partial class Database
    {
        #region Add
        /// <summary>
        /// Adds the given entity to the database.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entity">The TEntity object.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Add<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var values = new Dictionary<string, object>(properties.Length);
            var query = new QueryBuilder<TEntity>(querySettings, properties);

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType.IsArray)
                {
                    var arr = (query.PropertyGetter[i].GetValue(entity) as Array);
                    var arrElementType = arr.GetType().GetElementType();

                    for (var j = 0; j <= arr.Length; j++)
                        values.Add(properties[i].Name + j, arr.GetValue(j).ChangeTypeGet(arrElementType));
                }
                else if (!properties[i].HasAttribute<AutoIncrementAttribute>())
                {
                    values.Add(properties[i].Name, query.PropertyGetter[i].GetValue(entity));
                }
            }

            return Execute(query.BuildInsert(values));
        }

        /// <summary>
        /// Adds a 'list' of given entities to the database.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entities">A 'list' of TEntity objects.</param>
        public void Add<TEntity>(TEntity[] entities) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var query = new QueryBuilder<TEntity>(querySettings, properties);
            var queries = query.BuildBulkInsert(properties, entities);

            for (var i = 0; i < queries.Count; i++)
                Execute(queries[i]);
        }

        /// <summary>
        /// <see cref="Add{TEntity}(TEntity[])"/>
        /// </summary>
        public void Add<TEntity>(IEnumerable<TEntity> entities) where TEntity : Entity, new()
        {
            Add(entities.ToArray());
        }

        /// <summary>
        /// <see cref="Add{TEntity}(TEntity[])"/>
        /// </summary>
        public void Add<TEntity>(List<TEntity> entities) where TEntity : Entity, new()
        {
            Add(entities.ToArray());
        }
        #endregion

        #region Update
        /// <summary>
        /// Updates the database with the given entity using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The updated object of type TEntity</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Update<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var type = typeof(TEntity);
            var properties = type.GetReadWriteProperties();
            var primaryKeys = type.GetProperties().Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.Name == "Id" || p.Name == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>(querySettings);
            var query = builder.BuildUpdate(entity, properties, primaryKeys);

            return Execute(query);
        }

        /// <summary>
        /// Updates the database with the given properties using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="setExpressions">The properties to be updated using the <see cref="PublicExtensions.Set{T}(T,T)"/> extension method.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool UpdateAll<TEntity>(params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(querySettings);
            var expressions = from c in setExpressions select ((c.Body as UnaryExpression)?.Operand as MethodCallExpression) ?? c.Body as MethodCallExpression;
            var query = builder.BuildUpdate(expressions.ToArray(), false);

            return Execute(query);
        }

        /// <summary>
        /// Updates the database with the given properties using the given conditions.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="condition">The used condition to identify the to be updated entity.</param>
        /// <param name="setExpressions">The properties to be updated using the <see cref="PublicExtensions.Set{T}(T,T)"/> extension method.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Update<TEntity>(Expression<Func<TEntity, bool>> condition, params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(querySettings);

            var expressions = from c in setExpressions select ((c.Body as UnaryExpression)?.Operand as MethodCallExpression) ?? c.Body as MethodCallExpression;
            var query = builder.BuildUpdate(expressions.ToArray(), true);

            query = builder.BuildUpdate(condition);

            return Execute(query);
        }
        #endregion

        #region Delete
        /// <summary>
        /// Deletes an entity from the database using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entity">The to be deleted entity</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Delete<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var type = typeof(TEntity);
            var primaryKeys = type.GetProperties().Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.Name == "Id" || p.Name == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>(querySettings);
            var query = builder.BuildDelete(entity, primaryKeys);

            return Execute(query);
        }

        /// <summary>
        /// Deletes one or multiple entities from the database using the given conditions.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="condition">The used condition to identify the to be updated entity.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public bool Delete<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>(querySettings);
            var query = builder.BuildDelete(condition.Body);

            return Execute(query);
        }
        #endregion
    }
}
