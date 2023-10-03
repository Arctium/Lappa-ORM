// Copyright (c) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Lappa.ORM
{
    public record struct ApiRequest(string EntityName, bool IsSelectQuery, string SqlQuery, Dictionary<string, object> SqlParameters);
}
