// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public static class GlobalExtensions
{
    // Used for set expression in our Database.Update method.
    public static T Set<T>(this T @var, T value) => var = value;
}
