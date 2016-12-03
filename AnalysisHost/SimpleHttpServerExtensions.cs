using System;
using System.Text;
using Newtonsoft.Json;
using Simple1C.AnalysisHost;

public static class SimpleHttpServerExtensions
{
    public static void JsonHandler<TInput>(this SimpleHttpServer server,
        string prefix,
        Action<TInput> handler)
    {
        server.JsonHandler<TInput, object>(prefix, input =>
        {
            handler(input);
            return null;
        });
    }

    public static void JsonHandler<TInput, TResult>(this SimpleHttpServer server,
        string prefix,
        Func<TInput, TResult> handler)
    {
        server.Handler(prefix, context =>
        {
            var input = JsonConvert.DeserializeObject<TInput>(Encoding.UTF8.GetString(context.Request.Body));
            Console.WriteLine(JsonConvert.SerializeObject(input));
            var result = handler(input);

            Console.WriteLine(JsonConvert.SerializeObject(result));
            context.Response.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result));
        });
    }
}