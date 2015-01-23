// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    public partial class Database : IDisposable
    {
        ConnectorSettings connSettings;
        QuerySettings querySettings;
        DbConnection connection;
        IDatabase db;

        public bool CreateConnection(string connString, ConnectionType type = ConnectionType.MYSQL)
        {
            connSettings = new ConnectorSettings(type);
            querySettings = new QuerySettings(type);

            if (type == ConnectionType.MYSQL)
                db = new MySqlDatabase(this);
            else if (type == ConnectionType.MSSQL)
                db = new MSSqlDatabase(this);

            try
            {
                connection = connSettings.CreateObject("Connection") as DbConnection;
                connection.ConnectionString = connString;

                connection.Open();
            }
            catch
            {
                return false;
            }

            return connection.State == ConnectionState.Open;
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand CreateSqlCommand(string sql, params object[] args)
        {
            var sqlCommand = connSettings.CreateObject("Command") as DbCommand;

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
                var cmd = CreateSqlCommand(sql, args);

                return await cmd.ExecuteNonQueryAsync() > 0;
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
                var cmd = CreateSqlCommand(sql, args);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch
            {
                return false;
            }
        }

        internal async Task<DataTable> SelectAsync(string sql, string tableName, params object[] args)
        {
            try
            {
                var result = new DataTable();
                var adapter = connSettings.CreateObject("DataAdapter") as DbDataAdapter;

                adapter.SelectCommand = CreateSqlCommand(sql, args);
                adapter.SelectCommand.CommandTimeout = 2147483;

                result.TableName = tableName;

                var fillTask = adapter.FillAsync(result);

                return await fillTask.ContinueWith(res =>
                {
                    adapter.Dispose();

                    return result;
                });
            }
            catch
            {
                return null;
            }
        }

        internal DataTable Select(string sql, string tableName, params object[] args)
        {
            try
            {
                var result = new DataTable();
                var adapter = connSettings.CreateObject("DataAdapter") as DbDataAdapter;

                adapter.SelectCommand = CreateSqlCommand(sql, args);
                adapter.SelectCommand.CommandTimeout = 2147483;

                result.TableName = tableName;

                adapter.Fill(result);

                return result;
            }
            catch
            {
                return null;
            }
        }

        #region IDisposable Support
        bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    connection.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
