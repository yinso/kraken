using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

using Kraken.Util;

namespace Kraken.Http
{
    public class HttpRequest
    {
        HttpListenerRequest inner;

        public HttpRequest(HttpListenerRequest inner)
        {
            this.inner = inner;
            Path = UriUtil.URLDecode(inner.Url.AbsolutePath);
        }

        public NameValueCollection Headers {
            get {
                return inner.Headers;
            }
        }

        public NameValueCollection Query {
            get {
                return inner.QueryString;
            }
        }

        public string Path { get; private set; }

        public string RawUrl { get { return inner.RawUrl; } }

        public CookieCollection Cookies
        {
            get { return inner.Cookies; }
        }

        public string ContentType
        {
            get { return inner.ContentType; } 
        }

        public Stream InputStream
        {
            get { return inner.InputStream; }
        }

        public string Method { get { return inner.HttpMethod; } }
    }
}

