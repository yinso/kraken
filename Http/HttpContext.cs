using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;

namespace Kraken.Http
{
    public delegate void HttpCallback(HttpContext context);

    /// <summary>
    /// 
    /// Handler will get this call.
    /// 
    /// We might want a more PHP-like feel to it??
    /// 
    /// context.Headers
    /// context.Post
    /// context.Get
    /// context.Params
    /// context.Files
    /// 
    /// 
    /// vs. 
    /// 
    /// context.Request.Headers
    /// context.Response.Headers
    /// context.Session
    /// 
    /// blah...
    /// 
    /// Obviously the latter method is more orthogonal but it is also lower level feel.
    /// 
    /// I.e. it'll be nice if we just return something from the handler, and automatically have it 
    /// being convert into the right type.
    /// 
    /// But that'll need to be built on top of the current design.
    /// 
    /// Hmm... If we optimize for speed of development, it's just simply wrapping on the context object until
    /// we'll need to replace everything... Let's do that for now.
    /// 
    /// </summary>
    public class HttpContext {

        HttpListenerContext context;

        public HttpRequest Request { get; private set; }

        public HttpResponse Response { get ; private set; }

        public NameValueCollection UrlParams { get; internal set; }

        public Exception Error { get; internal set; }

        public HttpServer Server { get; internal set; }

        public HttpContext(HttpServer server, HttpListenerContext context) : this(server, context, null, null) { }

        public HttpContext(HttpServer server, HttpListenerContext context, Exception err) : this(server, context, null, err) { }

        public HttpContext(HttpServer server, HttpListenerContext context, NameValueCollection col) : this(server, context, col, null) { }
        
        public HttpContext(HttpServer server, HttpListenerContext context, NameValueCollection urlParams, Exception error)
        {
            Server = server;

            this.context = context;

            Response = new HttpResponse(context.Response);

            Request = new HttpRequest(context.Request);

            if (urlParams == null)
            {
                UrlParams = new NameValueCollection();
            } else
            {
                UrlParams = urlParams;
            }
            Error = error;
        }
    }
    
}
