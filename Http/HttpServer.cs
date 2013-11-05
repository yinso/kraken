using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

namespace Kraken.Http
{
    public class HttpServer
    {
        HttpListener inner;
        HttpRouteTable routeTable = new HttpRouteTable();
        // we ought to be able to hookup all responses
        public HttpServer(params string[] prefixes)
        {
            initialize(prefixes);
        }

        void initialize(string[] prefixes)
        {
            inner = new HttpListener();
            foreach (string prefix in prefixes)
            {
                inner.Prefixes.Add(prefix);
            }
            AddRoute("get", "/favicon.ico", HttpServer.defaultNotFound);
            AddRoute("get", "/", HttpServer.defaultContext);
            //AddRoute("get", "/path...", HttpServer.defaultSplat); // allow for matching the rest of the segments...
        }

        static void defaultSplat(HttpContext ctx)
        {
            byte[] response = Encoding.UTF8.GetBytes(string.Format("<html><body>URL: {0}</body></html>", ctx.Request.RawUrl));
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentLength64 = response.Length;
            ctx.Response.OutputStream.Write(response, 0, response.Length);
        }

        static void defaultNotFound(HttpContext ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.ContentLength64 = 0;
            //ctx.Response.OutputStream.Close();
        }

        // Keep-Alive is not implemented at this time.
        static void defaultServerError(HttpContext ctx)
        {
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentLength64 = 0;
            //ctx.Response.OutputStream.Close();
        }

        static void defaultContext(HttpContext ctx) {
            byte[] response = Encoding.UTF8.GetBytes("<html><body>OK</body></html>");
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentLength64 = response.Length;
            ctx.Response.OutputStream.Write(response, 0, response.Length);
            //ctx.Response.OutputStream.Close();
        }

        public void AddRoute(string method, string url, HttpCallback callback)
        {
            HttpCallback wrappedCallback = (HttpContext ctx) => {
                callback(ctx);
                ctx.Response.OutputStream.Close();
            };
            routeTable.AddRoute(method, url, wrappedCallback);
        }

        void startListening(object state) {
            try {
                HttpListenerContext context;
                while (inner.IsListening && (context = (HttpListenerContext)inner.GetContext()) != null) {
                    Console.WriteLine("{0} {1}", context.Request.HttpMethod, context.Request.RawUrl);
                    ThreadPool.QueueUserWorkItem(startProcessing, context);
                }
            } catch (Exception e) {
                Console.WriteLine("LISTENER ERROR: {0}", e);
            }
        }

        void startProcessing(object c)
        {
            var ctx = c as HttpListenerContext;
            try {
                HttpRouteMatch match = routeTable.Match(ctx);
                if (match.IsSuccess) {
                    match.Callback(new HttpContext(ctx, match.UrlParams));
                } else {
                    defaultNotFound(new HttpContext(ctx));
                }
            } catch (Exception e) {
                Console.WriteLine("REQUEST ERROR: {0}", e);
            } finally {
                
            }
        }

        public void Start() {
            inner.Start();
            ThreadPool.QueueUserWorkItem(startListening);
        }

        public void Stop() {
            inner.Stop();
        }
    }
}

