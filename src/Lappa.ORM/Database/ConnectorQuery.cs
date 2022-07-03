// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Lappa.ORM.Constants;

using System;

namespace Lappa.ORM
{
    // Use if db engines need different versions for base queries
    internal class ConnectorQuery
    {
        public string UpdateQuery { get; }
        public string UpdateQueryEnd { get; }
        public string DeleteQuery { get; }
        public string Equal { get; }
        public string AndEqual { get; }

        public string Table { get; }

        public ConnectorQuery(DatabaseType databaseType)
        {
            var escapedField = databaseType switch
            {
                DatabaseType.SQLite     => "\"{0}\"",
                DatabaseType.PostgreSql => "\"{0}\"",
                _ => throw new NotImplementedException(),
            };

            UpdateQuery    = $"UPDATE {escapedField} SET ";
            UpdateQueryEnd = $"FROM {escapedField} WHERE ";
            DeleteQuery    = $"DELETE FROM {escapedField} WHERE ";
            Equal          = $"{escapedField} = @{{0}}";
            AndEqual       = $" AND {escapedField} = @{{0}}";

            // Table & Column parts.
            Table = escapedField;
        }
    }
}
