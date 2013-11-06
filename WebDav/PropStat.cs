using System;
using System.Collections.Generic;
using System.Text;

namespace WebDav
{
    public class PropStat
    {
        public List<Property> PropList { get; private set; }
        public Dictionary<string, string> Namespace { get; private set; }
        public int StatusCode { get ; set ; }
        public PropStat()
        {
            PropList = new List<Property>();
            Namespace = new Dictionary<string, string>();
        }

        string getStatus() {
            return string.Format("<D:status>HTTP/1.1 {0} {1}</D:status>", StatusCode, "OK");
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<D:propstat><D:prop>");
            foreach (Property p in PropList)
            {
                sb.Append(p.ToString());
            }
            sb.Append("</D:prop>");
            sb.Append(getStatus());
            sb.Append("</D:propstat>");
            return sb.ToString();
        }
    }
}

