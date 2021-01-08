// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa.ORM.Caching
{
    internal class TypeInfoCache
    {
        public bool IsArray { get; set; }
        public bool IsArrayGroup { get; set; }
        public bool IsCustomClass { get; set; }
        public bool IsCustomStruct { get; set; }
    }
}
