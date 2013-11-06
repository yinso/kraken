using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;

using Kraken.Util;

namespace Kraken.Http
{
    public class HttpRouteMatch {
        public NameValueCollection UrlParams { get; private set; }
        public HttpCallback Callback { get; internal set; }
        public bool IsSuccess { get; internal set; }
        internal HttpRouteMatch() {
            UrlParams = new NameValueCollection();
            Callback = HttpRouteMatch.defaultMethod;
            IsSuccess = false;
        }

        static void defaultMethod(HttpContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.SetOutput("");
        }
    }

    internal class UrlSegmentMatcher {
        public string Segment { get; private set; }

        public bool IsParameter { get; private set; }

        public bool IsLiteral
        {
            get { return !IsParameter; }
        }

        public bool IsSplat { get; private set; }

        public UrlSegmentMatcher Next { get; private set; }

        public UrlSegmentMatcher(string url)
        {
            initialize(url, new Dictionary<string, UrlSegmentMatcher>());
        }

        public UrlSegmentMatcher(string url, Dictionary<string, UrlSegmentMatcher> urlParams)
        {
            initialize(url, urlParams);
        }

        void initialize(string url, Dictionary<string, UrlSegmentMatcher> urlParams) {
            string[] urlSeg = segmentize(url);
            string seg = urlSeg[0];
            if (seg.StartsWith(":"))
            {
                IsParameter = true;
                Segment = seg.Substring(1);
                if (urlParams.ContainsKey(Segment)) {
                    throw new Exception(string.Format("duplicate_url_param: {0} in {1}", Segment, url));
                } else {
                    urlParams[Segment] = this;
                }
            } else if (seg.EndsWith("..."))
            { // this will consume everything after this point!
                IsParameter = true;
                IsSplat = true;
                Segment = seg.Substring(0, seg.Length - 3);
                if (urlParams.ContainsKey(Segment)) {
                    throw new Exception(string.Format("duplicate_url_param: {0} in {1}", Segment, url));
                } else {
                    urlParams[Segment] = this;
                }
                if (seg != url) 
                    throw new Exception(string.Format("splat_must_end_url_segment: {0} in {1}", seg, url));
            } else
            {
                IsParameter = false;
                Segment = seg;
            }
            if (urlSeg.Length > 1)
            {
                Next = new UrlSegmentMatcher(urlSeg[1]);
            }
        }

        string[] segmentize(string url)
        {
            return url.Split(new char[]{'/'}, 2);
        }

        public bool MatchUrl(string url, HttpRouteMatch match)
        {
            if (IsSplat)
            { 
                match.UrlParams[Segment] = url;
                return true;
            } else 
            {
                string[] segs = segmentize(url);
                if (IsParameter) {
                    match.UrlParams[Segment] = segs[0];
                } else if (segs[0] != Segment) {
                    return false;
                } 
                if (Next != null) {
                    if (segs.Length > 1) {
                        return Next.MatchUrl(segs[1], match);
                    } else {
                        return false;
                    }
                } else if (segs.Length > 1) {
                    return false;
                } else {
                    return true;
                }
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
        UrlSegmentMatcher matcher;
        //Dictionary<string, UrlSegmentMatcher> urlParams = new Dictionary<string, UrlSegmentMatcher>();
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
            matcher = new UrlSegmentMatcher(url);
        }

        public HttpRouteMatch Match(HttpContext context) {
            HttpRouteMatch match = new HttpRouteMatch();
            if (method.ToLower() != context.Request.Method.ToLower()) 
                return match;
            match.IsSuccess = matcher.MatchUrl(context.Request.Path, match);
            if (match.IsSuccess)
                match.Callback = this.callback;
            return match;
        }
    }

    public class HttpRouteTable {

        List<HttpRoute> routes = new List<HttpRoute>();

        Dictionary<string, bool> supportedMethods = new Dictionary<string, bool>();

        public HttpRouteTable() {

        }

        public void AddRoute(string method, string url, HttpCallback callback) {
            routes.Add(new HttpRoute(method, url, callback));
            supportedMethods[method.ToLower()] = true;
        }

        public HttpRouteMatch Match(HttpContext context)
        {
            if (!supportedMethods.ContainsKey(context.Request.Method.ToLower()))
            {
                throw new HttpException(405);
            }
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

