// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Lappa.ORM.Constants;

namespace Lappa.ORM.Tests
{
    public class DatabaseTests
    {
        [Fact]
        public void SQLiteDatabaseConnection()
        {
            var db = new Database();
            var dbSettings = new ConnectorSettings
            {
                ConnectionMode = ConnectionMode.Database,
                ConnectionString = "Data Source=:memory:",
                DatabaseType = DatabaseType.SQLite
            };

            var initSuccess = db.Initialize(dbSettings);

            Assert.True(initSuccess);
        }
    }
}
