// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Threading.Tasks;

using Lappa.Pluralization;

namespace Lappa.ORM.Misc
{
    internal class Helper
    {
        // Create only one service. Only enUS supported.
        static readonly PluralizationService pluralService = new PluralizationService();

        public static string Pluralize<T>() => Pluralize(typeof(T));
        public static string Pluralize(Type t)
        {
            var dbTableAttribute = t.GetTypeInfo().GetCustomAttribute<DBTableAttribute>();

            if (dbTableAttribute != null)
            {
                if (dbTableAttribute.Name == null)
                    return dbTableAttribute.Pluralize ? pluralService.Pluralize(t.Name) : t.Name;
                else
                    return dbTableAttribute.Pluralize ? pluralService.Pluralize(dbTableAttribute.Name) : dbTableAttribute.Name;
            }

            return pluralService.Pluralize(t.Name);
        }

        internal static int GetDefaultFieldSize(Type type)
        {
            switch (type.Name)
            {
                case "Boolean":
                    return 1;
                case "SByte":
                case "Byte":
                    return 4;
                case "Int16":
                case "UInt16":
                    return 6;
                case "Int32":
                case "UInt32":
                    return 11;
                case "Int64":
                case "UInt64":
                    return 20;
                case "Single":
                case "Double":
                    return 0;
                case "String":
                    return 255;
                default:
                    return 0;
            }
        }

        private static T GetDefault<T>() => default(T);

        internal static object GetDefault(Type t)
        {
            Func<object> f = GetDefault<object>;

            return f.GetMethodInfo().GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        }

        public static TResult RunSync<TResult>(Func<Task<TResult>> func) => func().ConfigureAwait(false).GetAwaiter().GetResult();
        public static void RunSync(Func<Task> func) => func().ConfigureAwait(false).GetAwaiter().GetResult();
        public static TResult RunSync<TResult>(Func<ValueTask<TResult>> func) => func().GetAwaiter().GetResult();
        public static void RunSync(Func<ValueTask> func) => func().GetAwaiter().GetResult();
    }
}
