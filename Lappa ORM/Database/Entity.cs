// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa_ORM
{
    public abstract class Entity
    {
        public readonly bool AutoAssignForeignKeys = true;

        public virtual void InitializeNonTableProperties() { }
    }
}
