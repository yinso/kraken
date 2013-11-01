using System;
using System.Collections.Specialized;
using System.IO;

using Kraken.Util;

namespace Kraken.Core
{
    /// <summary>
    /// BLOB path store.
    /// </summary>
    public class PathStore
    {
        string pathFolder = "path";
        string workingFolder = ".working";
        string rootFolderPath;
        string workingFolderPath;
        BlobStore blobStore;
        // *****
        // format of Blob
        // *****
        // we want something simple but probably will need to pay for things a bit.
        // should it be JSON?
        // or should it be something that's a bit more appropriately evaluated?
        // for example - what happens if we end up storing symlink?
        // 
        // we still don't have 
        // what should we use as the format of the file?
        // let's think
        // 1 -> 
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
            return Path.ParseFile(System.IO.Path.Combine(rootFolderPath, virtualPath));
        }

        public Path SavePath(string filePath, string toPath)
        {
            string checksum = blobStore.SaveBlob(filePath);
            // now we have the checksum it's time to deal with the 
            string saveToPath = FileUtil.CombinePath(rootFolderPath, toPath);
            return Path.SavePath(saveToPath, workingFolderPath, checksum, DateTime.UtcNow);
        }
    }
}

