using System.Collections.Generic;
using Simple1C.Impl.Sql.SchemaMapping;

namespace Simple1C.AnalysisHost.Contracts
{
    internal class DbRequest
    {
        public string ConnectionString { get; set; }
    }

    internal class TranslationRequest : DbRequest
    {
        public string Query { get; set; }
    }

    internal class TranslationResult
    {
        public string Result { get; set; }
        public string Error { get; set; }
    }

    internal class ExecuteQueryRequest : DbRequest
    {
        public string Query { get; set; }
    }

    internal class QueryResult
    {
        public List<Column> Columns { get; set; }
        public List<object[]> Rows { get; set; }

        public class Column
        {
            public string Name { get; set; }
            public int MaxLength { get; set; }
            public string DataType { get; set; }
        }
    }

    internal class SchemaRequest : DbRequest
    {
        public string TableName { get; set; }
    }

    internal class TableMappingDto
    {
        public string Name { get; set; }
        public TableType Type { get; set; }
        public PropertyMappingDto[] Properties { get; set; }
    }

    internal class EnumDto
    {
        public string EnumName { get; set; }
        public string ValueName { get; set; }
    }

    internal class PropertyMappingDto
    {
        public string Name { get; set; }
        public string[] Tables { get; set; }
    }
}