// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Lappa_ORM.Misc;

namespace Lappa_ORM
{
    public partial class Database
    {
        string connectionString;
        ConnectorSettings connSettings;
        QuerySettings querySettings;
        IDatabase db;

        DbConnection CreateConnection()
        {
            var connection = connSettings.CreateObject("Connection") as DbConnection;

            connection.ConnectionString = connectionString;

            connection.Open();

            return connection;
        }

        public bool Initialize(string connString, ConnectionType type = ConnectionType.MySql)
        {
            connectionString = connString;
            connSettings = new ConnectorSettings(type);
            querySettings = new QuerySettings(type);

            if (type == ConnectionType.MySql)
                db = new MySqlDatabase(this);
            else if (type == ConnectionType.MSSql)
                db = new MSSqlDatabase(this);
            else if (type == ConnectionType.SQLite)
                db = new SQLiteDatabase(this);

            var isOpen = false;

            try
            {
                using (var connection = CreateConnection())
                    isOpen = connection.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }

            return isOpen;
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand CreateSqlCommand(DbConnection connection, string sql, params object[] args)
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
            catch
            {
                return false;
            }
        }

        internal async Task<DataTable> SelectAsync(string sql, string tableName, params object[] args)
        {
            try
            {
                Task<int> fillTask = null;

                var result = new DataTable { TableName = tableName };

                using (var connection = CreateConnection())
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                    {
                        using (var adapter = connSettings.CreateObject("DataAdapter") as DbDataAdapter)
                        {
                            adapter.SelectCommand = cmd;
                            adapter.SelectCommand.CommandTimeout = 2147483;

                            fillTask = adapter.FillAsync(result);
                        }
                    }
                }

                if (fillTask == null)
                    return null;

                return await fillTask.ContinueWith(res => { return result; });
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
                var result = new DataTable { TableName = tableName };

                using (var connection = CreateConnection())
                {
                    using (var cmd = CreateSqlCommand(connection, sql, args))
                    {
                        using (var adapter = connSettings.CreateObject("DataAdapter") as DbDataAdapter)
                        {
                            adapter.SelectCommand = cmd;
                            adapter.SelectCommand.CommandTimeout = 2147483;

                            adapter.Fill(result);
                        }
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
