using System;
using System.IO;
using System.Runtime.InteropServices;


namespace Kraken.CommandLine
{
    public class LocalStore
    {
        // these values will be shared once we allow for multiple files being added at once in server-mode.
        private string rootPath = Directory.GetCurrentDirectory();
        private string cacheFolder = "cache";
        private string storeFolder = "store";
        private string cachePath;
        private string storePath;

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
        }

        public FileStream GetFile(string checksum) {
            string filePath = ChecksumToFilePath(checksum);
            return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                MoveFile(tempPath, destPath);
            } catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e);
                File.Delete(tempPath);
            }
        }
    }
}

