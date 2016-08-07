// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Reflection;
using LappaORM.Constants;

namespace LappaORM
{
    internal sealed class Connector
    {
        Assembly assembly;
        Type connectionType;
        Type commandType;

        internal Connector(DatabaseType dbType)
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
                    typeBase = "";
                    //assembly = Assembly.Load(new AssemblyName(""));

                    throw new NotSupportedException("MySQL is not supported.");
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
