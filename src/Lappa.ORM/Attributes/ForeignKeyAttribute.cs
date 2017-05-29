// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace LappaORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        public string Name { get; set; }

        public ForeignKeyAttribute(string name)
        {
            Name = name;
        }
    }
}
