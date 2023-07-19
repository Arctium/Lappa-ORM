// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using Lappa.ORM.Caching;
using Lappa.ORM.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lappa.ORM
{
    public sealed class Database : Database<Database>, IDatabase
    {
        public Database(ILogger<Database> logger, IOptionsMonitor<ConnectionSettings> connectionSettings) : base(logger, connectionSettings)
        {
        }
    }

    public abstract partial class Database<T> : IDatabase<T>
    {
        public bool ApiMode { get; private set; }

        internal Connector Connector { get; private set; }

        protected readonly ILogger<T> logger;
        protected readonly ConnectionSettings connectionSettings;

        EntityBuilder<T> entityBuilder;
        ApiClient apiClient;

        public Database(ILogger<T> logger, IOptionsMonitor<ConnectionSettings> connectionSettings)
        {
            this.logger = logger;
            this.connectionSettings = connectionSettings.Get(typeof(T).Name);

            // TODO: Logic should not be here.
            {
                // Initialize the cache manager on first database creation. 
                CacheManager.Instance.Initialize();

                Connector = new Connector
                {
                    Settings = new()
                    {
                        ConnectionMode = this.connectionSettings.ConnectionMode,
                        ApiHost = this.connectionSettings.ApiHost,
                        ConnectionString = this.connectionSettings.ToString(),
                        DatabaseType = this.connectionSettings.Type,
                        UseTransactions = this.connectionSettings.UseTransactions,
                        DatabaseName = this.connectionSettings.ConnectionMode == ConnectionMode.Database ? this.connectionSettings.Database : ""
                    }
                };

                try
                {
                    Connector.Load();

                    // Extended caching that requires connector info.
                    CacheManager.Instance.CacheQueryBuilders(Connector);

                    if (this.connectionSettings.ConnectionMode == ConnectionMode.Api)
                    {
                        apiClient = new ApiClient(this.connectionSettings.ApiHost);
                        ApiMode = true;
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, ex.ToString());
                    throw;
                }

                entityBuilder = new EntityBuilder<T>(logger, this);
            }
        }

        internal async Task<DbConnection> CreateConnection()
        {
            var connection = Connector.CreateConnectionObject();

            connection.ConnectionString = Connector.Settings.ConnectionString;

            await connection.OpenAsync();

            return connection;
        }

        internal async Task<DbCommand> CreateSqlCommand(DbConnection connection, DbTransaction transaction, IQueryBuilder queryBuilder)
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

            // Always prepare for now.
            await sqlCommand.PrepareAsync();

            return sqlCommand;
        }

        internal async Task<DbCommand> CreateSqlCommand(DbConnection connection, DbTransaction transaction, ApiRequest apiRequest)
        {
            var sqlCommand = Connector.CreateCommandObject();

            sqlCommand.Connection = connection;
            sqlCommand.CommandText = apiRequest.SqlQuery;
            sqlCommand.CommandTimeout = 2147483;
            sqlCommand.Transaction = transaction;
            sqlCommand.CommandType = CommandType.Text;

            foreach (var p in apiRequest.SqlParameters)
            {
                var param = sqlCommand.CreateParameter();

                param.ParameterName = p.Key;

                var jsonElement = (JsonElement)p.Value;

                param.Value = jsonElement.ValueKind == JsonValueKind.String ? jsonElement.GetString() : jsonElement.GetRawText();

                sqlCommand.Parameters.Add(param);
            }

            // Always prepare for now.
            await sqlCommand.PrepareAsync();

            return sqlCommand;
        }

        internal async ValueTask<bool> Execute(IQueryBuilder queryBuilder)
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
                    using (var connection = await CreateConnection())
                    using (transaction = Connector.Settings.UseTransactions ? await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted) : null)
                    {
                        using var cmd = await CreateSqlCommand(connection, transaction, queryBuilder);
                        var affectedRows = await cmd.ExecuteNonQueryAsync();

                        await transaction?.CommitAsync();

                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.ToString());

                transaction?.RollbackAsync();

                return false;
            }
        }

        internal async ValueTask<object[][]> Select(IQueryBuilder queryBuilder)
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
                    var connection = await CreateConnection();
                    var sqlCommand = await CreateSqlCommand(connection, null, queryBuilder);

                    using var dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                    queryBuilder.IsSelectQuery = true;

                    return entityBuilder.VerifyDatabaseSchema(dataReader, queryBuilder);
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.ToString());

                return null;
            }
        }

        async ValueTask<object[][]> ExecuteFromApi(IQueryBuilder queryBuilder, ApiRequest apiRequest)
        {
            DbTransaction transaction = null;

            try
            {
                using (var connection = await CreateConnection())
                using (transaction = Connector.Settings.UseTransactions ? await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted) : null)
                {
                    using var cmd = await CreateSqlCommand(connection, transaction, apiRequest);
                    var affectedRows = await cmd.ExecuteNonQueryAsync();

                    transaction?.CommitAsync();

                    return new object[1][] { new object[] { affectedRows } };
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.ToString());

                await transaction?.RollbackAsync();

                return null;
            }
        }

        async ValueTask<object[][]> SelectFromApi(IQueryBuilder queryBuilder, ApiRequest apiRequest)
        {
            try
            {
                var connection = await CreateConnection();
                var sqlCommand = await CreateSqlCommand(connection, null, apiRequest);

                using var dataReader = await sqlCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                return entityBuilder.VerifyDatabaseSchema(dataReader, queryBuilder);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, ex.ToString());

                return null;
            }
        }

        // Used for select queries from api clients only.
        public ValueTask<object[][]> ProcessApiRequest(ApiRequest apiRequest)
        {
            if (apiRequest.IsSelectQuery)
                return SelectFromApi(CacheManager.Instance.GetQueryBuilder(apiRequest.EntityName), apiRequest);
            else
                return ExecuteFromApi(CacheManager.Instance.GetQueryBuilder(apiRequest.EntityName), apiRequest);
        }
    }
}
