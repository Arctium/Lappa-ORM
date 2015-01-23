// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa_ORM
{
    public abstract class Entity
    {
        public bool AutoAssignForeignKeys;

        public Entity(bool autoAssignForeignKeys = true)
        {
            AutoAssignForeignKeys = autoAssignForeignKeys;
        }

        public virtual void InitializeNonTableProperties() { }
    }
}
