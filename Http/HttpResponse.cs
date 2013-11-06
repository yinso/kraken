using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Kraken.Http
{
    public class HttpResponse
    {
        HttpListenerResponse inner;
        public HttpResponse(HttpListenerResponse inner)
        {
            this.inner = inner;
        }

        public int StatusCode
        { 
            get
            {
                return inner.StatusCode;
            }
            set
            {
                inner.StatusCode = value;
            }
        }

        public string ContentType
        {
            get
            {
                return inner.ContentType;
            }
            set
            {
                inner.ContentType = value;
            }
        }

        public NameValueCollection Headers {
            get {
                return inner.Headers;
            }
        }

        public void SetOutput(string fmt, params object[] args) {
            SetOutput(Encoding.UTF8.GetBytes(string.Format(fmt, (object[])args)));
        }

        public void SetOutput(byte[] bytes) {
            inner.ContentLength64 = bytes.Length;
            inner.OutputStream.Write(bytes, 0, bytes.Length);
            inner.OutputStream.Close();
        }

        public void SetOutput(Stream s)
        {
            if (!s.CanRead)
                throw new Exception("http_resposne_setoutput_stream_not_readable");
            if (!s.CanSeek) // we'll have to chunk the response.
            {
                inner.SendChunked = true;
                s.CopyTo(inner.OutputStream);
                inner.OutputStream.Close();
            } else
            {
                inner.ContentLength64 = s.Length;
                s.CopyTo(inner.OutputStream);
                inner.OutputStream.Close();
            }
        }

        public void Respond(int statusCode) {
            Respond(statusCode, "");
        }

        public void Respond(int statusCode, string fmt, params object[] args) {
            StatusCode = statusCode;
            SetOutput(string.Format(fmt, (object[])args));
        }
    }
}

