﻿// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DBFieldAttribute : Attribute
    {
        public string Name    { get; set; }
        public int Size       { get; set; }
        public object Default { get; set; }
        public bool Null      { get; set; }
    }
}
