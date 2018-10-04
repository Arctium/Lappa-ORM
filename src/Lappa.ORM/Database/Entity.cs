// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa.ORM
{
    public abstract class Entity
    {
        public bool LoadForeignKeys { get; } = false;

        public virtual void InitializeNonTableProperties()
        {
        }
    }
}
