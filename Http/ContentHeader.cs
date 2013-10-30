using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Http
{
    public class ContentHeader
    {
        string name;
        string value;
        NameValueCollection parameters = new NameValueCollection();
        public NameValueCollection Parameters { get { return parameters; } }
        public ContentHeader(string line)
        {
            Regex regex = new Regex(@"^\s*Content-([^\:]+)\:\s*([^\;]+)\s*(\;\s*([^\;]+)\s*)*$", RegexOptions.IgnoreCase);
            Match match = regex.Match(line);
            if (match.Success)
            {
                name = match.Groups[1].Value;
                value = match.Groups[2].Value;
                for (int i = 4; i < match.Groups.Count; ++i) {
                    parseParam(match.Groups[i].Value);
                }
            } else
            {

            }
        }

        void parseParam(string keyval)
        {
            Regex regex = new Regex(@"^([^\=]+)\=(.+)$");
            Match match = regex.Match(keyval);
            if (match.Success)
            {
                parameters.Add(match.Groups[1].Value, match.Groups[2].Value);
            }
        }

        public override string ToString()
        {
            return string.Format("[ContentHeader: Parameters={0}]", Parameters);
        }
    }
}

