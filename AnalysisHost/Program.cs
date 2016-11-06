using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Npgsql;
using Simple1C.AnalysisHost.Contracts;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.SqlAccess.Parsing;
using Simple1C.Impl.Sql.Translation;

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
            server.RegisterHandler("translate", JsonHandler<TranslationRequest, TranslationResult>(Translate));
            server.RegisterHandler("testConnection", JsonHandler<string>(TestConnection));
            server.RegisterHandler("executeQuery", JsonHandler<ExecuteQueryRequest, QueryResult>(ExecuteQuery));
            server.Start();
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
                    Query = queryTranslator.Translate(arg.Query)
                };
            }
            catch (ParseException e)
            {
                return new TranslationResult
                {
                    Messages = e.Errors.Select(m => new TranslationResult.ErrorMessage
                    {
                        Message = m.Message,
                        Offset = 0
                    }).ToList()
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
                var columns = PostgreeSqlDatabase.GetColumns((NpgsqlDataReader) reader);
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

        private static Action<SimpleHttpServer.SimpleContext> JsonHandler<TInput>(Action<TInput> handler)
        {
            return JsonHandler<TInput, object>(input =>
            {
                handler(input);
                return null;
            });
        }

        private static Action<SimpleHttpServer.SimpleContext> JsonHandler<TInput, TResult>(Func<TInput, TResult> handler)
        {
            return context =>
            {
                var input = JsonConvert.DeserializeObject<TInput>(Encoding.UTF8.GetString(context.Request.Body));
                Console.WriteLine(JsonConvert.SerializeObject(input));
                var result = handler(input);

                Console.WriteLine(JsonConvert.SerializeObject(result));
                context.Response.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
            };
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
    }
}