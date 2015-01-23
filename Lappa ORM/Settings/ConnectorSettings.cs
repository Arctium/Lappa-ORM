// Copyright (C) Arctium Software.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

            if (cType == ConnectionType.MYSQL)
            {
                assembly = Assembly.LoadFile(Environment.CurrentDirectory + "/MySql.Data.dll");
                type = "MySql.Data.MySqlClient.MySql";
            }
            else if (cType == ConnectionType.MSSQL)
            {
                // System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
                #pragma warning disable CS0618
                // TODO: <Insert justification for suppressing CS0618>
                assembly = Assembly.LoadWithPartialName("System.Data");

                type = "System.Data.SqlClient.Sql";
            }
        }

        internal object CreateObject(string classPart)
        {
            return Activator.CreateInstance(assembly.GetType(type + classPart));
        }
    }
}
