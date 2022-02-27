// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa.ORM
{
    public class PoolingSettings
    {
        public bool Enabled { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }
}
