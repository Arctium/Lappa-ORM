﻿// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Lappa.ORM.Constants;

namespace Lappa.ORM
{
    public class ConnectorSettings
    {
        public ConnectionMode ConnectionMode { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public string DatabaseName { get; set; }
        public string ConnectionString { get; set; }
        public string ConnectorPath { get; set; }
        public bool UseTransactions { get; set; }

        public string ApiHost { get; set; }
    }
}
