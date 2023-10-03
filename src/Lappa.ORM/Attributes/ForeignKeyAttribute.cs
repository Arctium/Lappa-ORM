// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute(string name) : Attribute
    {
        public string Name { get; set; } = name;
    }
}
