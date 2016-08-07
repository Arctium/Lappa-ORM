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
            switch (dbType)
            {
                case DatabaseType.MSSql:
                {
                    assembly = Assembly.Load(new AssemblyName("System.Data.SqlClient, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));

                    var typeBase = "System.Data.SqlClient.Sql";

                    connectionType = assembly.GetType($"{typeBase}Connection");
                    commandType = assembly.GetType($"{typeBase}Command");

                    break;
                }
                case DatabaseType.MySql:
                {
                    throw new NotSupportedException("MySQL is not supported.");
                }
                case DatabaseType.SQLite:
                {
                    throw new NotSupportedException("MySQL is not supported.");
                }
                default:
                    break;
            }
        }

        public DbConnection CreateConnectionObject() => Activator.CreateInstance(connectionType) as DbConnection;
        public DbCommand CreateCommandObject() => Activator.CreateInstance(commandType) as DbCommand;
    }
}