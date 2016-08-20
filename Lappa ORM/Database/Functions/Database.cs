// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using LappaORM.Constants;
using LappaORM.Logging;

namespace LappaORM
{
    public partial class Database
    {
        public DatabaseType Type { get; private set; }
        public ILog Log { get; private set; }

        string connectionString;
        Connector connector;
        ConnectorQuery connectorQuery;
        EntityBuilder entityBuilder;

        public Database()
        {
            // Initialize dummy logger.
            Log = new Log();

            connector = new Connector();
        }

        public bool Initialize(string connString, DatabaseType type, bool loadConnectorFromFile = true)
        {
            Type = type;

            connectionString = connString;
            connectorQuery = new ConnectorQuery(type);

            entityBuilder = new EntityBuilder(this);

            var isOpen = false;

            try
            {
                connector.Load(type, loadConnectorFromFile);

                using (var connection = CreateConnection())
                    isOpen = connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());
            }

            return isOpen;
        }

        // Overwrite dummy logger.
        // Can be called at any time.
        public void SetLogger(ILog logger) => Log = logger;

        // Default for MySql is the current assembly directory.
        // Must be called before Initialize.
        public void SetConnectorFilePath(string connectorFilePath) => connector.FilePath = connectorFilePath;

        // Default for MySql is "MySql.Data.dll".
        // Must be called before Initialize.
        public void SetConnectorFileName(string connectorFileName) => connector.FileName = connectorFileName;

        DbConnection CreateConnection()
        {
            var connection = connector.CreateConnectionObject();

            connection.ConnectionString = connectionString;

            connection.Open();

            return connection;
        }

        internal DbCommand CreateSqlCommand(DbConnection connection, string sql, params object[] args)
        {
            var sqlCommand = connector.CreateCommandObject();

            sqlCommand.Connection = connection;
            sqlCommand.CommandText = sql;
            sqlCommand.CommandTimeout = 2147483;

            if (args.Length > 0)
            {
                var mParams = new DbParameter[args.Length];

                for (var i = 0; i < args.Length; i++)
                {
                    var param = connector.CreateParameterObject();

                    param.ParameterName = "";
                    param.Value = args[i];

                    mParams[i] = param;
                }

                sqlCommand.Parameters.AddRange(mParams);
            }

            return sqlCommand;
        }

        internal async Task<bool> ExecuteAsync(string sql, params object[] args)
        {
            var ret = false;

            try
            {
                using (var connection = CreateConnection())
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                        ret = await cmd.ExecuteNonQueryAsync() > 0;
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());
            }

            return ret;
        }

        internal bool Execute(string sql, params object[] args)
        {
            var ret = false;

            try
            {

                using (var connection = CreateConnection())
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                        ret = cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());
            }

            return ret;
        }

        internal async Task<DbDataReader> SelectAsync(string sql, params object[] args)
        {
            DbDataReader result = null;

            try
            {
                using (var cmd = CreateSqlCommand(CreateConnection(), sql, args))
                {
                    result = await cmd.ExecuteReaderAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());
            }

            return result;
        }

        internal DbDataReader Select(string sql, params object[] args)
        {
            DbDataReader result = null;

            try
            {
                using (var cmd = CreateSqlCommand(CreateConnection(), sql, args))
                {
                    result = cmd.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());
            }

            return result;
        }
    }
}
