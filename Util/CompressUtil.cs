using System;
using System.IO;
using System.IO.Compression;

using Kraken.Util;

namespace Kraken.Util
{
    public enum CompressionType {
        NONE, GZIP
    }
    
    public class CompressUtil
    {
        private CompressUtil()
        {
        }

        public static CompressionType StringToCompressionType(string type)
        {
            return (CompressionType)Enum.Parse(typeof(CompressionType), type, true);
        }

        // the goal is to compress a file into another file.
        // basics would be string to string.

        public static bool IsCompressible(Stream source, Stream dest, long uptoSize)
        {
            double threshold = 0.8;
            UpToStream upto = new UpToStream(source, uptoSize);
            using (Stream s = GetCompressStream(dest))
            {
                upto.CopyTo(s);
                s.Flush();
                if ((dest.Position * 1.0 / upto.Position) < threshold) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        public static void Compress(string sourcePath, string destPath)
        {
            Compress(sourcePath, destPath, true);
        }

        public static GZipStream GetCompressStream(Stream dest)
        {
            return new GZipStream(dest, CompressionMode.Compress);
        }

        public static GZipStream GetDecompressStream(Stream source)
        {
            return new GZipStream(source, CompressionMode.Decompress);
        }

        public static void Compress(string sourcePath, string destPath, bool resetDestPosition)
        {
            StreamUtil.FileToFile(sourcePath, destPath, (src, dest) => Compress(src, dest, resetDestPosition));
        }

        public static void Compress(Stream source, Stream dest, bool resetDestPosition)
        {
            using (GZipStream gs = GetCompressStream(dest))
            {
                source.CopyTo(gs);
                gs.Flush();
                if (resetDestPosition)
                    dest.Position = 0;
            }
        }

        public static void Decompress(string sourcePath, string destPath)
        {
            Decompress(sourcePath, destPath, true);
        }

        public static void Decompress(string sourcePath, string destPath, bool resetDestPosition)
        {
            StreamUtil.FileToFile(sourcePath, destPath, (src, dest) => Decompress(src, dest, resetDestPosition));
        }

        public static void Decompress(Stream source, Stream dest, bool resetDestPosition)
        {
            using (GZipStream gs = GetDecompressStream(source))
            {
                gs.CopyTo(dest);
                gs.Flush();
                if (resetDestPosition)
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

