// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Lappa.ORM
{
    public struct ApiRequest
    {
        public string EntityName { get; set; }
        public bool IsSelectQuery { get; set; }
        public string SqlQuery { get; set; }
        public Dictionary<string, object> SqlParameters { get; set; }
    }
}
