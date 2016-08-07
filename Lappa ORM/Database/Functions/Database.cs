// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using LappaORM.Constants;

namespace LappaORM
{
    public partial class Database
    {
        public DatabaseType Type { get; private set; }

        string connectionString;
        Connector connector;
        ConnectorQuery connectorQuery;
        EntityBuilder entityBuilder;

        public bool Initialize(string connString, DatabaseType type)
        {
            Type = type;

            connectionString = connString;
            connector = new Connector(type);
            connectorQuery = new ConnectorQuery(type);

            entityBuilder = new EntityBuilder(this);

            var isOpen = false;

            try
            {
                using (var connection = CreateConnection())
                    isOpen = connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

            return isOpen;
        }

        // Overwrite dummy logger.
        // Can be called at any time.
        //public void EnableLogging<T>(ILog<T> logger) => Helper.Log = logger;

        DbConnection CreateConnection()
        {
            var connection = connector.CreateConnectionObject();

            connection.ConnectionString = connectionString;

            connection.Open();

            return connection;
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand CreateSqlCommand(DbConnection connection, string sql, params object[] args)
        {
            var sqlCommand = connector.CreateCommandObject();

            sqlCommand.Connection = connection;
            sqlCommand.CommandText = sql;
            sqlCommand.CommandTimeout = 2147483;

            if (args.Length > 0)
            {
                var mParams = new SqlParameter[args.Length];

                for (var i = 0; i < args.Length; i++)
                    mParams[i] = new SqlParameter("", args[i]);

                sqlCommand.Parameters.AddRange(mParams);
            }

            return sqlCommand;
        }

        internal async Task<bool> ExecuteAsync(string sql, params object[] args)
        {
            try
            {
                var ret = false;

                using (var connection = CreateConnection())
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                        ret = await cmd.ExecuteNonQueryAsync() > 0;
                }

                return ret;
            }
            catch
            {
                return false;
            }
        }

        internal bool Execute(string sql, params object[] args)
        {
            try
            {
                var ret = false;

                using (var connection = CreateConnection())
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                        ret = cmd.ExecuteNonQuery() > 0;
                }

                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        internal async Task<DbDataReader> SelectAsync(string sql, string tableName, params object[] args)
        {
            try
            {
                DbDataReader readTask = null;

                var connection = CreateConnection();
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                    {
                        readTask = await cmd.ExecuteReaderAsync();
                    }
                }

                return readTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        internal DbDataReader Select(string sql, string tableName, params object[] args)
        {
            try
            {
                DbDataReader result = null;

                using (var connection = CreateConnection())
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                    {
                        result = cmd.ExecuteReader();
                    }
                }

                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
