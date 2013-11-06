using System;
using System.Collections.Generic;
using System.Text;

namespace WebDav
{
    /// <summary>
    /// Response.
    /// 
    /// Represents a WebDav response
    /// </summary> 
    public class Response
    {
        public string Url { get; set; }
        public string Description { get ; set; }
        public List<PropStat> PropStatList { get ; private set; } 
        public Response()
        {
            PropStatList = new List<PropStat>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<D:response>");
            sb.AppendFormat("<D:href>{0}</D:href>", Url);
            foreach (PropStat propStat in PropStatList)
            {
                sb.Append(propStat.ToString());
            }
            sb.AppendFormat("<D:responsedescription>{0}</D:responsedescription>", Description);
            sb.Append("</D:response>");
            return sb.ToString();
        }
    }
}

