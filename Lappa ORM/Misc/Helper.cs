// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using LappaORM.Logging;
using LappaPluralization;

namespace LappaORM.Misc
{
    internal class Helper
    {
        // Use dummy logger as default.
        internal static ILog<LogTypes> Log { get; set; } = new Log();

        // Create only one service. Only enUS supported.
        static readonly PluralizationService pluralService = new PluralizationService();

        public static string Pluralize<T>() => Pluralize(typeof(T));
        public static string Pluralize(Type t) => t.GetTypeInfo().IsDefined(typeof(NoPluralizationAttribute)) ? t.Name : pluralService.Pluralize(t.Name);
    }
}
