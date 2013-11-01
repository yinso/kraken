using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Kraken.Util
{
    public class FileUtil
    {
#if !WIN32
        [DllImport("libc", SetLastError=true)]
        private extern static int rename(string oldPath, string newPath);
#endif 

        public static void Rename(string oldPath, string newPath)
        {

#if !WIN32
            int result = rename(oldPath, newPath);
            Console.WriteLine("File.Rename via rename {0} => {1}", oldPath, newPath);
            if (result != 0) {
                int errno = Marshal.GetLastWin32Error();
                throw new Exception(string.Format("fileutil_rename_failed_errocode: {0}", errno));
            }
#endif
        }


        private FileUtil()
        {
        }

        public static string CombinePath(params string[] paths) {
            return Path.Combine((string[])paths);
        }

        public static void EnsureDirectory(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
        }

        public static string ChangePathDirectory(string filePath, string newDir)
        {
            return Path.Combine(newDir, Path.GetFileName(filePath));
        }

        public static void EnsurePathDirectory(string filePath) {
            EnsureDirectory(Path.GetDirectoryName(filePath));
        }

        public static string TempFilePath(string filePath, string newBasePath)
        {
            return TempFilePath(ChangePathDirectory(filePath, newBasePath));
        }

        public static string TempFilePath(string filePath)
        {
            EnsurePathDirectory(filePath);
            return string.Format("{0}.{1}", filePath, Guid.NewGuid().ToString());
        }

        public static FileStream OpenTempFile(string filePath, string newBasePath)
        {
            return OpenTempFile(ChangePathDirectory(filePath, newBasePath));
        }

        public static FileStream OpenTempFile(string filePath) {
            return OpenTempFile(filePath, false);
        }

        public static FileStream OpenTempFile(string filePath, bool isTempPath) {

            string tempPath = isTempPath ? filePath : TempFilePath(filePath);
            return File.Open(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        }
    }
}

