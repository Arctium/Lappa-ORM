// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Lappa.ORM.Misc;

namespace Lappa.ORM
{
    public partial class Database
    {
        // Use en-US as number format for all languages.
        IFormatProvider numberFormat = new CultureInfo("en-US").NumberFormat;

        // TODO Rewrite...
        internal void AssignForeignKeyData<TEntity>(TEntity entity, PropertyInfo[] foreignKeys, ConcurrentDictionary<int, int> groups) where TEntity : Entity, new()
        {
            for (var i = 0; i < foreignKeys.Length; i++)
            {
                var fk = foreignKeys[i];
                var fkName = GetForeignKeyName<TEntity>(fk);

                if (fkName?.Item1 != null)
                {
                    var value = typeof(TEntity).GetTypeInfo().GetProperty(fkName.Item1).GetValue(entity);
                    var pType = fk.PropertyType.GetTypeInfo().IsGenericType ? fk.PropertyType.GetTypeInfo().GetGenericArguments()[0] : fk.PropertyType;
                    var foreignKeyData = WhereForeignKey(pType, Helper.Pluralize(pType), fkName.Item2, value, groups);

                    if (foreignKeyData == null || foreignKeyData.Count == 0)
                        continue;

                    fk.SetValue(entity, fk.PropertyType.GetTypeInfo().IsGenericType ? foreignKeyData : foreignKeyData[0], null);
                }
            }
        }

        internal Tuple<string, string> GetForeignKeyName<T>(PropertyInfo prop)
        {
            var type = typeof(T);
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

            return fkNameByPk == null ? null : Tuple.Create(fkNameByPk, typeName + fkNameByPk);
        }

        // TODO Rewrite.
        // This function is disabled until it's rewritten. absolutly not compatible with the api connection mode.
        internal IList WhereForeignKey(Type entityType, string name, string fkName, object value, ConcurrentDictionary<int, int> groups)
        {
            return null;

            /*
            var entityLock = new object();
            var query = new StringBuilder();

            query.AppendFormat(numberFormat, "SELECT * FROM " + Connector.Query.Table + " WHERE ", name);
            query.AppendFormat(numberFormat, Connector.Query.Equal, fkName, value);

            var entities = entityType.CreateList();

            using (var dataReader = Select(query.ToString()))
            {
                if (dataReader?.Read() == true)
                {
                    var properties = entityType.GetReadWriteProperties();

                    // TODO: Replace with error log?
                    if (dataReader.FieldCount != properties.Length)
                        throw new NotSupportedException("Columns doesn't match the entity fields.");

                    do
                    {
                        var entity = Activator.CreateInstance(entityType) as Entity;

                        for (var j = 0; j < properties.Length; j++)
                            properties[j].SetValue(entity, dataReader[properties[j].GetName()].ChangeTypeGet(properties[j].PropertyType));

                        entity.InitializeNonTableProperties();

                        // IList isn't thread safe...
                        lock (entityLock)
                            entities.Add(entity);
                    } while (dataReader.Read());
                }
            }

            return entities;*/
        }
    }
}
