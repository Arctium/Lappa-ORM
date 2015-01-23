// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa_ORM.Misc
{
    internal class Helper
    {
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

        internal static object GetDefault(Type t)
        {
            Func<object> f = GetDefault<object>;

            return f.Method.GetGenericMethodDefinition().MakeGenericMethod(t).Invoke(null, null);
        }
        private static T GetDefault<T>()
        {
            return default(T);
        }
    }
}
