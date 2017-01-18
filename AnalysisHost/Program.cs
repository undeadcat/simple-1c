using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Simple1C.AnalysisHost.Contracts;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.SqlAccess.Parsing;
using Simple1C.Impl.Sql.Translation;
using Npgsql;

namespace Simple1C.AnalysisHost
{
    public class Program
    {
        //TODO. cache query parser. don't recreate?
        //TODO. long queries. batches. async execution.
        public static void Main(string[] args)
        {
            var parameters = NameValueCollectionHelpers.ParseCommandLine(args);
            var port = int.Parse(parameters.Get("port"));
            if (parameters["debug"] != null)
                Debugger.Launch();
            var server = new SimpleHttpServer(port);
            server.JsonHandler<TranslationRequest, TranslationResult>("translate", Translate);
            server.JsonHandler<string>("testConnection", TestConnection);
            server.JsonHandler<ExecuteQueryRequest, QueryResult>("executeQuery", ExecuteQuery);
            server.JsonHandler<DbRequest, List<string>>("listTables", ListTables);
            server.JsonHandler<SchemaRequest, TableMappingDto>("tableMapping", GetTable);
            server.Start();
        }

        private static List<string> ListTables(DbRequest arg)
        {
            return SchemaStore(arg.ConnectionString).ListTables();
        }

        private static TableMappingDto GetTable(SchemaRequest arg)
        {
            var table = SchemaStore(arg.ConnectionString).ResolveTableOrNull(arg.TableName);
            if (table == null)
                return null;
            return ConvertTable(table);
        }

        private static void TestConnection(string connectionString)
        {
            Db(connectionString).ExecuteInt("select 1");
        }

        private static TranslationResult Translate(TranslationRequest arg)
        {
            var queryTranslator = Translator(arg.ConnectionString);
            try
            {
                return new TranslationResult
                {
                    Result = queryTranslator.Translate(arg.Query)
                };
            }
            catch (ParseException e)
            {
                return new TranslationResult
                {
                    Error = e.Message
                };
            }
        }

        private static QueryResult ExecuteQuery(ExecuteQueryRequest arg)
        {
            var db = Db(arg.ConnectionString);
            var rows = new List<object[]>();
            return db.ExecuteWithResult(arg.Query, new object[0], command =>
            {
                var reader = command.ExecuteReader();
                var columns = DatabaseHelpers.GetColumns((NpgsqlDataReader)reader);
                while (reader.Read())
                {
                    var row = new object[columns.Length];
                    reader.GetValues(row);
                    rows.Add(row);
                }
                return new QueryResult
                {
                    Columns = columns.Select(col => new QueryResult.Column
                    {
                        Name = col.ColumnName,
                        MaxLength = col.MaxLength,
                        DataType = col.DataType.ToString()
                    }).ToList(),
                    Rows = rows
                };
            });
        }

        private static QueryToSqlTranslator Translator(string connectionString)
        {
            return new QueryToSqlTranslator(SchemaStore(connectionString), new int[0]);
        }

        private static PostgreeSqlSchemaStore SchemaStore(string connectionString)
        {
            return new PostgreeSqlSchemaStore(Db(connectionString));
        }

        private static PostgreeSqlDatabase Db(string connectionString)
        {
            return new PostgreeSqlDatabase(connectionString);
        }

        private static TableMappingDto ConvertTable(TableMapping mapping)
        {
            return new TableMappingDto
            {
                Name = mapping.QueryTableName,
                Type = mapping.Type,
                Properties = mapping.Properties
                    .Select(t => new PropertyMappingDto
                    {
                        Name = t.PropertyName,
                        Tables = t.SingleLayout != null
                            ? new[] {t.SingleLayout.NestedTableName}
                            : t.UnionLayout.NestedTables
                    })
                    .ToArray()
            };
        }
    }
}