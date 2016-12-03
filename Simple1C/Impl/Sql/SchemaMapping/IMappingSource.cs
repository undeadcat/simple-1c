using System.Collections.Generic;

namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal interface IMappingSource
    {
        TableMapping ResolveTableOrNull(string queryName);
        List<string> ListTables();
    }
}