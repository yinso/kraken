using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Http
{
    public class Request : BaseObject
    {
        string method;
        string url;

        public string Url
        {
            get { return url; }
            set { url = value; }
        }

        public string Method
        {
            get { return method; }
            set { method = value.ToUpper(); }
        }

        public Request() : base()
        {
            method = "GET";
            url = "/";
        }

        public string RequestLine
        {
            get
            {
                return string.Format("{0} {1} HTTP/{2}.{3}\r\n", method, url, MajorVersion, MinorVersion);
            }
        }

        public void WriteTo(Stream s)
        {
            byte[] requestLine = Encoding.ASCII.GetBytes(RequestLine);
            s.Write(requestLine, 0, requestLine.Length);
            Headers.WriteTo(s);
            s.Write(CRLFBytes, 0, CRLFBytes.Length);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(RequestLine);
            Headers.WriteToStringBuilder(builder);
            builder.Append(CRLF);
            return builder.ToString();
        }
    }
}

