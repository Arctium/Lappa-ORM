// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Lappa.ORM.Constants;
using Microsoft.Extensions.DependencyModel;

namespace Lappa.ORM
{
    internal sealed class Connector
    {
        internal ConnectorSettings Settings { get; set; }
        internal ConnectorQuery Query { get; private set; }

        Assembly assembly;
        Type connectionType;
        Type commandType;
        Type parameterType;

        internal void Load()
        {
            Query = new ConnectorQuery(Settings.DatabaseType);

            // Use MSSql as default.
            var typeBase = "System.Data.SqlClient.Sql";

            switch (Settings.DatabaseType)
            {
                case DatabaseType.MSSql:
                {
                    assembly = Assembly.Load(new AssemblyName("System.Data.SqlClient, Version=4.5.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
                    break;
                }
                case DatabaseType.MySql:
                {
                    typeBase = "MySql.Data.MySqlClient.MySql";

                    if (!string.IsNullOrEmpty(Settings.ConnectorPath))
                        assembly = Assembly.LoadFrom(Settings.ConnectorPath);
                    else
                    {
                        var mysqlAssemblyNames = DependencyContext.Default.GetDefaultAssemblyNames().Where(asm => asm.Name.StartsWith("MySql.Data") ||
                                                                                                                       asm.Name.StartsWith("MySqlConnector"));
                        // Let's throw a type load exception if no supported MySql lib is found.
                        if (mysqlAssemblyNames.Count() == 0)
                            throw new TypeLoadException("No assembly referencing 'MySql' found.");

                        if (mysqlAssemblyNames.Count() > 1)
                            throw new NotSupportedException("Multiple assemblies referencing 'MySql' found.");

                        assembly = Assembly.Load(mysqlAssemblyNames.First());
                    }

                    break;
                }
                case DatabaseType.SQLite:
                {
                    typeBase = "Microsoft.Data.Sqlite.Sqlite";
                    assembly = Assembly.Load(new AssemblyName("Microsoft.Data.Sqlite, Version=2.1.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60"));

                    break;
                }
                case DatabaseType.Oracle:
                    throw new NotSupportedException("Oracle is not supported.");
                case DatabaseType.PostgreSql:
                {
                    typeBase = "Npgsql";

                    if (!string.IsNullOrEmpty(Settings.ConnectorPath))
                        assembly = Assembly.LoadFrom(Settings.ConnectorPath);
                    else
                    {
                        var npgsqlAssemblyNames = DependencyContext.Default.GetDefaultAssemblyNames().Where(asm => asm.Name.StartsWith("Npgsql"));

                        // Let's throw a type load exception if no supported Npgsql lib is found.
                        if (npgsqlAssemblyNames.Count() == 0)
                            throw new TypeLoadException("No assembly referencing 'Npgsql' found.");

                        if (npgsqlAssemblyNames.Count() > 1)
                            throw new NotSupportedException("Multiple assemblies referencing 'Npgsql' found.");

                        assembly = Assembly.Load(npgsqlAssemblyNames.First());
                    }

                    break;
                }
                default:
                    break;
            }

            connectionType = assembly.GetType($"{typeBase}Connection");
            commandType = assembly.GetType($"{typeBase}Command");
            parameterType = assembly.GetType($"{typeBase}Parameter");

            if (connectionType == null || commandType == null || parameterType == null)
                throw new TypeLoadException($"connectionType: {connectionType}, commandType: {commandType}, parameterType: {parameterType}.");
        }

        public DbConnection CreateConnectionObject() => Activator.CreateInstance(connectionType) as DbConnection;
        public DbCommand CreateCommandObject() => Activator.CreateInstance(commandType) as DbCommand;
        public DbParameter CreateParameterObject() => Activator.CreateInstance(parameterType) as DbParameter;
    }
}
