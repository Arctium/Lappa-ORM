// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa.ORM
{
    public abstract class Entity
    {
        public bool LoadForeignKeys { get; } = true;

        public virtual void InitializeNonTableProperties()
        {
        }
    }
}
