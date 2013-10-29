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

        public static void Compress(string sourcePath, string destPath)
        {
            StreamUtil.FileToFile(sourcePath, destPath, Compress);
        }

        public static void Compress(Stream source, Stream dest)
        {
            using (GZipStream gs = new GZipStream(dest, CompressionMode.Compress))
            {
                source.CopyTo(gs);
                source.Flush();
                dest.Position = 0;
            }
        }

        public static void Decompress(string sourcePath, string destPath)
        {
            StreamUtil.FileToFile(sourcePath, destPath, Decompress);
        }

        public static void Decompress(Stream source, Stream dest)
        {
            using (GZipStream gs = new GZipStream(source, CompressionMode.Decompress))
            {
                gs.CopyTo(dest);
                gs.Flush();
                dest.Position = 0;
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

