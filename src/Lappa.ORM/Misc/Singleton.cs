// Copyright (c) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace LapapORM.Misc
{
    public abstract class Singleton<T> where T : class
    {
        public bool Initialized => lazy.IsValueCreated;
        public static T Instance => lazy.Value;

        static readonly Lazy<T> lazy = new Lazy<T>(() =>
        {
            var constructorInfo = typeof(T).GetTypeInfo().GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

            return constructorInfo[0].Invoke(new object[0]) as T;
        });

        public void Initialize() { }
    }
}
