using System;
using System.IO;

namespace Kraken.Util
{
    public class StreamUtil
    {
        private StreamUtil()
        {
        }

        public static void FileToFile(string sourcePath, string destPath, Action<Stream, Stream> helper) {
            using (FileStream source = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (FileStream dest = File.Open(destPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
                    helper(source, dest);
                }
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

        public static Stream StreamFromString(string s) {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}

