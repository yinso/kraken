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

        public HttpRouteTable RouteTable { get; private set; }
        // we ought to be able to hookup all responses
        public HttpServer(params string[] prefixes)
        {
            RouteTable = new HttpRouteTable();
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
        }

        static void defaultSplat(HttpContext ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.SetOutput(string.Format("<html><body>URL: {0}</body></html>", ctx.Request.RawUrl));
        }

        static void defaultNotFound(HttpContext ctx)
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.SetOutput("");
        }

        // Keep-Alive is not implemented at this time.
        static void defaultServerError(HttpContext ctx)
        {
            if (ctx.Error != null)
            {
                if (ctx.Error is HttpException) {
                    ctx.Response.StatusCode = (ctx.Error as HttpException).StatusCode;
                } else {
                    ctx.Response.StatusCode = 500;
                }
                ctx.Response.SetOutput(string.Format("<html><body><h3>Error</h3><p>Error: {0}</p></body></html>", ctx.Error));
            } else
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.SetOutput("");
            }
        }

        static void defaultContext(HttpContext ctx) {
            ctx.Response.StatusCode = 200;
            ctx.Response.SetOutput("<html><body>OK</body></html>");
        }

        public void AddRoute(string method, string url, HttpCallback callback)
        {
            RouteTable.AddRoute(method, url, callback);
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
            HttpContext context = new HttpContext(this, c as HttpListenerContext);
            context.Response.Headers["Server"] = "Kraken/0.1";
            try {
                HttpRouteMatch match = RouteTable.Match(context);
                if (match.IsSuccess) {
                    context.UrlParams = match.UrlParams;
                    match.Callback(context);
                } else {
                    defaultNotFound(context);
                }
            } catch (Exception e) {
                if (e is HttpException) {
                    Console.WriteLine("HTTP Code: {0}", (e as HttpException).StatusCode, e.Message);
                } else {
                    Console.WriteLine("REQUEST ERROR: {0}", e);
                }
                context.Error = e;
                defaultServerError(context);
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

