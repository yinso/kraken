using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Http
{
    public class Response : BaseObject
    {
        public int StatusCode { get; set; }
        public string Status { get; set; }
        public Response() : base()
        {
        }

        public override string ToString()
        {
            return string.Format("HTTP/{0}.{1} {2} {3}\r\n{4}", MajorVersion, MinorVersion, StatusCode, Status, Headers.ToString());
        }
    }
}

