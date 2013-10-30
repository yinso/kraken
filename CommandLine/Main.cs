using System;
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

namespace Kraken.CommandLine
{
	class MainClass
	{
		public static void Main(string[] args)
        {
            LocalStore store = new LocalStore();
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
