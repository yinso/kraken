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
            string storePath = Path.Combine(appSettings["rootPath"], appSettings["storeFolder"]);
            BlobStore store = new BlobStore(storePath, checksumType, encryptionType, encryptionKey);
            string filePath = "../../Main.cs";
            string checksum = store.SaveFile(filePath);
            using (Blob b = store.OpenBlob(checksum))
            {
                Console.WriteLine("File: {0} => {1}", filePath, checksum);
                PrintToConsole(b);
            }
        }
        
        public static void Main2(string[] args)
        {
            Console.WriteLine("ROOT => {0}", ConfigurationManager.AppSettings["rootPath"]);
            Console.WriteLine("STORE => {0}", ConfigurationManager.AppSettings["storeFolder"]);
            Console.WriteLine("DB => {0}", ConfigurationManager.AppSettings["dbFolder"]);
            Console.WriteLine("CACHE => {0}", ConfigurationManager.AppSettings["cacheFolder"]);
            LocalStore store = new LocalStore(ConfigurationManager.AppSettings);
            // 1 - hash a file. -> the first argument will be a file name. the hash will be output on the console.
            if (args.Length == 0 || args.Length == 1)
            {
                Console.WriteLine("Usage: kraken in <file> || kraken out <checksum>");
            } else if (args[0] == "in") 
            {
                string fileName = args[1];
                // we'll test to see if the file exists.
                try {
                    string checksum = ChecksumUtil.ComputeChecksum(ChecksumType.SHA1, fileName);
                    store.StoreFile(fileName, checksum);
                    Console.WriteLine("Checksum {0} => {1}", fileName, checksum);
                } catch (FileNotFoundException) {
                    Console.WriteLine("File {0} does not exist.", fileName);
                }
            } else { // we are now trying to retrieve the file. let's print it out to STDIN first.
                string checksum = args[1];
                using (Stream s = store.GetFile(checksum)) {
                    Console.WriteLine("File Stored: {0} => {1}\n", checksum, s.Length);
                    PrintToConsole(s);
                }
            }
            store.Dispose();
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
