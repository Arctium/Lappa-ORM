// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using LappaORM.Constants;
using LappaORM.Misc;

namespace LappaORM
{
    internal sealed class Connector
    {
        Assembly assembly;
        Type connectionType;
        Type commandType;

        internal Connector(DatabaseType dbType, string connectorFileName = null)
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
                    assembly = new AssemblyLoader().LoadFromAssemblyPath($"{AppContext.BaseDirectory}/{connectorFileName ?? "MySql.Data.dll"}");
                    break;
                }
                case DatabaseType.SQLite:
                {
                    typeBase = "";
                    //assembly = Assembly.Load(new AssemblyName(""));

                    throw new NotSupportedException("SQLite is not supported.");
                }
                default:
                    break;
            }

            connectionType = assembly.GetType($"{typeBase}Connection");
            commandType = assembly.GetType($"{typeBase}Command");
        }

        public DbConnection CreateConnectionObject() => Activator.CreateInstance(connectionType) as DbConnection;
        public DbCommand CreateCommandObject() => Activator.CreateInstance(commandType) as DbCommand;
    }
}
