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

        public string Part0 { get; private set; }
        public string Part1 { get; private set; }
        public string Part2 { get; private set; }

        public QuerySettings(ConnectionType type)
        {
            if (type == ConnectionType.MSSql)
            {
                UpdateQuery    = "UPDATE [{0}] SET ";
                UpdateQueryEnd = "FROM [{0}] WHERE ";
                DeleteQuery    = "DELETE FROM [{0}] WHERE ";
                Equal          = "[{0}] = '{1}'";
                AndEqual       = " AND [{0}] = '{1}'";

                Part0 = "[{0}]";
                Part1 = "[{1}]";
                Part2 = "[{2}]";
            }
            else if (type == ConnectionType.MySql)
            {
                UpdateQuery    = "UPDATE `{0}` SET ";
                UpdateQueryEnd = "WHERE ";
                DeleteQuery    = "DELETE FROM `{0}` WHERE ";
                Equal          = "`{0}` = '{1}'";
                AndEqual       = " AND `{0}` = '{1}'";

                Part0 = "`{0}`";
                Part1 = "`{1}`";
                Part2 = "`{2}`";
            }
            else if (type == ConnectionType.SQLite)
            {
                UpdateQuery = "UPDATE \"{0}\" SET ";
                UpdateQueryEnd = "WHERE ";
                DeleteQuery = "DELETE FROM \"{0}\" WHERE ";
                Equal = "\"{0}\" = '{1}'";
                AndEqual = " AND \"{0}\" = '{1}'";

                Part0 = "\"{0}\"";
                Part1 = "\"{1}\"";
                Part2 = "\"{2}\"";
            }
        }
    }
}
