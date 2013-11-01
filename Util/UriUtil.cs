using System;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace Kraken.Util
{
    public class UriUtil
    {
        public UriUtil()
        {
        }
        public static string URLDecode(string s) {
            return Uri.UnescapeDataString(s);
        }
        
        public static string URLEncode(string s) {
            return Uri.EscapeDataString(s);
        }
        
        public static string URLPlusEncode(string s) {
            return Regex.Replace(s, "\\+", "%2B");
        }
        
        public static string HexDecode(string input) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            int len = input.Length;
            int i = 0;
            int sIx = 0;
            while (true) {
                if (input[i] == '%') {
                    sb.Append(input.Substring(sIx, i - sIx));
                    string hc = input.Substring(i + 1, 2);
                    int hi = int.Parse(hc, System.Globalization.NumberStyles.HexNumber);
                    char c = (char)hi;
                    sb.Append(c);
                    
                    sIx = i + 3;
                    i = i + 2;
                }
                i++;
                if (i >= len) {
                    sb.Append(input.Substring(sIx));
                    break;
                }
            }
            return sb.ToString();
        }
        // do we store extended metadata with the path? this will mean that we should copy the metadata 
        public static NameValueCollection ParseQueryString(string query) {
            NameValueCollection nvc = new NameValueCollection();
            query = Regex.Replace(query, "^\\?", "");
            foreach (string vp in Regex.Split(query, "&")) {
                string[] singlePair = Regex.Split(vp, "=");
                if (singlePair.Length >= 2) {
                    Console.WriteLine("parseQS key/val => {0} = {1}", URLDecode(singlePair[0]), URLDecode(singlePair[1]));
                    nvc.Add(URLDecode(singlePair[0]), URLDecode(singlePair[1]));
                }
                else {
                    nvc.Add(URLDecode(singlePair[0]), string.Empty);
                }
            }
            return nvc;
        }    
    
        public static string NameValueCollectionToQueryString(NameValueCollection nvc) {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < nvc.Keys.Count; ++i) {
                string[] values = nvc.GetValues(nvc.Keys[i]);
                for (int j = 0; j < values.Length; ++j) {
                    if (i != 0 || j != 0) {
                        sb.Append("&");
                    }
                    sb.Append(string.Format("{0}={1}", URLEncode(nvc.Keys[i]), URLEncode(values[j])));
                }
            }
            return sb.ToString();
        }


    
    }
}

