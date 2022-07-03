// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Lappa.ORM.Constants;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Lappa.ORM
{
    public interface IDatabase<T> : IDatabase
    {

    }

    public interface IDatabase
    {
        ValueTask<bool> Any<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : IEntity, new();
        ValueTask<long> Count<TEntity>(Expression<Func<TEntity, object>> condition = null) where TEntity : IEntity, new();
        ValueTask<bool> Create<TEntity>(MySqlEngine dbEngine = MySqlEngine.MyISAM, bool replaceTable = false) where TEntity : IEntity, new();
        ValueTask<bool> Delete<TEntity>(Expression<Func<TEntity, object>> condition) where TEntity : IEntity, new();
        ValueTask<bool> Delete<TEntity>(TEntity entity) where TEntity : IEntity, new();
        ValueTask<bool> Exists<TEntity>() where TEntity : IEntity, new();
        ValueTask<int> GetAutoIncrementValue<TEntity>() where TEntity : IEntity, new();
        ValueTask<bool> Insert<TEntity>(IReadOnlyList<TEntity> entities) where TEntity : IEntity, new();
        ValueTask<bool> Insert<TEntity>(TEntity entity) where TEntity : IEntity, new();
        ValueTask<TEntity[]> Select<TEntity>(Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new();
        ValueTask<Dictionary<TKey, TEntity>> Select<TKey, TEntity>(Func<TEntity, TKey> func, Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new();
        ValueTask<TEntity> Single<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new();
        ValueTask<bool> Update<TEntity>(Expression<Func<TEntity, bool>> condition, params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : IEntity, new();
        ValueTask<bool> Update<TEntity>(TEntity entity) where TEntity : IEntity, new();
        ValueTask<bool> UpdateAll<TEntity>(params Expression<Func<TEntity, object>>[] setExpressions) where TEntity : IEntity, new();
        ValueTask<TEntity[]> Where<TEntity>(Expression<Func<TEntity, object>> condition, Expression<Func<TEntity, object>> newExpression = null) where TEntity : IEntity, new();
    }
}
