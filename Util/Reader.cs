using System;
using System.IO;
using System.Text;

namespace Kraken.Util
{
    public class Reader : IDisposable
    {
        public static byte CR = Convert.ToByte('\r');
        public static byte LF = Convert.ToByte('\n');
        public static byte COLON = Convert.ToByte(':');
        Stream stream;
        BinaryReader reader;
        public Reader(Stream s)
        {
            if (s is MemoryStream) 
                stream = s;
            else 
                stream = new Buffer(s);
                //stream = s;
            reader = new BinaryReader(stream);
        }

        public long Position
        {
            get
            {
                return stream.Position;
            }
        }

        public void Release()
        {
            if (stream is Buffer)
            {
                (stream as Buffer).Release();
            }
        }

        public int PeekChar()
        {
            return reader.PeekChar();
        }

        public char ReadChar() {
            return reader.ReadChar();
        }

        public byte[] ReadBytes(int count)
        {
            return reader.ReadBytes(count);
        }

        public int PeekByte() {
            int b = stream.ReadByte();
            stream.Position -= 1;
            return b;
        }

        public byte ReadByte()
        {
            return Convert.ToByte(stream.ReadByte());
        }

        public byte[] ReadLineBytes()
        {
            MemoryStream ms = new MemoryStream();
            byte current;
            while (PeekByte() != -1)
            {
                current = ReadByte();
                if (current == CR) {
                    int next = PeekByte();
                    if (next != -1) {
                        if (LF == Convert.ToByte(next)) {
                            ReadByte();
                            return ms.ToArray();
                        } else {
                            return ms.ToArray();
                        }
                    } else {
                        return ms.ToArray();
                    }
                } else if (current == LF) {
                    return ms.ToArray();
                } else {
                    ms.WriteByte(current);
                }
            }
            return ms.ToArray();
        }

        public string ReadLine()
        {
            StringBuilder builder = new StringBuilder ();
            char current;
            while (reader.PeekChar() != -1) {
                current = reader.ReadChar();
                if (current == '\r') {
                    int next = reader.PeekChar();
                    if (next != -1) {
                        char nextChar = Convert.ToChar(next);
                        if (nextChar == '\n') {
                            reader.ReadChar();
                            return builder.ToString();
                        } else {
                            return builder.ToString();
                        }
                    } else {
                        return builder.ToString();
                    }
                } else if (current == '\n') {
                    return builder.ToString();
                } else {
                    builder.Append(current);
                }
            }
            if (builder.Length == 0) { // nothign captured.
                return null;
            } else {
                return builder.ToString();
            }
        }

        public void Dispose()
        {
            //if (stream is BufferedStream)
            //{
            //    stream.Dispose();
            //}
        }
    }
}

