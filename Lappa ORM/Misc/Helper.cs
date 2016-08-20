// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using LappaPluralization;

namespace LappaORM.Misc
{
    internal class Helper
    {
        // Create only one service. Only enUS supported.
        static readonly PluralizationService pluralService = new PluralizationService();

        public static string Pluralize<T>() => Pluralize(typeof(T));
        public static string Pluralize(Type t) => t.GetTypeInfo().IsDefined(typeof(NoPluralizationAttribute)) ? t.Name : pluralService.Pluralize(t.Name);

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
    }
}
