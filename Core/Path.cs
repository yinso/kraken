using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Kraken.Util;

namespace Kraken.Core
{
    /// <summary>
    /// Path.
    /// 
    /// Usage: 
    /// 
    /// Path obj = Path.StoreFile(filePath, toPath, checksum, timestamp); 
    /// 
    /// </summary>
    public class Path // the thing about this is that it's a lazy collection. i.e. we aren't going to load pass the 
    { 
        public PathEnvelope Envelope { get; internal set; }

        List<PathVersion> Versions { get; set; }

        public string VirtualPath { get; private set; }

        string fullPath;

        protected Path(string path)
        {
            fullPath = path; // this will give us a chance to reload the filestream as needed.
            Envelope = new PathEnvelope();
            Versions = new List<PathVersion>();
        }

        public static Path ParseFile(string filePath)
        {
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Parse(fs, filePath);
            }
        }

        public static Path Parse(Stream s, string filePath)
        {
            using (Reader reader = new Reader(s))
            {
                Path path = new Path(filePath);
                path.Envelope = PathEnvelope.Parse(reader);
                // then what? 
                while (reader.PeekByte() != -1) {
                    path.Versions.Add(PathVersion.Parse(reader));
                }
                return path;
            }
        }

        public static Path SavePath(string fullPath, string workingDir, string checksum, DateTime timestamp)
        {
            Path path;
            FileUtil.EnsurePathDirectory(fullPath);
            try
            {
                using (FileStream fs = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    //using (Stream gz = CompressUtil.GetDecompressStream(fs)) {
                    path = Parse(fs, fullPath);
                    if (path.Envelope.Checksum == checksum) 
                        return path;
                    path.Envelope.Checksum = checksum;
                    path.Envelope.LastModified = timestamp;
                    //}
                }
            } catch (FileNotFoundException)
            {
                path = new Path(fullPath);
                path.Envelope.Checksum = checksum;
                path.Envelope.Created = timestamp;
                path.Envelope.LastModified = timestamp;
            }
            setNewVersion(path, checksum, timestamp);
            string tempPath = FileUtil.TempFilePath(fullPath);
            // time to serialize to a temp file, and then *move* the file over the existing file.
            using (FileStream tempFile = FileUtil.OpenTempFile(tempPath, true)) {
                path.WriteTo(tempFile);
            }
            FileUtil.Rename(tempPath, fullPath);
            //File.Replace(tempPath, fullPath, fullPath + ".backup");
            return path;
        }

        static void setNewVersion(Path path, string checksum, DateTime timestamp) {
            PathVersion version = new PathVersion();
            version.Checksum = checksum;
            version.Timestamp = timestamp;
            path.Versions.Insert(0, version);
        }

        public void WriteTo(Stream s)
        {
            Envelope.WriteTo(s);
            foreach (PathVersion version in Versions)
            {
                version.WriteTo(s);
            }
        }
    }
}

