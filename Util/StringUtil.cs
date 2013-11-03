using System;
using System.IO;
using System.Text;

namespace Kraken.Util
{
    public class StringUtil
    {
        private StringUtil()
        {
        }

        // this is not a full on parser util... that I should spend sometime develope/revive
        public static string ParseString(Stream s) {
            // we'll read one byte at a time for parsing...
            // what the heck happens when we end up blocking?
            StringBuilder builder = new StringBuilder();
            char quotedChar = '"';
            bool started = false;
            using (Reader reader = new Reader(s)) {
                while (reader.PeekByte() != -1) {
                    char c = reader.ReadChar();
                    if (c == '"' && !started) {
                        started = true;
                    } else if (c == '\'' && !started) {
                        quotedChar = c;
                        started = true;
                    } else if (c == '\\') { // an escape character... simple escape for now (i.e. no complex unicode escape).
                        if (reader.PeekByte() != -1) {
                            char next = reader.ReadChar();
                            if (next == 'r') {
                                builder.Append('\r');
                            } else if (next == 'n') {
                                builder.Append('\n');
                            } else if (next == 't') {
                                builder.Append('\t');
                            } else {
                                builder.Append(next);
                            }
                        }
                    } else if (c == quotedChar && started) { 
                        break;
                    } else {
                        builder.Append(c);
                    }
                }
            }
            return builder.ToString();
        }
    }
}

