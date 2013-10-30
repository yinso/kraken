using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Http
{
    public class Headers : IEnumerable
    {
        private NameValueCollection col;
        public Headers()
        {
            col = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerator GetEnumerator() {
            return col.GetEnumerator();
        }

        public string[] AllKeys
        {
            get
            {
                return col.AllKeys;
            }
        }

        public string Get(string key)
        {
            return col.Get(key);
        }

        public string[] GetValues(string key) {
            return col.GetValues(key);
        }

        public void Add(string key, string val) {
            col.Add(key, val);
        }

        public void WriteTo(Stream s)
        {
            foreach (string key in col.AllKeys)
            {
                foreach (string val in col.GetValues(key)) {
                    string header = string.Format("{0}: {1}\r\n", key, val);
                    byte[] bytes = Encoding.ASCII.GetBytes(header);
                    s.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public void WriteToStringBuilder(StringBuilder builder)
        {
            foreach (string key in col.AllKeys) {
                foreach (string val in col.GetValues(key)) {
                    builder.AppendFormat("{0} => {1}\n", key, val);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            WriteToStringBuilder(builder);
            return builder.ToString();
        }

    }
}

