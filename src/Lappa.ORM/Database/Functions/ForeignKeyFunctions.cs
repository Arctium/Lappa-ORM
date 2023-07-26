// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    public partial class Database<T>
    {
        // Use en-US as number format for all languages.
        IFormatProvider numberFormat = new CultureInfo("en-US").NumberFormat;

        // TODO Rewrite...
        internal void AssignForeignKeyData<TEntity>(TEntity entity, PropertyInfo[] foreignKeys, ConcurrentDictionary<int, int> groups) where TEntity : IEntity, new()
        {
            for (var i = 0; i < foreignKeys.Length; i++)
            {
                var fk = foreignKeys[i];
                var fkName = GetForeignKeyName<TEntity>(fk);

                if (fkName?.Item1 != null)
                {
                    var value = typeof(TEntity).GetTypeInfo().GetProperty(fkName.Item1).GetValue(entity);
                    var pType = fk.PropertyType.GetTypeInfo().IsGenericType ? fk.PropertyType.GetTypeInfo().GetGenericArguments()[0] : fk.PropertyType;
                    var foreignKeyData = WhereForeignKey<TEntity>(pType, Helper.Pluralize(pType), fkName.Item2, value, groups).GetAwaiter().GetResult();

                    if (foreignKeyData == null || foreignKeyData.Count == 0)
                        continue;

                    fk.SetValue(entity, fk.PropertyType.GetTypeInfo().IsGenericType ? foreignKeyData : foreignKeyData[0], null);
                }
            }
        }

        internal Tuple<string, string> GetForeignKeyName<TEntity>(PropertyInfo prop)
        {
            var type = typeof(TEntity);
            var typeName = type.Name;
            var fkNameByAttribute = prop.GetCustomAttribute<ForeignKeyAttribute>();

            if (fkNameByAttribute != null)
                return Tuple.Create(fkNameByAttribute.Name, fkNameByAttribute.Name);

            var primaryKeysByAttribute = type.GetTypeInfo().DeclaredProperties.Where(p =>
            {
                return p.HasAttribute<PrimaryKeyAttribute>();
            }).ToArray();

            var fkNameByPk = "";

            if (primaryKeysByAttribute.Length > 0)
                fkNameByPk = primaryKeysByAttribute[0]?.GetName();
            else
            {
                var primaryKeys = type.GetTypeInfo().DeclaredProperties.Where(p => p.GetName() == "Id" || p.GetName() == typeName + "Id").ToArray();

                fkNameByPk = primaryKeys.Length > 0 ? primaryKeys[0]?.GetName() : null;
            }

            // One to many relations.
            if (prop.PropertyType.IsGenericType)
                return fkNameByPk == null ? null : Tuple.Create(fkNameByPk, typeName + fkNameByPk);

            // All other relations.
            return fkNameByPk == null ? null : Tuple.Create(prop.Name + fkNameByPk, fkNameByPk);
        }

        // TODO Rewrite.
        // Absolutly not compatible with the api connection mode.
        internal async Task<IList> WhereForeignKey<TEntity>(Type entityType, string name, string fkName, object value, ConcurrentDictionary<int, int> groups) where TEntity : IEntity, new()
        {
            var builder = new QueryBuilder<TEntity>(Connector.Query);

            builder.BuildWhereForeignKey(entityType, name, fkName, value);

            var entityLock = new object();
            var entities = entityType.CreateList();

            var dataReader = await Select(builder);
            {
                if (dataReader?.Length != 0)
                {
                    var properties = entityType.GetReadWriteProperties();

                    // TODO: Replace with error log?
                    if (dataReader[0].Length != properties.Length)
                        throw new NotSupportedException("Columns doesn't match the entity fields.");

                    for (var i = 0; i < dataReader.Length; i++)
                    {
                        var entity = Activator.CreateInstance(entityType) as IEntity;

                        for (var j = 0; j < properties.Length; j++)
                            properties[j].SetValue(entity, dataReader[i][j].ChangeTypeGet(properties[j].PropertyType));

                        entity.InitializeNonTableProperties();

                        // IList isn't thread safe...
                        lock (entityLock)
                            entities.Add(entity);
                    }
                }
            }

            return entities;
        }
    }
}
