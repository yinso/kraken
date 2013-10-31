using System;
using System.Collections.Specialized;
using System.IO;

using Kraken.Util;

namespace Kraken.Core
{
    /// <summary>
    /// BLOB path store.
    /// </summary>
    public class BlobPathStore
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
        public BlobPathStore(NameValueCollection settings)
        {
            rootFolderPath = Path.Combine(settings["rootPath"], pathFolder);
            workingFolderPath = Path.Combine(rootFolderPath, workingFolder);
            FileUtil.EnsureDirectory(workingFolderPath);
            blobStore = new BlobStore(settings);
        }

        public void ReadPath(string virtualPath) 
        {
        }

        public void SavePath(string filePath, string toPath)
        {
            string checksum = blobStore.SaveBlob(filePath);
            // now we have the checksum it's time to deal with the 
            string saveToPath = FileUtil.CombinePath(rootFolderPath, toPath);
            FileUtil.EnsurePathDirectory(saveToPath);
            try
            {
                string tempFilePath = FileUtil.TempFilePath(FileUtil.ChangePathDirectory(saveToPath, workingFolderPath));
                using (FileStream fs = File.Open(saveToPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (FileStream tempFile = File.Open(tempFilePath, FileMode.Open, FileAccess.Write, FileShare.None)) {
                        fs.CopyTo(tempFile);
                        tempFile.Flush();
                    }
                }
                FileUtil.Rename(tempFilePath, saveToPath);
            } catch (FileNotFoundException e)
            {
                // time to decide the format of the file...

                // file does not exist... 
                // this is the part where there are challenges about writing against a single file...
                // i.e. this needs to be exclusive access (we'll assume that others are not trying to touch the file...
                // this might not be a good assumption...).
                // OK - how do we think through the challenging case? Because most of the time this will be fine, but
                // it seems to be difficult to gaurantee the transactional nature of the work (and we don't know
                // whether or not it works).
                // 1 - we can use the write-ahead log approach... that can get us to start on transactions.
                // 2 - what we need to be careful is that the file needs to be easy to merge...
                // the process is 
                // create a new file with the line in the beginning, and then save over the old file (we'll clobber it
                // if it exists).
                // there is a possibility that someone else will have "merged" in changes during this period).
                // 

            }
        }
    }
}

