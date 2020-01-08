// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Common;
using System.Text.Json;
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

        public async ValueTask<bool> InitializeAsync(ConnectorSettings connectorSettings)
        {
            Connector = new Connector { Settings = connectorSettings };
            entityBuilder = new EntityBuilder(this);

            try
            {
                Connector.Load();

                // Extended caching that requires connector info.
                CacheManager.Instance.CacheQueryBuilders(Connector);

                if (connectorSettings.ConnectionMode == ConnectionMode.Database)
                {
                    using var connection = await CreateConnectionAsync();

                    // Set the database name.
                    Connector.Settings.DatabaseName = connection.Database;

                    return connection.State == ConnectionState.Open;
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

        internal async ValueTask<DbConnection> CreateConnectionAsync()
        {
            var connection = Connector.CreateConnectionObject();

            connection.ConnectionString = Connector.Settings.ConnectionString;

            await connection.OpenAsync();

            return connection;
        }

        internal DbCommand CreateSqlCommand(DbConnection connection, DbTransaction transaction, IQueryBuilder queryBuilder)
        {
            var sqlCommand = Connector.CreateCommandObject();

            sqlCommand.Connection = connection;
            sqlCommand.CommandText = queryBuilder.SqlQuery.ToString();
            sqlCommand.CommandTimeout = 2147483;
            sqlCommand.Transaction = transaction;

            foreach (var p in queryBuilder.SqlParameters)
            {
                var param = sqlCommand.CreateParameter();

                param.ParameterName = p.Key;
                param.Value = p.Value;

                sqlCommand.Parameters.Add(param);
            }

            return sqlCommand;
        }

        internal DbCommand CreateSqlCommand(DbConnection connection, DbTransaction transaction, ApiRequest apiRequest)
        {
            var sqlCommand = Connector.CreateCommandObject();

            sqlCommand.Connection = connection;
            sqlCommand.CommandText = apiRequest.SqlQuery;
            sqlCommand.CommandTimeout = 2147483;
            sqlCommand.Transaction = transaction;
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.Prepare();

            foreach (var p in apiRequest.SqlParameters)
            {
                var param = sqlCommand.CreateParameter();

                param.ParameterName = p.Key;

                var jsonElement = (JsonElement)p.Value;

                param.Value = jsonElement.ValueKind == JsonValueKind.String ? jsonElement.GetString() : jsonElement.GetRawText();

                sqlCommand.Parameters.Add(param);
            }

            return sqlCommand;
        }

        internal bool Execute(IQueryBuilder queryBuilder) => RunSync(() => ExecuteAsync(queryBuilder));

        internal async ValueTask<bool> ExecuteAsync(IQueryBuilder queryBuilder)
        {
            DbTransaction transaction = null;

            try
            {
                if (ApiMode)
                {
                    var affectedRows = await apiClient.GetResponse(queryBuilder);

                    return Convert.ToInt32(affectedRows[0]?[0]) > 0;
                }
                else
                {
                    using (var connection = await CreateConnectionAsync())
                    using (transaction = Connector.Settings.UseTransactions ? connection.BeginTransaction(IsolationLevel.ReadCommitted) : null)
                    {
                        using var cmd = CreateSqlCommand(connection, transaction, queryBuilder);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();

                        transaction?.Commit();

                        return affectedRows > 0;
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

        internal object[][] Select(IQueryBuilder queryBuilder)
        {
            return RunSync(() => SelectAsync(queryBuilder));
        }

        internal async ValueTask<object[][]> SelectAsync(IQueryBuilder queryBuilder)
        {
            try
            {
                if (ApiMode)
                {
                    queryBuilder.IsSelectQuery = true;

                    return await apiClient.GetResponse(queryBuilder);
                }
                else
                {
                    var connection = await CreateConnectionAsync();
                    var sqlCommand = CreateSqlCommand(connection, null, queryBuilder);

                    using var dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                    return entityBuilder.VerifyDatabaseSchema(dataReader, queryBuilder);
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());

                return null;
            }
        }

        async ValueTask<object[][]> ExecuteFromApiAsync(IQueryBuilder queryBuilder, ApiRequest apiRequest)
        {
            DbTransaction transaction = null;

            try
            {
                using (var connection = await CreateConnectionAsync())
                using (transaction = Connector.Settings.UseTransactions ? connection.BeginTransaction(IsolationLevel.ReadCommitted) : null)
                {
                    using var cmd = CreateSqlCommand(connection, transaction, apiRequest);
                    var affectedRows = await cmd.ExecuteNonQueryAsync();

                    transaction?.Commit();

                    return new object[1][] { new object[] { affectedRows } };
                }
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());

                transaction?.Rollback();

                return null;
            }
        }

        async ValueTask<object[][]> SelectFromApiAsync(IQueryBuilder queryBuilder, ApiRequest apiRequest)
        {
            try
            {
                var connection = await CreateConnectionAsync();
                var sqlCommand = CreateSqlCommand(connection, null, apiRequest);

                using var dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                return entityBuilder.VerifyDatabaseSchema(dataReader, queryBuilder);
            }
            catch (Exception ex)
            {
                Log.Message(LogTypes.Error, ex.ToString());

                return null;
            }
        }

        // Used for select queries from api clients only.
        public object[][] ProcessApiRequest(ApiRequest apiRequest) => RunSync(() => ProcessApiRequestAsync(apiRequest));

        public ValueTask<object[][]> ProcessApiRequestAsync(ApiRequest apiRequest)
        {
            if (apiRequest.IsSelectQuery)
                return SelectFromApiAsync(CacheManager.Instance.GetQueryBuilder(apiRequest.EntityName), apiRequest);
            else
                return ExecuteFromApiAsync(CacheManager.Instance.GetQueryBuilder(apiRequest.EntityName), apiRequest);
        }
    }
}
