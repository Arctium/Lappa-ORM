// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    public partial class Database
    {
        #region Add
        public bool Add<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetProperties().Where(p => !p.GetMethod.IsVirtual && p.GetSetMethod(false) != null).ToArray();
            var values = new Dictionary<string, object>(properties.Length);
            var query = new QueryBuilder<TEntity>(properties);

            for (var i = 0; i < properties.Length; i++)
            {
                if (properties[i].PropertyType.IsArray)
                {
                    var arr = (query.PropertyGetter[i].GetValue(entity) as Array);

                    for (var j = 0; j <= arr.Length; j++)
                        values.Add(properties[i].Name + j, arr.GetValue(j));
                }
                else if (!properties[i].HasAttribute<AutoIncrementAttribute>())
                {
                    values.Add(properties[i].Name, query.PropertyGetter[i].GetValue(entity));
                }
            }

            return Execute(query.BuildInsert(values));
        }

        public void Add<TEntity>(IEnumerable<TEntity> entities) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetProperties().Where(p => !p.GetMethod.IsVirtual && p.GetSetMethod(false) != null).ToArray();
            var query = new QueryBuilder<TEntity>(properties);
            var queries = query.BuildBulkInsert(properties, entities);

            for (var i = 0; i < queries.Count; i++)
                Execute(queries[i]);
        }

        // Code duplication of Add<TEntity>(IEnumerable<TEntity> entities)
        public void Add<TEntity>(List<TEntity> entities) where TEntity : Entity, new()
        {
            var properties = typeof(TEntity).GetProperties().Where(p => !p.GetMethod.IsVirtual && p.GetSetMethod(false) != null).ToArray();
            var query = new QueryBuilder<TEntity>(properties);
            var queries = query.BuildBulkInsert(properties, entities);

            for (var i = 0; i < queries.Count; i++)
                Execute(queries[i]);
        }
        #endregion
        #region Update
        public bool Update<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var type = typeof(TEntity);
            var properties = type.GetProperties().Where(p => !p.GetMethod.IsVirtual && p.GetSetMethod(false) != null).ToArray();
            var primaryKeys = type.GetProperties().Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.Name == "Id" || p.Name == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>();
            var query = builder.BuildUpdate(querySettings, entity, properties, primaryKeys);

            return Execute(query);
        }

        public bool Update<TEntity>(TEntity entity, params string[] fields) where TEntity : Entity, new()
        {
            var type = typeof(TEntity);
            var primaryKeys = type.GetProperties().Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.Name == "Id" || p.Name == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>(primaryKeys);
            var query = builder.BuildUpdate(querySettings, entity, primaryKeys, fields);

            return Execute(query);
        }

        public bool Update<TEntity>(Expression<Func<TEntity, object>> condition, TEntity entity, params string[] fields) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>();
            var query = builder.BuildUpdate(condition, querySettings, entity, condition.Parameters[0].Name, fields);

            return Execute(query);
        }

        public bool Update<TEntity>(params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>();
            var expressions = from c in setExpressions select ((c.Body as UnaryExpression).Operand as MethodCallExpression);
            var param = setExpressions[0].Parameters[0].Name;
            var query = builder.BuildUpdate(expressions.ToArray(), querySettings, param, false);

            return Execute(query);
        }

        // TODO: Finish this implementation
        internal bool Update<TEntity>(Expression<Func<TEntity, bool>> condition, params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>();
            var expressions = from c in setExpressions select ((c.Body as UnaryExpression).Operand as MethodCallExpression);
            var param = setExpressions[0].Parameters[0].Name;
            var query = builder.BuildUpdate(expressions.ToArray(), querySettings, param, true);

            query = builder.BuildUpdate(condition);

            return Execute(query);
        }
        #endregion
        #region Delete
        public bool Delete<TEntity>(TEntity entity) where TEntity : Entity, new()
        {
            var type = typeof(TEntity);
            var primaryKeys = type.GetProperties().Where(p => p.HasAttribute<PrimaryKeyAttribute>() || p.Name == "Id" || p.Name == type.Name + "Id").ToArray();
            var builder = new QueryBuilder<TEntity>();
            var query = builder.BuildDelete(querySettings, entity, primaryKeys);

            return Execute(query);
        }

        public bool Delete<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : Entity, new()
        {
            var builder = new QueryBuilder<TEntity>();
            var query = builder.BuildDelete(condition.Body, querySettings, condition.Parameters[0].Name);

            return Execute(query);
        }
        #endregion
    }
}
