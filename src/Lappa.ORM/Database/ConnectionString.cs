﻿// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Lappa.ORM.Constants;

namespace Lappa.ORM
{
    public class ConnectionString
    {
        // Database Settings.
        public DatabaseType Type { get; set; }

        // Connection Settings.
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public int Port { get; set; }

        // Pooling Settings.
        public (int Min, int Max)? Pooling { get; set; }

        // Misc Settings
        public string CharSet { get; set; } = "utf8";

        // This field is used for options that are not provided through this class.
        public string ExtraOptions { get; set; }

        public override string ToString()
        {
            return Type switch
            {
                DatabaseType.MySql => $"Server={Host};User Id={User};Port={Port};Password={Password};Database={Database};Pooling={Pooling.HasValue};Min Pool Size={Pooling?.Min};Max Pool Size={Pooling?.Max};CharSet={CharSet};{ExtraOptions}",
                DatabaseType.MSSql => $"Data Source={Host}; Initial Catalog = {Database}; User ID = {User}; Password = {Password};Pooling={Pooling.HasValue};Min Pool Size={Pooling?.Min};Max Pool Size={Pooling?.Max};{ExtraOptions}",
                DatabaseType.SQLite => $"Data Source={Host};Password={Password};Database={Database};Pooling={Pooling.HasValue};Min Pool Size={Pooling?.Min};Max Pool Size={Pooling?.Max};{ExtraOptions}",
                DatabaseType.PostgreSql => $"Server={Host};User Id={User};Port={Port};Password={Password};Database={Database};Pooling={Pooling.HasValue};Minimum Pool Size={Pooling?.Min};Maximum Pool Size={Pooling?.Max};{ExtraOptions}",
                _ => throw new NotSupportedException($"{Type}")
            };
        }
    }
}
