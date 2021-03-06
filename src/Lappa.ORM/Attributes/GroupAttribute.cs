﻿// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GroupAttribute : Attribute
    {
        public string Name { get; set; }

        public GroupAttribute(string name)
        {
            Name = name;
        }
    }
}
