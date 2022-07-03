// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Lappa.ORM.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lappa.ORM.Tests
{
    public class TestDatabase : Database<TestDatabase>, IDatabase
    {
        public TestDatabase(ILogger<TestDatabase> logger, IOptions<ConnectionSettings> connectionSettings)
            : base(logger, connectionSettings)
        {
        }
    }

    public class DatabaseTests
    {
        [Fact]
        public async void SQLiteDatabaseConnection()
        {
            var settings = Options.Create(new ConnectionSettings
            {
                ConnectionMode = ConnectionMode.Database,
                Type = DatabaseType.SQLite,
                ConnectionStringOverride = "Data Source=:memory:"
            });

            var loggerFactory = LoggerFactory.Create(builder => builder.ClearProviders());
            var db = new TestDatabase(loggerFactory.CreateLogger<TestDatabase>(), settings);

            using var connection = await db.CreateConnection();
            Assert.True(connection.State == ConnectionState.Open);
        }
    }
}
