// Copyright (C) Arctium.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Lappa.ORM
{
    [DBTable(Name = "Table")]
    public class InformationSchemaTable : IEntity
    {
        [DBField(Name = "TABLE_CATALOG")]
        public string TableCatalog { get; set; }
        [DBField(Name = "TABLE_SCHEMA")]
        public string TableSchema { get; set; }
        [DBField(Name = "TABLE_NAME")]
        public string TableName { get; set; }
        [DBField(Name = "TABLE_TYPE")]
        public string TableType { get; set; }
        [DBField(Name = "ENGINE")]
        public string Engine { get; set; }
        [DBField(Name = "ROW_FORMAT")]
        public string RowFormat { get; set; }
        [DBField(Name = "TABLE_COLLATION")]
        public string TableCollation { get; set; }
        [DBField(Name = "CREATE_OPTIONS")]
        public string CreateOptions { get; set; }
        [DBField(Name = "TABLE_COMMENT")]
        public string TableComment { get; set; }
        [DBField(Name = "CREATE_TIME")]
        public DateTime? CreateTime { get; set; }
        [DBField(Name = "UPDATE_TIME")]
        public DateTime? UpdateTime { get; set; }
        [DBField(Name = "CHECK_TIME")]
        public DateTime? CheckTime { get; set; }
        [DBField(Name = "VERSION")]
        public int? Version { get; set; }
        [DBField(Name = "TABLE_ROWS")]
        public int? TableRows { get; set; }
        [DBField(Name = "AVG_ROW_LENGTH")]
        public int? AverageRowLength { get; set; }
        [DBField(Name = "DATA_LENGTH")]
        public int? DataLength { get; set; }
        [DBField(Name = "MAX_DATA_LENGTH")]
        public int? MaxDataLength { get; set; }
        [DBField(Name = "INDEX_LENGTH")]
        public int? IndexLength { get; set; }
        [DBField(Name = "DATA_FREE")]
        public int? DataFree { get; set; }
        [DBField(Name = "AUTO_INCREMENT")]
        public int? AutoIncrement { get; set; }
        [DBField(Name = "CHECKSUM")]
        public int? Checksum { get; set; }
    }
}
