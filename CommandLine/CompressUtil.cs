using System;
using System.IO;
using System.IO.Compression;

namespace Kraken.CommandLine
{
    public class CompressUtil
    {
        private CompressUtil()
        {
        }

        // the goal is to compress a file into another file.
        // basics would be string to string.
        public static void Compress(Stream source, Stream dest)
        {
            using (GZipStream gs = new GZipStream(dest, CompressionMode.Compress))
            {
                source.CopyTo(dest);
            }
        }

        // from http://stackoverflow.com/questions/230128/best-way-to-copy-between-two-stream-instances
        // http://stackoverflow.com/questions/1540658/net-asynchronous-stream-read-write
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write (buffer, 0, read);
            }
        }

        public static byte[] CompressString (string text)
        {
            byte[] result;
            using (MemoryStream s = new MemoryStream()) {
                using (GZipStream gs = new GZipStream(s, CompressionMode.Compress)) {
                    using (StreamWriter writer = new StreamWriter(gs)) {
                        writer.Write(text);
                    }
                }
                result = s.ToArray();
            }
            return result;
        }
        
        public static string DecompressString (byte[] compressed)
        {
            string result;
            using (MemoryStream s = new MemoryStream(compressed)) {
                using (GZipStream gs = new GZipStream(s, CompressionMode.Decompress)) {
                    using (StreamReader reader = new StreamReader(gs)) {
                        result = reader.ReadToEnd();
                    }
                }
            }
            return result;
        }
    }
}

