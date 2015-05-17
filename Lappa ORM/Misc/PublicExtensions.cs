// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa_ORM
{
    public static class PublicExtensions
    {
        // Úsed for set expression in our Database.Update method.
        public static T Set<T>(this T @var, T value) => var = value;
    }
}
