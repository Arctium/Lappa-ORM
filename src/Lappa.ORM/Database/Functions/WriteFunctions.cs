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
    public partial class Database<T>
    {
        /// <summary>
        /// Inserts the given entity to the database.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entity">The TEntity object.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public async ValueTask<bool> Insert<TEntity>(TEntity entity) where TEntity : IEntity, new()
        {
            var properties = typeof(TEntity).GetReadWriteProperties();
            var values = new Dictionary<string, (object ColumnValue, bool IsArrayGroup)>(properties.Length);
            var builder = new QueryBuilder<TEntity>(Connector.Query, properties);

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType.IsArray && properties[i].HasAttribute<GroupAttribute>())
                {
                    var arr = (builder.PropertyGetter[i](entity) as Array);
                    var arrElementType = arr.GetType().GetElementType();

                    for (var j = 0; j < arr.Length; j++)
                        values.Add(properties[i].GetName() + j, (arr.GetValue(j).ChangeTypeGet(arrElementType), true));
                }
                else if (!properties[i].HasAttribute<AutoIncrementAttribute>())
                {
                    values.Add(properties[i].GetName(), (builder.PropertyGetter[i](entity), false));
                }
            }

            builder.BuildInsert(values);

            return (await Execute(builder)) > 0;
        }

        /// <summary>
        /// Updates the database with the given entity using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The updated object of type TEntity</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public ValueTask<int> Update<TEntity>(TEntity entity) where TEntity : IEntity, new()
        {
            var type = typeof(TEntity);
            var properties = type.GetReadWriteProperties();
            var primaryKeys = type.GetTypeInfo().DeclaredProperties.Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.GetName() == "Id" || p.GetName() == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>(Connector.Query);

            builder.BuildUpdate(entity, properties, primaryKeys);

            return Execute(builder);
        }

        /// <summary>
        /// Updates the database with the given properties using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="setExpressions">The properties to be updated using the <see cref="PublicExtensions.Set{T}(T,T)"/> extension method.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public ValueTask<int> UpdateAll<TEntity>(params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : IEntity, new()
        {
            var builder = new QueryBuilder<TEntity>(Connector.Query);
            var expressions = from c in setExpressions select ((c.Body as UnaryExpression)?.Operand as MethodCallExpression) ?? c.Body as MethodCallExpression;

            builder.BuildUpdate(expressions.ToArray(), null);

            return Execute(builder);
        }

        /// <summary>
        /// Updates the database with the given properties using the given conditions.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="condition">The used condition to identify the to be updated entity.</param>
        /// <param name="setExpressions">The properties to be updated using the <see cref="PublicExtensions.Set{T}(T,T)"/> extension method.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public ValueTask<int> Update<TEntity>(Expression<Func<TEntity, bool>> condition, params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : IEntity, new()
        {
            var builder = new QueryBuilder<TEntity>(Connector.Query);
            var expressions = from c in setExpressions select ((c.Body as UnaryExpression)?.Operand as MethodCallExpression) ?? c.Body as MethodCallExpression;

            builder.BuildUpdate(expressions.ToArray(), condition);

            return Execute(builder);
        }

        /// <summary>
        /// Deletes an entity from the database using it's primary keys as condition.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="entity">The to be deleted entity</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public ValueTask<int> Delete<TEntity>(TEntity entity) where TEntity : IEntity, new()
        {
            var type = typeof(TEntity);
            var primaryKeys = type.GetTypeInfo().DeclaredProperties.Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.GetName() == "Id" || p.GetName() == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>(Connector.Query);

            builder.BuildDelete(entity, primaryKeys);

            return Execute(builder);
        }

        /// <summary>
        /// Deletes one or multiple entities from the database using the given conditions.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="condition">The used condition to identify the to be updated entity.</param>
        /// <returns>True if the SQL query execution is successful.</returns>
        public ValueTask<int> Delete<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : IEntity, new()
        {
            var builder = new QueryBuilder<TEntity>(Connector.Query);

            builder.BuildDelete(condition.Body);

            return Execute(builder);
        }
    }
}
