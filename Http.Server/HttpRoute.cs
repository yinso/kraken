using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Kraken.HttpServer
{
    public delegate void HttpCallback(HttpListenerContext context);

    public class Context {

        HttpListenerContext context;

        public Context(HttpListenerContext context) {
            this.context = context;
        }

    }

    public class HttpRouteMatch {
        public Dictionary<string, string> UrlParams { get; private set; }
        public HttpCallback Callback { get; internal set; }
        public bool IsSuccess { get; internal set; }
        internal HttpRouteMatch() {
            UrlParams = new Dictionary<string, string>();
            Callback = HttpRouteMatch.defaultMethod;
            IsSuccess = false;
        }

        static void defaultMethod(HttpListenerContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentLength64 = 0;
            context.Response.OutputStream.Close();
        }
    }

    internal class UrlSegmentMatcher {
        public string Segment { get; private set; }

        public bool IsParameter { get; private set; }


        public UrlSegmentMatcher(string seg)
        {
            if (seg.StartsWith(":"))
            {
                IsParameter = true;
                Segment = seg.Substring(1);
            } else
            {
                IsParameter = false;
                Segment = seg;
            }
        }

        public bool Match(string seg, HttpRouteMatch match)
        {
            if (IsParameter)
            {
                match.UrlParams.Add(Segment, seg);
                return true;
            } else 
            {
                if (seg == Segment)
                    return true;
                else
                    return false;
            }
        }
    }

    public class HttpRoute
    {
        string method;
        string url;
        HttpCallback callback;
        List<UrlSegmentMatcher> matchers = new List<UrlSegmentMatcher>();
        Dictionary<string, UrlSegmentMatcher> urlParams = new Dictionary<string, UrlSegmentMatcher>();
        // we'll support similar routing as express.
        public HttpRoute(string method, string url, HttpCallback callback)
        {
            this.method = method;
            this.url = url;
            this.callback = callback;
            parseUrl(); // at the same time we'll add params...
        }

        void parseUrl()
        {
            string[] segments = url.Split(new char[]{'/'});
            foreach (string seg in segments)
            {
                UrlSegmentMatcher matcher = new UrlSegmentMatcher(seg);
                matchers.Add(matcher);
                if (matcher.IsParameter) {
                    if (urlParams.ContainsKey(matcher.Segment)) {
                        throw new Exception(string.Format("duplicate_url_route_segment: {0} in {1}", matcher.Segment, url));
                    } else {
                        urlParams[matcher.Segment] = matcher;
                    }
                }
            }
        }

        // do we return something that has the following.
        public HttpRouteMatch Match(HttpListenerContext context) {
            HttpRouteMatch match = new HttpRouteMatch();
            if (method.ToLower() != context.Request.HttpMethod.ToLower()) 
                return match;
            urlMatch(context.Request.RawUrl, match);
            if (match.IsSuccess)
                match.Callback = this.callback;
            return match;
        }

        void urlMatch(string url, HttpRouteMatch match) {
            // the first thing we do is to split the url into segments.
            string[] segments = url.Split(new char[]{'/'});
            // the segments need to have the same length.
            if (segments.Length != this.matchers.Count)
                return;
            // once the segments are the same length... it's time to iterate through them.
            for (int i = 0; i < this.matchers.Count; ++i) {
                bool result = matchers[i].Match(segments[i], match);
                if (!result)
                    return;
            }
            match.IsSuccess = true;
        }
    }

    public class HttpRouteTable {
        List<HttpRoute> routes = new List<HttpRoute>();
        public HttpRouteTable() {

        }

        public void AddRoute(string method, string url, HttpCallback callback) {
            routes.Add(new HttpRoute(method, url, callback));
        }

        public HttpRouteMatch Match(HttpListenerContext context)
        {
            foreach (HttpRoute route in routes)
            {
                HttpRouteMatch match = route.Match(context);
                if (match.IsSuccess) {
                    return match;
                }
            }
            HttpRouteMatch failed = new HttpRouteMatch();
            return failed;
        }
    }
}

