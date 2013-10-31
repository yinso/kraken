using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Json;
using Kraken.Util;

namespace Http
{
    public class Parser
    {
        private Parser()
        {
        }

        public static Request ParseRequest(Stream s) {
            return ParseRequest(new Reader(s));
        }

        public static Request ParseRequest(Reader reader)
        {
            Request req = new Request();
            ParseRequestLine(req, reader);
            ParseHeaders(req, reader);
            req.Initialize();
            return req;
        }

        public static void ParseRequestLine(Request req, Reader reader) {
            // we just need to read the first line.
            string line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                throw new Exception("BAD_REQUEST");
            } else
            {
                Regex version = new Regex(@"^HTTP/(\d)\.(\d)\s+([^\s]+)\s+([^\s]+)\s*$", RegexOptions.IgnoreCase);
                Match match = version.Match(line);
                if (match.Success) {
                    // how to get the captured value?
                    req.MajorVersion = int.Parse(match.Groups[1].Value);
                    req.MinorVersion = int.Parse(match.Groups[2].Value);
                    req.Url = match.Groups[3].Value;
                    req.Method = match.Groups[4].Value;
                } else {
                    throw new Exception("Back HTTP Version");
                }
            }

        }

        public static Response ParseResponse(Stream s)
        {
            return ParseResponse(new Reader(s));
        }

        public static Response ParseResponse(Reader reader)
        {
            Response res = new Response();
            ParseResponseLine(res, reader);
            ParseHeaders(res, reader);
            res.Initialize();
            return res;
        }

        public static void ParseResponseLine(Response res, Reader reader)
        {
            // first parse the preamble.
            string line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                throw new Exception("BAD_RESPONSE");
            } else
            {
                Regex regex = new Regex(@"^HTTP/(\d)\.(\d)\s+(\d+)\s+(.+)$", RegexOptions.IgnoreCase);
                Match match = regex.Match(line);
                if (match.Success)
                {
                    res.MajorVersion = int.Parse(match.Groups[1].Value);
                    res.MinorVersion = int.Parse(match.Groups[2].Value);
                    res.StatusCode = int.Parse(match.Groups[3].Value);
                    res.Status = match.Groups[4].Value;
                }
            }
        }
        
        public static Headers ParseHeaders(Stream s)
        {
            return ParseHeaders(new Reader(s));
        }

        public static Headers ParseHeaders(Reader reader) {
            Headers headers = new Headers();
            return ParseHeaders(headers, reader);
        }

        public static Headers ParseHeaders(BaseObject obj, Reader reader)
        {
            return ParseHeaders(obj.Headers, reader);
        }

        public static Headers ParseHeaders(Headers headers, Reader reader) {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line == string.Empty) {
                    return headers;
                } else {
                    int index = line.IndexOf(":");
                    if (index > 0) {
                        string name = line.Substring(0, index).Trim();
                        string data = line.Substring(index + 1).Trim();
                        headers.Add(name, data);
                    } 
                }
            }
            return headers;
        }

        public static string ParseContentType(string contentType) {
            Regex regex = new Regex(@"^([^\;]+);\s");
            Match match = regex.Match(contentType);
            if (match.Success) {
                return match.Groups[1].Value;
            } else {
                return "application/octet-stream";
            }
        }

        public static long ParseContentLength(string contentLength)
        {
            Regex regex = new Regex(@"^(\d+)$");
            Match match = regex.Match(contentLength);
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            } else
            {
                return 0;
            }
        }

        public static void ParseBody(string contentType, long contentLength, bool chunked, Stream s)
        {
            Stream bodyStream;
            if (chunked)
            {
                bodyStream = new ChunkedStream(s);
            } else // can we safely rely on contentLength? in general I don't.
            {
                bodyStream = s;
            }

            // let's read to the end...
            if (contentType == "x-www-form-urlencoded") {

            } else if (contentType == "multipart/form-data") {

            } else if (contentType == "application/json") { // there are also multiple different types of json as well.
                JsonValue value = JsonValue.Load(bodyStream);
            } else if (contentType == "text/xml") { // there are many different type of XML to parse here...

            }
        }

    }
}

