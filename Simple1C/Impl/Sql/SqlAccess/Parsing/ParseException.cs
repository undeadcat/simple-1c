using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class ParseException : Exception
    {
        public List<LogMessage> Errors { get; private set; }

        public ParseException(List<LogMessage> errors, string query)
            : base(FormatMessage(errors, query))
        {
            Errors = errors;
        }

        private static string FormatMessage(List<LogMessage> errors, string query)
        {
            var b = new StringBuilder();
            foreach (var message in errors)
            {
                b.AppendLine(string.Format("{0}: {1} at {2} in state {3}", message.Level, message.Message,
                    message.Location, message.ParserState));

                var theMessage = message;
                var lines = query
                    .Split(new[] {"\r\n", "\n"}, StringSplitOptions.None)
                    .Select((sourceLine, index) =>
                        index == theMessage.Location.Line
                            ? string.Format("{0}\r\n{1}|<-Here", sourceLine,
                                new string('_', theMessage.Location.Column))
                            : sourceLine);
                foreach (var line in lines)
                    b.AppendLine(line);
            }
            return string.Format("parse errors\r\n:{0}", b);
        }
    }
}