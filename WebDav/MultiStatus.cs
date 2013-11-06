using System;
using System.Collections.Generic;
using System.Text;

namespace WebDav
{
    public class MultiStatus
    {
        public List<Response> ResponseList { get; private set; }
        public MultiStatus()
        {
            ResponseList = new List<Response>();
        }

        public void AddResponse(string url, string description)
        {
            Response resp = new Response();
            resp.Url = url;
            resp.Description = description;
        }

        public void AddPropStat(int idx, int statusCode, params Property[] props)
        {
            Response resp = ResponseList [idx];
            if (resp == null) 
                throw new Exception("multistatus_index_out_of_range");
            PropStat stat = new PropStat();
            stat.StatusCode = statusCode;
            foreach (Property prop in props)
            {
                stat.PropList.Add(prop);
            }
        }

        public void AddProperty(int respIdx, int propStatIdx, Property prop)
        {
            Response resp = ResponseList[respIdx];
            if (resp == null)
                throw new Exception("multistatus_index_out_of_range");
            PropStat stat = resp.PropStatList[propStatIdx];
            if (stat == null) 
                throw new Exception("multistatus_resp_index_out_of_range");
            stat.PropList.Add(prop);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<D:multistatus xmlns:D=\"DAV:\">");
            foreach (Response sp in ResponseList)
            {
                sb.Append(sp.ToString());
            }
            sb.Append("</D:multistatus>");
            return sb.ToString();
        }
    }
}

