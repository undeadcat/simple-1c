using System;
using System.Collections.Generic;

namespace Simple1C.AnalysisHost.Contracts
{
    internal class TranslationRequest
    {
        public string ConnectionString { get; set; }
        public string Query { get; set; }
    }

    internal class TranslationResult
    {
        public string Query { get; set; }
        public List<ErrorMessage> Messages { get; set; }

        public class ErrorMessage
        {
            public int Offset { get; set; }
            public string Message { get; set; }
        }
    }

    internal class ExecuteQueryRequest
    {
        public string ConnectionString { get; set; }
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
}