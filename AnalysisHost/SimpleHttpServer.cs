using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace Simple1C.AnalysisHost
{
    public class SimpleHttpServer
    {
        private readonly HttpListener httpListener;
        private readonly ManualResetEvent waitHandle;

        private readonly List<Tuple<string, Action<SimpleContext>>> handlers
            = new List<Tuple<string, Action<SimpleContext>>>();

        public SimpleHttpServer(int port)
        {
            httpListener = new HttpListener
            {
                AuthenticationSchemes = AuthenticationSchemes.Anonymous,
                Prefixes = {string.Format("http://*:{0}/", port)}
            };
            waitHandle = new ManualResetEvent(false);
        }

        public void Start()
        {
            httpListener.Start();
            httpListener.BeginGetContext(Handle, httpListener);
            waitHandle.WaitOne();
        }

        public void Stop()
        {
            waitHandle.Set();
            httpListener.Stop();
        }

        public void Handler(string prefix, Action<SimpleContext> handler)
        {
            handlers.Add(Tuple.Create(prefix, handler));
        }

        private void Handle(IAsyncResult ar)
        {
            try
            {
                var listener = (HttpListener) ar.AsyncState;
                var context = listener.EndGetContext(ar);
                listener.BeginGetContext(Handle, listener);
                context.Response.SendChunked = false;
                try
                {
                    var sw = Stopwatch.StartNew();
                    var localPath = context.Request.Url.LocalPath.Trim('/');
                    var handler = handlers
                        .FirstOrDefault(c => localPath.StartsWith(c.Item1, StringComparison.InvariantCultureIgnoreCase));
                    if (handler == null)
                        throw new InvalidOperationException(string.Format("Handler not found for path [{0}]", localPath));
                    var simpleContext = ConvertContext(context);
                    handler.Item2(simpleContext);
                    context.Response.StatusCode = 200;
                    ApplyResponse(simpleContext.Response, context.Response);
                    Console.WriteLine("Processed request to [{0}] in [{1}]", context.Request.Url.PathAndQuery,
                        sw.Elapsed);
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = 500;
                    context.Response.OutputStream.WriteString(e.ToString());
                    Console.WriteLine("Exception processing request to [{0}] occured: {1}",
                        context.Request.Url.PathAndQuery, e);
                }
                context.Response.OutputStream.Flush();
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured: {0}", e);
            }
        }

        private void ApplyResponse(SimpleResponse simpleResponse, HttpListenerResponse listenerResponse)
        {
            listenerResponse.OutputStream.Write(simpleResponse.Body);
        }

        private static SimpleContext ConvertContext(HttpListenerContext context)
        {
            return new SimpleContext
            {
                Request = ConvertRequest(context.Request)
            };
        }

        private static SimpleRequest ConvertRequest(HttpListenerRequest request)
        {
            return new SimpleRequest
            {
                Body = request.InputStream.ReadToEnd()
            };
        }

        public class SimpleContext
        {
            public SimpleContext()
            {
                Response = new SimpleResponse
                {
                    Body = new byte[0]
                };
            }

            public SimpleRequest Request { get; set; }
            public SimpleResponse Response { get; set; }
        }

        public class SimpleRequest
        {
            public byte[] Body { get; set; }
        }

        public class SimpleResponse
        {
            public byte[] Body { get; set; }
        }
    }
}