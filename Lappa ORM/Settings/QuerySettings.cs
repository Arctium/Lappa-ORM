// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Lappa_ORM
{
    // Use if db engines need different versions for base queries
    internal class QuerySettings
    {
        public string InsertQuery    { get; private set; }
        public string SelectQuery    { get; private set; }
        public string UpdateQuery    { get; private set; }
        public string UpdateQueryEnd { get; private set; }
        public string DeleteQuery    { get; private set; }
        public string Equal          { get; private set; }
        public string AndEqual       { get; private set; }

        // General and usable without object
        public static string Part0;
        public static string Part1;
        public static string Part2;

        public QuerySettings(ConnectionType type)
        {
            if (type == ConnectionType.MSSQL)
            {
                UpdateQuery    = "UPDATE [{1}] SET ";
                UpdateQueryEnd = "FROM [{0}] AS [{1}] WHERE ";
                DeleteQuery    = "DELETE FROM [{0}] FROM [{0}] [{1}] WHERE ";
                Equal          = "[{0}] = '{1}'";
                AndEqual       = " AND [{0}] = '{1}'";

                Part0 = "[{0}]";
                Part1 = "[{1}]";
                Part2 = "[{2}]";
            }
            else if (type == ConnectionType.MYSQL)
            {
                UpdateQuery    = "UPDATE `{0}` `{1}` SET ";
                UpdateQueryEnd = "WHERE ";
                DeleteQuery    = "DELETE FROM `{1}` USING `{0}` AS `{1}` WHERE ";
                Equal          = "{0} = '{1}'";
                AndEqual       = " AND {0} = '{1}'";

                Part0 = "`{0}`";
                Part1 = "`{1}`";
                Part2 = "`{2}`";
            }
        }
    }
}
