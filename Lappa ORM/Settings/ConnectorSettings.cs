// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

namespace Lappa_ORM
{
    internal sealed class ConnectorSettings
    {
        public ConnectionType ConnectionType { get; private set; }
        string type;
        Assembly assembly;

        internal ConnectorSettings(ConnectionType cType)
        {
            ConnectionType = cType;

            if (cType == ConnectionType.MySql)
            {
                if (!File.Exists(Environment.CurrentDirectory + "/MySql.Data.dll"))
                {
                    Console.WriteLine("MySql.Data.dll doesn't exist.");

                    return;
                }

                assembly = Assembly.LoadFile(Environment.CurrentDirectory + "/MySql.Data.dll");
                type = "MySql.Data.MySqlClient.MySql";
            }
            else if (cType == ConnectionType.MSSql)
            {
                // System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
                #pragma warning disable 612 // For mono...
                #pragma warning disable 618 // Visual studio...
                assembly = Assembly.LoadWithPartialName("System.Data");
                #pragma warning restore 612
                #pragma warning restore 618

                type = "System.Data.SqlClient.Sql";
            }
            else if (cType == ConnectionType.SQLite)
            {
                if (!File.Exists(Environment.CurrentDirectory + "/System.Data.SQLite.dll"))
                {
                    Console.WriteLine("System.Data.SQLite.dll doesn't exist.");

                    return;
                }

                assembly = Assembly.LoadFile(Environment.CurrentDirectory + "/System.Data.SQLite.dll");
                type = "System.Data.SQLite.SQLite";
            }
        }

        internal object CreateObject(string classPart)
        {
            return Activator.CreateInstance(assembly.GetType(type + classPart));
        }
    }
}
