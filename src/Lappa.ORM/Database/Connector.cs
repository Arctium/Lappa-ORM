// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Lappa.ORM.Constants;
using Lappa.ORM.Misc;
using Microsoft.Extensions.DependencyModel;

namespace Lappa.ORM
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
                    assembly = Assembly.Load(new AssemblyName("System.Data.SqlClient, Version=4.3.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));
                    break;
                }
                case DatabaseType.MySql:
                {
                    typeBase = "MySql.Data.MySqlClient.MySql";

                    if (loadFromFile)
                        assembly = new AssemblyLoader().LoadFromAssemblyPath($"{FilePath ?? AppContext.BaseDirectory}/{FileName ?? "MySqlConnector.dll"}");
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
                    assembly = Assembly.Load(new AssemblyName("Microsoft.Data.Sqlite, Version=1.1.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60"));

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
