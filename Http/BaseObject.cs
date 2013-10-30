using System;
using System.Text;

namespace Http
{
    public class BaseObject
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public Headers Headers { get; internal set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public BaseObject()
        {
            MajorVersion = 1;
            MinorVersion = 1;
            Headers = new Headers();
        }

        public void Initialize() {
            ContentType = Parser.ParseContentType(Headers.Get("content-type"));
            ContentLength = Parser.ParseContentLength(Headers.Get("content-length"));
        }

        public string CRLF
        {
            get
            {
                return "\r\n";
            }
        }

        public byte[] CRLFBytes
        {
            get
            {
                return Encoding.ASCII.GetBytes(CRLF);
            }
        }
    }
}

