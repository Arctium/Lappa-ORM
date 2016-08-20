// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LappaORM.Misc
{
    internal static class Extensions
    {
        // Create only one service. Only enUS supported.
        internal static string Pluralize(this string word) => Pluralize(word);

        internal static IList CreateList(this Type type)
        {
            var genericType = typeof(List<>).MakeGenericType(type);

            return Activator.CreateInstance(genericType) as IList;
        }

        internal static T ChangeTypeGet<T>(this object value) => (T)ChangeTypeGet(value, typeof(T));

        internal static object ChangeTypeGet(this object value, Type destType)
        {
            return Convert.ChangeType(value, destType.GetTypeInfo().IsEnum ? destType.GetTypeInfo().GetEnumUnderlyingType() : destType);
        }

        internal static T ChangeTypeSet<T>(this object value) => (T)ChangeTypeSet(value, typeof(T));

        internal static object ChangeTypeSet(this object value, Type destType)
        {
            if (value is bool)
                return Convert.ToByte(value);
            else if (destType.GetTypeInfo().IsEnum)
                return Convert.ChangeType(value, destType.GetTypeInfo().GetEnumUnderlyingType());

            return Convert.ChangeType(value, destType);
        }

        internal static Func<T, object> GetGetter<T>(this PropertyInfo propertyInfo)
        {
            var paramExpression = Expression.Parameter(typeof(T));
            var propertyExpression = Expression.Property(paramExpression, propertyInfo);
            var convertExpression = Expression.TypeAs(propertyExpression, typeof(object));

            return Expression.Lambda<Func<T, object>>(convertExpression, paramExpression).Compile();
        }

        internal static Action<T, object> GetSetter<T>(this PropertyInfo propertyInfo)
        {
            var paramExpression = Expression.Parameter(typeof(T));
            var propertyExpression = Expression.Property(paramExpression, propertyInfo);
            var valueExpression = Expression.Parameter(typeof(object));
            var convertExpression = Expression.Convert(valueExpression, propertyInfo.PropertyType);
            var assignExpression = Expression.Assign(propertyExpression, convertExpression);

            return Expression.Lambda<Action<T, object>>(assignExpression, paramExpression, valueExpression).Compile();
        }

        internal static Func<T, object> GetGetter<T>(this FieldInfo fieldInfo)
        {
            var paramExpression = Expression.Parameter(typeof(T));
            var propertyExpression = Expression.Field(paramExpression, fieldInfo);
            var convertExpression = Expression.TypeAs(propertyExpression, typeof(object));

            return Expression.Lambda<Func<T, object>>(convertExpression, paramExpression).Compile();
        }

        internal static Action<T, object> GetSetter<T>(this FieldInfo fieldInfo)
        {
            var paramExpression = Expression.Parameter(typeof(T));
            var propertyExpression = Expression.Field(paramExpression, fieldInfo);
            var valueExpression = Expression.Parameter(typeof(object));
            var convertExpression = Expression.Convert(valueExpression, fieldInfo.FieldType);
            var assignExpression = Expression.Assign(propertyExpression, convertExpression);

            return Expression.Lambda<Action<T, object>>(assignExpression, paramExpression, valueExpression).Compile();
        }

        internal static object GetValue<T>(this Func<T, object> action, T entity)
        {
            var ret = action.Invoke(entity);

            return ret?.ChangeTypeGet(ret.GetType());
        }

        internal static void SetValue<T>(this Action<T, object> action, T entity, object value) => action.Invoke(entity, value);

        internal static bool HasMember(this object obj, string memberName)
        {
            var type = obj.GetType();

            return type.GetTypeInfo().GetField(memberName) != null || type.GetTypeInfo().GetProperty(memberName) != null;
        }

        internal static bool HasAttribute<T>(this PropertyInfo property) where T : Attribute
        {
            return property.GetCustomAttribute<T>() != null;
        }

        internal static bool IsCustomClass(this Type type) => type.GetTypeInfo().IsClass && type != typeof(string);

        internal static bool IsCustomStruct(this Type type) => type.GetTypeInfo().IsValueType && !type.GetTypeInfo().IsEnum && !type.GetTypeInfo().IsPrimitive;

        internal static PropertyInfo[] GetReadWriteProperties(this Type t)
        {
            return t.GetTypeInfo().DeclaredProperties.Where(p => !p.GetMethod.IsVirtual && p.GetSetMethod(false) != null).ToArray();
        }

        internal static Dictionary<TKey, TValue> AsDictionary<TKey, TValue>(this TValue[] data, Func<TValue, TKey> selector)
        {
            var dic = new Dictionary<TKey, TValue>(data.Length);
            //var datapPartitioner = Partitioner.Create(0, data.Length);

            //Parallel.For(0, data.Length - 1, i =>
            //{
            for (var i = 0; i < data.Length; i++)
            {
                dic.Add(selector(data[i]), data[i]);
            }
            //});

            return dic;
        }
    }
}
