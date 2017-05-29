// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DBTableAttribute : Attribute
    {
        public string Name    { get; set; } = null;
        public bool Pluralize { get; set; } = true;
    }
}
