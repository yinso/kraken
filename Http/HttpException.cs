using System;
using System.Net;

namespace Kraken.Http
{

    public class HttpException : Exception
    {
        public int StatusCode { get ; protected set; }

        public HttpException(int statusCode) : base() {
            StatusCode = statusCode;
        }

        public HttpException(int statusCode, string msg) : base(msg)
        {
            StatusCode = statusCode;
        }

        public HttpException(int statusCode, string msg, Exception inner) : base(msg)
        {
            StatusCode = statusCode;
        }

        public static HttpException Raise(int statusCode)
        {
            throw new HttpException(statusCode);
        }
    }
}

