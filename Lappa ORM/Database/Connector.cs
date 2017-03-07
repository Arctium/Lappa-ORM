// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Reflection;
using LappaORM.Constants;
using LappaORM.Misc;

namespace LappaORM
{
    internal sealed class Connector
    {
        public string FilePath { get; set; } = null;
        public string FileName { get; set; } = null;

        Assembly assembly;
        Type connectionType;
        Type commandType;
        Type parameterType;

        internal void Load(DatabaseType dbType, bool loadFromFile)
        {
            // Use MSSQL as default.
            var typeBase = "System.Data.SqlClient.Sql";

            switch (dbType)
            {
                case DatabaseType.MSSql:
                {
                    assembly = Assembly.Load(new AssemblyName("System.Data.SqlClient, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
                    break;
                }
                case DatabaseType.MySql:
                {
                    typeBase = "MySql.Data.MySqlClient.MySql";

                    if (loadFromFile)
                        assembly = new AssemblyLoader().LoadFromAssemblyPath($"{FilePath ?? AppContext.BaseDirectory}/{FileName ?? "MySqlConnector.dll"}");
                    else
                        assembly = Assembly.GetEntryAssembly();

                    break;
                }
                case DatabaseType.SQLite:
                {
                    typeBase = "Microsoft.Data.Sqlite.Sqlite";
                    assembly = Assembly.Load(new AssemblyName("Microsoft.Data.Sqlite, Version=1.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60"));

                    break;                    
                }
                default:
                    break;
            }

            connectionType = assembly.GetType($"{typeBase}Connection");
            commandType = assembly.GetType($"{typeBase}Command");
            parameterType = assembly.GetType($"{typeBase}Parameter");

            if (connectionType == null || commandType == null || parameterType == null)
                throw new TypeLoadException($"Can't find '{typeBase}Connection' or '{typeBase}Command' or '{typeBase}Parameter'.");
        }

        public DbConnection CreateConnectionObject() => Activator.CreateInstance(connectionType) as DbConnection;
        public DbCommand CreateCommandObject() => Activator.CreateInstance(commandType) as DbCommand;
        public DbParameter CreateParameterObject() => Activator.CreateInstance(parameterType) as DbParameter;
    }
}
