using System;
using System.IO;
using System.Runtime.InteropServices;

using System.Data;
using System.Data.Common;
using Mono.Data.Sqlite; // refactor this out later.

namespace Kraken.CommandLine
{
    public class LocalStore : IDisposable
    {
        // these values will be shared once we allow for multiple files being added at once in server-mode.
        private string rootPath = Directory.GetCurrentDirectory();
        private string cacheFolder = "cache";
        private string storeFolder = "store";
        string dbFolder = "db";
        string dbFile = "testdb.db";
        private string cachePath;
        private string storePath;
        string databasePath;
        string connString;
        IDbConnection conn;

        // For Atomic Move File without errors.
        // This is actually quite difficult to achieve on Windows, and File.Move 
        // implementation doesn't have an overwrite flag for overwriting a file if it
        // exists (it throws an error instead.
        // TODO we'll need win32 equivalent for the code below.
#if !WIN32
        [DllImport("libc")]
        private static extern int rename(string sourcePath, string destPath);
#endif
        public static void MoveFile(string sourcePath, string destPath)
        {
#if !WIN32
            int result = rename(sourcePath, destPath);
            if (result == 0)
                return;
            throw new Exception(string.Format("Rename Failed: ErrorCode: {0}", result));
#else
            try { // not the most optimal way of doing it...
                File.Move(sourcePath, destPath);
            } catch (Exception) { }
#endif
        }

        public LocalStore()
        {
            cachePath = Path.Combine(rootPath, cacheFolder);
            storePath = Path.Combine(rootPath, storeFolder);
            Directory.CreateDirectory(storePath);
            Directory.CreateDirectory(cachePath);
            conn = InitDatabase();
            conn.Open();
        }

        public void Dispose() {
            conn.Close();
            conn.Dispose();
        }

        public Stream GetFile(string checksum)
        {
            string filePath = ChecksumToFilePath(checksum);
            // we should determine whether or not the file is compressed.
            if (IsFileCompressed(checksum))
            {
                // return a uncompressed stream.
                MemoryStream s = new MemoryStream(); // not efficient this way...
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    CompressUtil.Decompress(fs, s);
                    return s;
                }
            } else
            {
                return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
        }

        public bool IsFileCompressed(string checksum)
        {
            string cmdText = "select compressed from path_t where checksum = ?";
            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.CommandType = CommandType.Text;
                
                IDbDataParameter checksumParm = cmd.CreateParameter();
                checksumParm.ParameterName = "checksum";
                checksumParm.DbType = DbType.String;
                checksumParm.Value = checksum;
                
                cmd.Parameters.Add(checksumParm);
                return (bool)cmd.ExecuteScalar();
            }
        }

        public string ChecksumToFilePath(string checksum)
        {
            string folder = ChecksumToFolderPath(checksum);
            string fileName = checksum.Substring(2);
            return Path.Combine(folder, fileName);
        }

        public string ChecksumToFolderPath(string checksum)
        {
            string folder = checksum.Substring(0, 2);
            return Path.Combine(storePath, folder);
        }

        public void StoreFile(string filePath, string checksum)
        {
            string destFolder = ChecksumToFolderPath(checksum);
            string destPath = ChecksumToFilePath(checksum);
            Console.WriteLine("Ensure Path: {0}", destFolder);
            Directory.CreateDirectory(destFolder);
            string tempPath = Path.Combine(cachePath, checksum + (new Guid()).ToString());
            try
            {
                File.Copy(filePath, tempPath);
                bool compressed = false;
                // for now we'll just try to compress the file without testing for its mimetype, since mimetype isn't really complete enough for this particular test.
                string finalTempPath = TestCompress(tempPath, out compressed); // we don't know the file has been compressed... that's ok.
                Console.WriteLine("Compressed: {0}", compressed);
                MoveFile(finalTempPath, destPath);
                // now we can store the information in the database.
                StoreFileMetaInfo(checksum, compressed);
            } catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
                File.Delete(tempPath);
            }
        }

        public string TestCompress(string origPath, out bool compressed)
        {
            string newPath = origPath + "." + (new Guid()).ToString();
            CompressUtil.Compress(origPath, newPath);
            // get the file size for each file.
            long origPathSize = FileLength(origPath);
            long newPathSize = FileLength(newPath);
            Console.WriteLine("orig: {0} => {1}, new {2} => {3}", origPath, origPathSize, newPath, newPathSize);
            if (newPathSize < origPathSize)
            {
                compressed = true;
                File.Delete(origPath);
                return newPath;
            } else
            {
                compressed = false;
                File.Delete(newPath);
                return origPath;
            }
        }

        public long FileLength(string path)
        {
            FileInfo fi = new FileInfo(path);
            return fi.Length;
        }

        public void StoreFileMetaInfo(string checksum, bool compressed)
        {
            string cmdText = "insert into path_t (checksum, compressed) values (?, ?)";
            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.CommandType = CommandType.Text;

                IDbDataParameter checksumParm = cmd.CreateParameter();
                checksumParm.ParameterName = "checksum";
                checksumParm.DbType = DbType.String;
                checksumParm.Value = checksum;

                cmd.Parameters.Add(checksumParm);

                IDbDataParameter compressedParm = cmd.CreateParameter();
                compressedParm.ParameterName = "compressed";
                compressedParm.DbType = DbType.Boolean;
                compressedParm.Value = compressed;
                cmd.Parameters.Add(compressedParm);
                try {
                    cmd.ExecuteNonQuery();
                } catch (Exception) { } // if it exists already it's ok.
            }
        }

        public IDbConnection InitDatabase()
        {
            Directory.CreateDirectory(Path.Combine(rootPath, dbFolder));
            databasePath = Path.Combine(rootPath, dbFolder, dbFile);
            connString = string.Format("URI={0},version=3", databasePath);
            try
            {
                using (FileStream s = File.Open(databasePath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                }
                return new SqliteConnection(connString);
            } catch (FileNotFoundException)
            {
                InitializeSqlite();
                return new SqliteConnection(connString);
            }
        }

        public void InitializeSqlite()
        {
            string[] initCommands = {
                "create table path_t(id integer primary key AUTOINCREMENT not null, checksum varchar(64) unique not null, compressed bool default false not null)"
            };

            try
            {
                using (IDbConnection conn = new SqliteConnection(connString)) {
                    conn.Open();
                    foreach (string initCmd in initCommands) {
                        using (IDbCommand cmd = conn.CreateCommand()) {
                            cmd.CommandText = initCmd;
                            Console.WriteLine("CMD: {0}", initCmd);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine("Using Sqlite Failed: {0}", e);
            }
        }
    }
}

