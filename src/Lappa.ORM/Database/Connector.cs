// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Data.SqlClient;
using Lappa.ORM.Constants;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace Lappa.ORM
{
    internal sealed class Connector
    {
        internal ConnectorSettings Settings { get; set; }
        internal ConnectorQuery Query { get; private set; }

        Type connectionType;
        Type commandType;
        Type parameterType;

        internal void Load()
        {
            Query = new ConnectorQuery(Settings.DatabaseType);

            switch (Settings.DatabaseType)
            {
                case DatabaseType.MSSql:
                {

                    connectionType = typeof(SqlConnection);
                    commandType = typeof(SqlCommand);
                    parameterType = typeof(SqlParameter);

                    break;
                }
                case DatabaseType.MySql:
                {
                    connectionType = typeof(MySqlConnection);
                    commandType = typeof(MySqlCommand);
                    parameterType = typeof(MySqlParameter);

                    break;
                }
                case DatabaseType.SQLite:
                {
                    connectionType = typeof(SqliteConnection);
                    commandType = typeof(SqliteCommand);
                    parameterType = typeof(SqliteParameter);

                    break;
                }
                case DatabaseType.PostgreSql:
                {
                    connectionType = typeof(NpgsqlConnection);
                    commandType = typeof(NpgsqlCommand);
                    parameterType = typeof(NpgsqlParameter);

                    break;
                }
                default:
                    break;
            }

            if (connectionType == null || commandType == null || parameterType == null)
                throw new TypeLoadException($"connectionType: {connectionType}, commandType: {commandType}, parameterType: {parameterType}.");
        }

        public DbConnection CreateConnectionObject() => Activator.CreateInstance(connectionType) as DbConnection;
        public DbCommand CreateCommandObject() => Activator.CreateInstance(commandType) as DbCommand;
        public DbParameter CreateParameterObject() => Activator.CreateInstance(parameterType) as DbParameter;
    }
}
