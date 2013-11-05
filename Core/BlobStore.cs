using System;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Kraken.Util;

namespace Kraken.Core
{
    /// <summary>
    /// BLOB store.
    /// 
    /// This is the *local* version of the BlobStore, i.e. it stores the blobs (along with its metadata)
    /// on the local filesystem.
    /// 
    /// The structure of the local blob store looks like the following.
    /// 
    /// /<blob_store_root>
    ///   /<hash-part>/<hash-part>/.../<hash_final_part>
    /// 
    /// The advantage of this design is that it allows filesystem to handle the traversal and resolution.
    /// 
    /// Saving into the blob store basically means the following
    /// 
    /// 1 - calculate the checksum of the blob (default SHA1 but configurable)
    /// 2 - prepare the blob (compression, and encryption - both configurable)
    /// 3 - write to temp file of the following
    ///   The *envelope* (metadata describing the blob)
    ///   The prepared blob itself
    /// 4 - move the blob into its actual location determined by checksum.
    ///   NOTE - for this design, the move CAN fail if the file exists.
    ///   and in this case we DO NOT overwrite the old blob, because we want to ensure that
    ///   the metadata stay the same as the old blob (for example - the encryption IV).
    /// 5 - the checksum is returned and serve as the key to retrieve the blob.
    /// 
    /// Retrieval is based on the checksum.
    /// 
    /// 1 - take the checksum, and transform it into the appropriate underlying storage path.
    /// 2 - open the file - if it doesn't exist - throw (the calling function is expected to handle
    ///     a failure).
    /// 3 - if the file exist - create a Blob object that holds the envelope/metadata info, as well 
    ///     as prepare to reverse any compression/encryption done on the data.
    /// 4 - return the Blob for future use by the calling function.
    /// 
    /// </summary>
    public class BlobStore
    {
        const string workingFolder = ".work";
        const string blobFolder = "blob";

        string rootPath;
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
            string storePath = System.IO.Path.Combine(settings["rootPath"], blobFolder);
            int folderLevels = int.Parse(settings["folderLevels"]);
            int folderNameLength = int.Parse(settings["folderNameLength"]);
            initialize(storePath, checksumType, encryptionType, encryptionKey, folderLevels, folderNameLength);
        }

        public BlobStream OpenBlob(string checksum) {
            string path = normalizeChecksumPath(checksum);
            return new BlobStream(path, encryptionKey);
        }
        
        public string SaveBlob(string filePath) {
            byte[] iv = generateIV();
            long length = fileLength(filePath);
            string checksum = fileChecksum(filePath); 
            bool isCompressible = fileCompressible(filePath, length);
            string tempFile = saveToTempFile(filePath, checksum, length, iv, isCompressible);
            moveFile(tempFile, checksum);
            return checksum;
        }
        
        void initialize(string root, ChecksumType type, EncryptionType scheme, string key, int levels, int length)
        {
            rootPath = root;
            Directory.CreateDirectory(rootPath);
            workingPath = System.IO.Path.Combine(rootPath, workingFolder);
            Directory.CreateDirectory(workingPath);
            checksumType = type;
            encryptionScheme = scheme;
            if (encryptionScheme != EncryptionType.NONE)
            {
                encryptionKey = ByteUtil.HexStringToByteArray(key);
            }
            folderLevels = levels;
            folderNameLength = length;
        }

        string fileChecksum(string filePath) {
            return ChecksumUtil.ComputeChecksum(checksumType, filePath);
        }

        long fileLength(string path)
        {
            FileInfo fi = new FileInfo(path);
            return fi.Length;
        }

        byte[] generateIV() {
            return EncryptionUtil.GetRandomBytes(16);
        }

        void moveFile(string tempFile, string checksum)
        {
            string checksumPath = normalizeChecksumPath(checksum);
            try
            {
                File.Move(tempFile, checksumPath);
            } catch (Exception e)
            {
                Console.WriteLine("MoveFile_successful {0}", e);
                File.Delete(tempFile);
            }
        }

        string normalizeChecksumPath(string checksum)
        {
            string folderPath = checksumToFolderPath(checksum);
            Directory.CreateDirectory(folderPath);
            return System.IO.Path.Combine(folderPath, checksumToFileName(checksum));
        }

        string checksumToFileName(string checksum)
        {
            return checksum.Substring(folderNameLength * folderLevels);
        }
        
        string checksumToFolderPath(string checksum)
        {
            string[] folders = new string[folderLevels + 1];
            folders[0] = rootPath;
            for (int i = 0; i < folderLevels; ++i)
            {
                folders[i + 1] = checksum.Substring(i * folderNameLength, folderNameLength);
            }
            return System.IO.Path.Combine((string[])folders);
        }

        string saveToTempFile(string filePath, string checksum, long length, byte[] iv, bool isCompressible)
        {
            // secure a tempfile based on the checksum + the working path.
            Guid uuid = Guid.NewGuid();
            string tempFilePath = System.IO.Path.Combine(workingPath, string.Format("{0}.{1}", checksum, uuid));
            using (FileStream tempFile = File.Open(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                BlobEnvelope envelope = makeBlobEnvelope(checksum, length, iv, isCompressible);
                envelope.WriteTo(tempFile);
                using (FileStream input = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    using (Stream output = getStream(tempFile, iv, isCompressible)) {
                        input.CopyTo(output);
                        output.Flush();
                        return tempFilePath;
                    }
                }
            }
        }

        Stream getStream(Stream s, byte[] iv, bool isCompressible)
        {
            Stream resultStream = s;
            if (encryptionScheme != EncryptionType.NONE)
            {
                resultStream = EncryptionUtil.GetEncryptStream(resultStream, encryptionScheme, encryptionKey, iv);
            }
            if (isCompressible)
            {
                resultStream = CompressUtil.GetCompressStream(resultStream);
            }
            return resultStream;
        }

        void writeString(Stream dest, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            dest.Write(bytes, 0, bytes.Length);
        }


        BlobEnvelope makeBlobEnvelope(string checksum, long length, byte[] iv, bool isCompressible)
        {
            BlobEnvelope envelope = new BlobEnvelope();
            envelope.Checksum = checksum;
            envelope.OriginalLength = length;
            envelope.CompressionScheme = isCompressible ? CompressionType.GZIP : CompressionType.NONE;
            envelope.EncryptionScheme = encryptionScheme;
            envelope.EncryptionIV = iv;
            return envelope;
        }

        // this might belong with CompressUtil.
        bool fileCompressible(string filePath, long length) {
            long uptoBytes = 1024 * 1024;
            using (MemoryStream ms = new MemoryStream()) {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    return CompressUtil.IsCompressible(fs, ms, uptoBytes);
                }
            }
        }
    }
}

