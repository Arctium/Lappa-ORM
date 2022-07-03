// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Lappa.ORM.Constants;

namespace Lappa.ORM
{
    public class ConnectionSettings
    {

        // Lappa ORM specific.
        public DatabaseType Type { get; set; }
        public ConnectionMode ConnectionMode { get; set; } = default;
        public string ApiHost { get; set; }

        // Connection Settings.
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        // Pooling Settings.
        public PoolingSettings Pooling { get; set; } = new();

        // Misc Settings
        public string CharSet { get; set; } = "utf8";
        public bool UseTransactions { get; set; }

        // This field is used for options that are not provided through this class.
        public string ExtraOptions { get; set; }

        // Overwrites the set field values with a hard coded connection string.
        public string ConnectionStringOverride { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionStringOverride))
                return ConnectionStringOverride;

            return Type switch
            {
                DatabaseType.SQLite => $"Data Source={Host};Password={Password};Database={Database};Pooling={Pooling.Enabled};{ExtraOptions}",
                DatabaseType.PostgreSql => $"Server={Host};User Id={User};Port={Port};Password={Password};Database={Database};Pooling={Pooling.Enabled};Minimum Pool Size={Pooling?.Min};Maximum Pool Size={Pooling?.Max};{ExtraOptions}",
                _ => throw new NotSupportedException($"{Type}")
            };
        }
    }
}
