using System;
using System.Collections.Specialized;
using System.IO;

using Kraken.Util;

namespace Kraken.Core
{
    /// <summary>
    /// PathStore
    /// 
    /// An abstraction over the plain BlobStore to provide more than just the CAS.
    /// 
    /// It looks like regular file system to users.
    /// 
    /// 
    /// 
    /// </summary>
    public class PathStore
    {
        string pathFolder = "path";
        string workingFolder = ".working";
        string rootFolderPath;
        string workingFolderPath;
        BlobStore blobStore;
        public PathStore(NameValueCollection settings)
        {
            rootFolderPath = System.IO.Path.Combine(settings["rootPath"], pathFolder);
            workingFolderPath = System.IO.Path.Combine(rootFolderPath, workingFolder);
            FileUtil.EnsureDirectory(workingFolderPath);
            blobStore = new BlobStore(settings);
        }

        public Blob GetBlob(string virtualPath)
        {
            return PathToBlob(ReadPath(virtualPath));
        }

        public Blob PathToBlob(Path path)
        {
            return blobStore.OpenBlob(path.Envelope.Checksum);
        }

        public Path ReadPath(string virtualPath) 
        {
            // we should load the Path object by translating this into a a fullPath.
            // the virtual Path wou
            return Path.ParseFile(NormalizePath(virtualPath));
        }

        public string NormalizePath(string vPath)
        {
            return System.IO.Path.Combine(rootFolderPath, vPath);
        }

        public void DeletePath(string vPath)
        {
            Path.DeletePath(NormalizePath(vPath));
        }

        public void SaveFolder(string filePath, string toPath)
        {
            Console.WriteLine("SaveFolder: {0} => {1}", filePath, toPath);
            if (File.Exists(filePath))
            {
                SavePath(filePath, toPath);
            } else if (Directory.Exists(filePath))
            {
                foreach (string newFilePath in Directory.GetFiles(filePath)) {
                    string fileName = System.IO.Path.GetFileName(newFilePath);
                    SavePath(newFilePath, System.IO.Path.Combine(toPath, fileName));
                }
                foreach (string newDir in Directory.GetDirectories(filePath)) {
                    string dirName = System.IO.Path.GetFileName(newDir);
                    SaveFolder(newDir, System.IO.Path.Combine(toPath, dirName));
                }
            } else // this should not be reachable???
            {
                throw new Exception(string.Format("save_folder_path_neither_file_nor_folder: {0}", filePath));
            }
        }

        public Path SavePath(string filePath, string toPath)
        {
            Console.WriteLine("SavePath: {0} => {1}", filePath, toPath);
            // we have been assuming this is a file all this time.
            // let's now also deal with directory (not yet dealing with symlinks... not sure what to do there).
            if (Directory.Exists(filePath))
            {
                // the question is... what do we do when we save a bunch of files?
                // as path isn't about the folders - if this is a folder, we don't return a Path function.
                throw new Exception("savepath_is_wrong_function_for_folder");
            } else
            {
                return SaveOnePath(filePath, toPath);
            }
        }

        public Path SaveOnePath(string filePath, string toPath) {
            string checksum = blobStore.SaveBlob(filePath);
            // now we have the checksum it's time to deal with the 
            string saveToPath = FileUtil.CombinePath(rootFolderPath, toPath);
            return Path.SavePath(saveToPath, workingFolderPath, checksum, DateTime.UtcNow);
        }
    }
}

