using System;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Kraken.Util;

namespace Kraken.Core
{
    public class BlobStore
    {
        string rootPath;
        string workingFolder = ".work";
        string workingPath;
        ChecksumType checksumType;
        EncryptionType encryptionScheme;
        byte[] encryptionKey = new byte[0];
        int folderLevels = 3; 
        int folderNameLength = 2;

        public BlobStore(NameValueCollection settings)
        {
            EncryptionType encryptionType = EncryptionUtil.StringToEncryptionType(settings["cryptoType"]);
            string encryptionKey = settings["cryptoKey"];
            ChecksumType checksumType = ChecksumUtil.StringToChecksumType(settings["checksumType"]);
            string storePath = Path.Combine(settings["rootPath"], settings["blobFolder"]);
            int folderLevels = int.Parse(settings["folderLevels"]);
            int folderNameLength = int.Parse(settings["folderNameLength"]);
            initialize(storePath, checksumType, encryptionType, encryptionKey, folderLevels, folderNameLength);
        }

        void initialize(string root, ChecksumType type, EncryptionType scheme, string key, int levels, int length)
        {
            rootPath = root;
            Directory.CreateDirectory(rootPath);
            workingPath = Path.Combine(rootPath, workingFolder);
            Directory.CreateDirectory(workingPath);
            checksumType = type;
            encryptionScheme = scheme;
            if (encryptionScheme != EncryptionType.NONE)
            {
                encryptionKey = StringUtil.HexStringToByteArray(key);
            }
            folderLevels = levels;
            folderNameLength = length;
        }

        public string FileChecksum(string filePath) {
            return ChecksumUtil.ComputeChecksum(checksumType, filePath);
        }

        public long FileLength(string path)
        {
            FileInfo fi = new FileInfo(path);
            return fi.Length;
        }

        public byte[] GenerateIV() {
            return EncryptionUtil.GetRandomBytes(16);
        }

        public Blob OpenBlob(string checksum) {
            // convert the checksum to path.
            string path = NormalizeChecksumPath(checksum);
            // we'll need encryption key + 
            return new Blob(path, encryptionKey);
        }

        public string SaveBlob(string filePath) {
            // in order to store a file - we will need to go through the following steps.
            // 1 - figure out the checksum - if the file exists we can safely ignore the rest of the steps
            byte[] iv = GenerateIV();
            long length = FileLength(filePath);
            string checksum = FileChecksum(filePath); // this step is needed in order for us to figure out what to do with the file... oh well - will see how it can be optimized
            bool isCompressible = FileCompressible(filePath, length);
            string tempFile = SaveToTempFile(filePath, checksum, length, iv, isCompressible);
            // finally - we can move the file to its new location.
            // this ought to be the same logica as the local store, except we are completely managing it within 
            // blob environment.
            // we ought to allow this to be configurable as well...
            // we should decide how many layers down for a given blob repo (there might be more than one).
            MoveFile(tempFile, checksum);
            return checksum;
        }

        public void MoveFile(string tempFile, string checksum)
        {
            // let's convert the checksum to a filePath.
            string checksumPath = NormalizeChecksumPath(checksum);
            try
            {
                File.Move(tempFile, checksumPath);
            } catch (Exception e)
            {
                Console.WriteLine("MoveFile_successful {0}", e);
                File.Delete(tempFile);
            }
        }

        string NormalizeChecksumPath(string checksum)
        {
            string folderPath = ChecksumToFolderPath(checksum);
            Directory.CreateDirectory(folderPath);
            return Path.Combine(folderPath, ChecksumToFileName(checksum));
        }

        // because this is *configurable* - we'll have to do more work for the calculation...
        public string ChecksumToFileName(string checksum)
        {
            return checksum.Substring(folderNameLength * folderLevels);
        }
        
        public string ChecksumToFolderPath(string checksum)
        {
            // take the checksum and che
            string[] folders = new string[folderLevels + 1];
            folders[0] = rootPath;
            for (int i = 0; i < folderLevels; ++i)
            {
                folders[i + 1] = checksum.Substring(i * folderNameLength, folderNameLength);
            }
            return Path.Combine((string[])folders);
        }

        public string SaveToTempFile(string filePath, string checksum, long length, byte[] iv, bool isCompressible)
        {
            // secure a tempfile based on the checksum + the working path.
            Guid uuid = Guid.NewGuid();
            string tempFilePath = Path.Combine(workingPath, string.Format("{0}.{1}", checksum, uuid));
            using (FileStream tempFile = File.Open(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                string preamble = BlobPreamble(length, iv, isCompressible);
                // preamble needs to be written as bytes to the tempFile...
                WriteString(tempFile, preamble);
                WriteString(tempFile, "\r\n");
                using (FileStream input = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    using (Stream output = GetStream(tempFile, iv, isCompressible)) {
                        input.CopyTo(output);
                        output.Flush();
                        return tempFilePath;
                    }
                }
            }
        }

        Stream GetStream(Stream s, byte[] iv, bool isCompressible)
        {
            Stream result = s;
            if (encryptionScheme != EncryptionType.NONE)
            {
                result = EncryptionUtil.GetEncryptStream(result, encryptionScheme, encryptionKey, iv);
                // we'll get the encryption figured out...
            }
            if (isCompressible)
            {
                result = CompressUtil.GetCompressStream(result);
            }
            return result;
        }

        void WriteString(Stream dest, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            dest.Write(bytes, 0, bytes.Length);
        }


        public string BlobPreamble(long length, byte[] iv, bool isCompressible)
        {
            string[] values = new string[4];
            values [0] = length.ToString();
            if (isCompressible)
            {
                values [1] = CompressionType.GZIP.ToString().ToLower();
            } else
            {
                values [1] = CompressionType.NONE.ToString().ToLower();
            }
            values [2] = encryptionScheme.ToString().ToLower();
            if (encryptionScheme == EncryptionType.NONE)
            {
                values [3] = "none";
            } else
            {
                values[3] = StringUtil.ByteArrayToHexString(iv);
            }
            return string.Join(" ", values) + "\r\n";
        }

        public bool FileCompressible(string filePath, long length) {
            // if the file is under a particular threshold, we'll test encrypt the whole thing.
            // otherwise we'll encrypt just a portion of it.
            // let's say the threashold is 1MB.
            long threshold = 1024 * 1024;
            long gain = 1024;
            // in either case - we'll compress up to this particular point.
            // we'll need to control a particular amount to copy over... 
            using (MemoryStream ms = new MemoryStream()) {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    return CompressUtil.IsCompressible(fs, ms, threshold);
                }
            }
        }
    }
}

