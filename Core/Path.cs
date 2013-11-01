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

        public static void DeletePath(string fullPath, bool recursive)
        {
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, recursive);
            } else
            {
                File.Delete(fullPath);
            }
        }

        public static void DeletePath(string fullPath)
        {
            DeletePath(fullPath, false);
        }

        public static void MovePath(string fromFullPath, string toFullPath)
        {
            // one thing we should also allow in this system is an UNDO.
            // that means we don't fully delete what's been moved... we move it somewhere...

            // both of the path are managed path by us.
            // if toFullPath doesn't exist - it's just a simple move.
            // if toPath exists - we have a few things.
            // if fromPath is a file and toPath is a file -> do we *merge* the two? This is almost equivalent to "SavePath"
            // if fromPath is a folder and toPath is a file -> do we clobber over the toPath?
            // moving a folder to another folder will put things into that folder
            //
            // filesystem behaves this way.
            // file to folder => move into the folder
            // folder to folder => move into the folder
            // file to file => overwrite the file
            // folder to file => not a legal operation.
            // so we should follow exactly the same thing...
            // let's see what happens if there are things inside the folder that have the same name?
            // file to folder/<file_with_file_name> => overwrite the file.
            // file to folder/<folder_with_file_name> => fails
            // folder to folder/<file_with_same_name> => fails
            // folder to folder/<folder_with_the_same_name> => appears to be OK (with empty directory - FAIL otherwise).
            // is this just a case of letting the underlying operating system take care of it?
            // 
            // we can just overwrite and call it good.
            if (Directory.Exists(fromFullPath))
            {
                Directory.Move(fromFullPath, toFullPath);
            } else if (File.Exists(fromFullPath))
            {
                // this one will not work well... it'll fail on existing file.
                File.Move(fromFullPath, toFullPath);
            } else
            {
                throw new FileNotFoundException(fromFullPath);
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
            string tempPath = FileUtil.TempFilePath(fullPath, workingDir);
            // time to serialize to a temp file, and then *move* the file over the existing file.
            using (FileStream tempFile = FileUtil.OpenTempFile(tempPath, true)) {
                path.WriteTo(tempFile);
            }
            FileUtil.Rename(tempPath, fullPath);
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

