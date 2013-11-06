using System;
using System.Collections.Generic;
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
        List<string> osJunkFiles = new List<string> {".DS_Store", "thumbs.db"};

        public RootPathMap RootPathMap { get; internal set; }

        public PathStore(NameValueCollection settings)
        {
            rootFolderPath = System.IO.Path.Combine(settings["rootPath"], pathFolder);
            workingFolderPath = System.IO.Path.Combine(rootFolderPath, workingFolder);
            FileUtil.EnsureDirectory(workingFolderPath);
            blobStore = new BlobStore(settings);
            RootPathMap = new RootPathMap();
        }

        public bool IsJunkFile(string path)
        {
            string fileName = System.IO.Path.GetFileName(path);
            return osJunkFiles.Contains(fileName);
        }

        public bool IsDirectory(string virtualPath) {
            return Directory.Exists(NormalizePath(virtualPath));
        }

        public bool IsBlob(string virtualPath)
        {
            return File.Exists(NormalizePath(virtualPath));
        }

        public BlobStream GetBlob(string virtualPath)
        {
            return PathToBlob(ReadPath(virtualPath));
        }

        public BlobStream PathToBlob(Path path)
        {
            return blobStore.OpenBlob(path.Envelope.Checksum);
        }

        public Path ReadPath(string virtualPath) 
        {
            // we should load the Path object by translating this into a a fullPath.
            // the virtual Path wou
            return Path.ParseFile(NormalizePath(virtualPath), true);
        }

        public string NormalizePath(string vPath)
        {
            return System.IO.Path.Combine(rootFolderPath, vPath);
        }

        public string DenormalizePath(string absPath)
        {
            if (absPath.IndexOf(rootFolderPath) == 0)
            {
                return absPath.Substring(rootFolderPath.Length);
            } else
            {
                return absPath;
            }
        }

        public void DeleteFolder(string vPath)
        {
            string normalizedPath = System.IO.Path.IsPathRooted(vPath) ? vPath : NormalizePath(vPath);
            foreach (string dirPath in Directory.GetDirectories(normalizedPath))
            {
                DeleteFolder(dirPath);
            }
            foreach (string filePath in Directory.GetFiles(normalizedPath))
            {
                Path.DeletePath(filePath);
            }
            Directory.Delete(normalizedPath);
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
                if (!IsJunkFile(filePath)) {
                    SavePath(filePath, toPath);
                }
            } else if (Directory.Exists(filePath))
            {
                foreach (string newFilePath in Directory.GetFiles(filePath)) {
                    if (!IsJunkFile(newFilePath)) {
                        Console.WriteLine("Save {0}...", newFilePath);
                        string fileName = System.IO.Path.GetFileName(newFilePath);
                        SavePath(newFilePath, System.IO.Path.Combine(toPath, fileName));
                    }
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

        public void MergeFolder(string fromPath, string toPath)
        {

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
            string saveToPath = NormalizePath(toPath); 
            return Path.SavePath(filePath, saveToPath, workingFolderPath, checksum);
        }

        public Path SaveStream(Stream s, string toPath)
        {
            string checksum = blobStore.SaveBlob(s);
            string saveToPath = NormalizePath(toPath);
            return Path.SavePath(saveToPath, workingFolder, checksum);
        }

        public void RestoreOnePath(string fromPath, string toPath) {
            Console.WriteLine("Restore File {0} to {1}", fromPath, toPath);
            Path path = ReadPath(fromPath);
            using (BlobStream blob = PathToBlob(path)) {
                using (AtomicFileStream fs = new AtomicFileStream(toPath)) {
                    blob.CopyTo(fs);
                }
            }
            File.SetCreationTimeUtc(toPath, path.Envelope.LastModified);
            File.SetLastWriteTimeUtc(toPath, path.Envelope.LastModified);
        }

        public void RestoreFolder(string fromPath, string toPath) {
            Console.WriteLine("Restore Folder {0} to {1}", fromPath, toPath);
            string normalizedPath = System.IO.Path.IsPathRooted(fromPath) ? fromPath : NormalizePath(fromPath);
            FileUtil.EnsureDirectory(toPath, normalizedPath);
            foreach (string filePath in Directory.GetFiles(normalizedPath)) {
                if (!IsJunkFile(filePath)) {
                    RestoreOnePath(filePath, FileUtil.ChangePathDirectory(filePath, toPath));
                }
            }
            foreach (string folderPath in Directory.GetDirectories(normalizedPath)) {
                RestoreFolder(folderPath, FileUtil.ChangePathDirectory(folderPath, toPath));
            }
        }

        public IEnumerable<string> ListPaths(string startPath, int depth) {
            string normPath = NormalizePath(startPath);
            List<string> paths = new List<string>();
            listPathHelper(normPath, depth, 0, paths);
            return paths;
        }

        void listPathHelper(string startPath, int depth, int level, List<string> paths) {
            string normPath = startPath;
            if (IsDirectory(normPath)) {
                foreach (string dirPath in Directory.GetDirectories(normPath)) {
                    paths.Add(string.Format("{0}/", DenormalizePath(dirPath)));
                    if (level < depth) {
                        listPathHelper(dirPath, depth, level + 1, paths);
                    }
                }
                foreach (string filePath in Directory.GetFiles(normPath)) {
                    if (!IsJunkFile(filePath))
                        paths.Add(DenormalizePath(filePath));
                }
            } else if (IsBlob(normPath)) {
                paths.Add(startPath);
            }
        }
    }
}

