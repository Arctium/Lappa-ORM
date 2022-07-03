// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa.ORM
{
    public interface IEntity
    {
        bool LoadForeignKeys => false;

        void InitializeNonTableProperties() { }
    }
}
