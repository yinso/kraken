using System;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Data;
using System.Data.Common;
using System.Text;
using Mono;
//using FirebirdSql.Data.FirebirdClient;
//using Mono.Data.Sqlite;
//using MongoDB.Driver;
//using MongoDB.Bson;
using System.Configuration;

using Kraken.Util;


namespace Kraken.CommandLine
{
	class MainClass
	{

        public static void Main(string[] args)
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            EncryptionType encryptionType = EncryptionUtil.StringToEncryptionType(appSettings["cryptoType"]);
            string encryptionKey = appSettings["cryptoKey"];
            ChecksumType checksumType = ChecksumUtil.StringToChecksumType(appSettings["checksumType"]);
            string storePath = Path.Combine(appSettings["rootPath"], appSettings["blobFolder"]);
            int folderLevels = int.Parse(appSettings["folderLevels"]);
            int folderNameLength = int.Parse(appSettings["folderNameLength"]);
            BlobStore store = new BlobStore(storePath, checksumType, encryptionType, encryptionKey, folderLevels, folderNameLength);
            string filePath = "../../Main.cs";
            string checksum = store.SaveFile(filePath);
            using (Blob b = store.OpenBlob(checksum))
            {
                Console.WriteLine("File: {0} => {1}", filePath, checksum);
                PrintToConsole(b);
            }
        }

        public static void PrintToConsole(Stream s)
        {
            using (StreamReader reader = new StreamReader(s)) {
                string allText = reader.ReadToEnd();
                Console.WriteLine(allText);
            }
        }
    }
}
