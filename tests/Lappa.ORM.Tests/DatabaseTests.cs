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
            var initSuccess = db.Initialize("Data Source=:memory:", DatabaseType.SQLite);

            Assert.True(initSuccess);
        }
    }
}
