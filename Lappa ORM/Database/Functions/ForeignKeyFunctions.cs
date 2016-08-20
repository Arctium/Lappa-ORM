// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using LappaORM.Misc;

namespace LappaORM
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
                    var data = WhereForeignKey(pType, Helper.Pluralize(pType), fkName.Item2, value, groups);

                    if (data == null || data.Count == 0)
                        continue;

                    fk.SetValue(entity, fk.PropertyType.GetTypeInfo().IsGenericType ? data : data[0], null);
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
                fkNameByPk = primaryKeysByAttribute[0]?.Name;
            else
            {
                var primaryKeys = type.GetTypeInfo().DeclaredProperties.Where(p => p.Name == "Id" || p.Name == typeName + "Id").ToArray();

                fkNameByPk = primaryKeys.Length > 0 ? primaryKeys[0]?.Name : null;
            }

            return fkNameByPk == null ? null : Tuple.Create(fkNameByPk, typeName + fkNameByPk);
        }

        // TODO Rewrite...
        internal IList WhereForeignKey(Type entityType, string name, string fkName, object value, ConcurrentDictionary<int, int> groups)
        {
            var entityLock = new object();
            var query = new StringBuilder();

            query.AppendFormat(numberFormat, "SELECT * FROM " + connectorQuery.Part0 + " WHERE ", name);
            query.AppendFormat(numberFormat, connectorQuery.Equal, fkName, value);

            var entities = entityType.CreateList();
            var data = Select(query.ToString());

            if (data != null)
            {
                // Some MySql connectors need at least one read before all column info are available.
                data.Read();

                if (!data.HasRows)
                    return entities;

                var properties = entityType.GetReadWriteProperties();

                if (data.FieldCount != properties.Length)
                    throw new NotSupportedException("Columns doesn't match the entity fields.");

                do
                {
                    var entity = Activator.CreateInstance(entityType) as Entity;

                    for (var j = 0; j < properties.Length; j++)
                        properties[j].SetValue(entity, data[properties[j].Name].ChangeTypeGet(properties[j].PropertyType));

                    entity.InitializeNonTableProperties();

                    // IList isn't thread safe...
                    lock (entityLock)
                        entities.Add(entity);
                } while (data.Read());
            }

            return entities;
        }
    }
}
