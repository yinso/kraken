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
using Kraken.Core;


namespace Kraken.CommandLine
{
	class MainClass
	{

        public static void Main(string[] args)
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            BlobStore store = new BlobStore(appSettings);
            string filePath = "../../Main.cs";
            string checksum = store.SaveBlob(filePath);
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
