using System;
using System.Text;
using Irony.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class QueryParser
    {
        private readonly Parser parser;

        public QueryParser()
        {
            var queryGrammar = new QueryGrammar();
            var languageData = new LanguageData(queryGrammar);
            if (languageData.Errors.Count > 0)
            {
                var b = new StringBuilder();
                foreach (var error in languageData.Errors)
                    b.Append(error);
                throw new InvalidOperationException(string.Format("invalid grammar\r\n{0}", b));
            }
            parser = new Parser(languageData);
        }

        public SqlQuery Parse(string source)
        {
            var parseTree = parser.Parse(source);
            if (parseTree.Status != ParseTreeStatus.Parsed)
                throw new ParseException(parseTree.ParserMessages,
                    ToTabs(parseTree.SourceText, parser.Context.TabWidth));
            var result = (SqlQuery) parseTree.Root.AstNode;
            new ColumnReferenceTableNameResolver().Visit(result);
            return result;
        }

        private static string ToTabs(string s, int tabSize)
        {
            return s.Replace("\t", new string(' ', tabSize));
        }
    }
}