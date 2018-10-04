// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Lappa.ORM.Constants;
using Lappa.ORM.Logging;
using Lappa.ORM.Caching;
using static Lappa.ORM.Misc.Helper;

namespace Lappa.ORM
{
    public partial class Database
    {
        public bool ApiMode { get; private set; }
        public ILog Log { get; private set; }

        internal Connector Connector { get; private set; }

        EntityBuilder entityBuilder;
        ApiClient apiClient;

        public Database()
        {
            // Initialize the cache manager on first database creation. 
            CacheManager.Instance.Initialize();

            // Initialize dummy logger.
            Log = new Log();
        }

        public async Task<bool> InitializeAsync(ConnectorSettings connectorSettings)
        {
            Connector = new Connector { Settings = connectorSettings };
            entityBuilder = new EntityBuilder(this);


            try
            {
                Connector.Load();

                if (connectorSettings.ConnectionMode == ConnectionMode.Database)
                {
                    using (var connection = await CreateConnectionAsync())
                    {
                        // Set the database name.
                        Connector.Settings.DatabaseName = connection.Database;

                        return connection.State == ConnectionState.Open;
                    }
                }
                else
                {
                    apiClient = new ApiClient(connectorSettings.ApiHost);
                    ApiMode = true;
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());

                return false;
            }

            // Always return true for ConnectionMode.Api
            return true;
        }

        public bool Initialize(ConnectorSettings connectorSettings)
        {
            return RunSync(() => InitializeAsync(connectorSettings));
        }

        // Overwrite dummy logger.
        // Can be called at any time.
        public void SetLogger(ILog logger) => Log = logger;

        DbConnection CreateConnection() => CreateConnectionAsync().GetAwaiter().GetResult();

        internal async Task<DbConnection> CreateConnectionAsync()
        {
            var connection = Connector.CreateConnectionObject();

            connection.ConnectionString = Connector.Settings.ConnectionString;

            await connection.OpenAsync();

            return connection;
        }

        internal DbCommand CreateSqlCommand(DbConnection connection, DbTransaction transaction, string sql, params object[] args)
        {
            var sqlCommand = Connector.CreateCommandObject();

            sqlCommand.Connection = connection;
            sqlCommand.CommandText = sql;
            sqlCommand.CommandTimeout = 2147483;
            sqlCommand.Transaction = transaction;

            if (args.Length > 0)
            {
                var mParams = new DbParameter[args.Length];

                for (var i = 0; i < args.Length; i++)
                {
                    var param = Connector.CreateParameterObject();

                    param.ParameterName = "";
                    param.Value = args[i];

                    mParams[i] = param;
                }

                sqlCommand.Parameters.AddRange(mParams);
            }

            return sqlCommand;
        }

        internal bool Execute(string sql, params object[] args) => RunSync(() => ExecuteAsync(sql, args));

        internal async Task<bool> ExecuteAsync(string sql, params object[] args)
        {
            DbTransaction transaction = null;

            try
            {
                if (ApiMode)
                {
                    using (var sqlCommand = CreateSqlCommand(null, null, sql, args))
                    {
                        var affectedRows = await apiClient.GetResponse(null, sqlCommand.CommandText, Connector.Settings.ApiDeserializeFunction);

                        return Convert.ToInt32(affectedRows[0]?[0]) > 0;
                    }
                }
                else
                {
                    using (var connection = await CreateConnectionAsync())
                    using (transaction = Connector.Settings.UseTransactions ? connection.BeginTransaction(IsolationLevel.ReadCommitted) : null)
                    {
                        using (var cmd = CreateSqlCommand(connection, transaction, sql, args))
                        {
                            var affectedRows = await cmd.ExecuteNonQueryAsync();

                            transaction?.Commit();

                            return affectedRows > 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());

                transaction?.Rollback();

                return false;
            }
        }

        internal object[][] Select<T>(string sql, QueryBuilder<T> queryBuilder = null, params object[] args) where T : Entity, new()
        {
            return RunSync(() => SelectAsync(sql, queryBuilder, args));
        }

        internal async Task<object[][]> SelectAsync<T>(string sql, QueryBuilder<T> queryBuilder = null, params object[] args) where T : Entity, new()
        {
            try
            {
                if (ApiMode)
                {
                    var sqlCommand = CreateSqlCommand(null, null, sql, args);

                    return await apiClient.GetResponse(queryBuilder.EntityName, sqlCommand.CommandText, Connector.Settings.ApiDeserializeFunction);
                }
                else
                {
                    var connection = await CreateConnectionAsync();
                    var sqlCommand = CreateSqlCommand(connection, null, sql, args);

                    using (var dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                        return entityBuilder.VerifyDatabaseSchema(dataReader, queryBuilder);
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());

                return null;
            }
        }
    }
}
