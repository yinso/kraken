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

        /*
        public static void UsingMongoDB ()
        {
            MongoClient client = new MongoClient ();
            MongoServer server = client.GetServer ();
            MongoDatabase database = server.GetDatabase ("test");
            MongoCollection posts = database.GetCollection ("post");
            Console.WriteLine("Using MongoDB START");
            foreach (BsonDocument doc in posts.FindAllAs(typeof(BsonDocument))) {
                Console.WriteLine("Mongo: {0}", doc);
            }
            Console.WriteLine("Using MongoDB END");
        }
        
        // http://www.codeguru.com/csharp/.net/net_data/sortinganditerating/article.php/c10487/Create-ProviderIndependent-Data-Access-Code-with-ADONET-20.htm
        public static void UsingDbProviders ()
        {
            using (DataTable table = DbProviderFactories.GetFactoryClasses()) {
                foreach (DataRow row in table.Rows) {
                    Console.WriteLine("Name: {0}, {1}, {2}, {3}", row[0], row[1], row[2], row[3]);
                }
            }
        }
        

        public static void UsingFirebird ()
        {
            string connString = "ServerType=1;User=sysdba;Password='masterkey';Database='localhost:/Users/yc/temp/firebird/firstdb.fdb';DataSource=localhost";
            try {
                using (IDbConnection conn = new FbConnection(connString)) {
                    conn.Open ();
                    using (IDbCommand cmd = conn.CreateCommand()) {
                        cmd.CommandText = "select * from sales_catalog";
                        using (IDataReader reader = cmd.ExecuteReader()) {
                            while (reader.Read()) {
                                Console.WriteLine ("Read from Firebird {0}, {1}, {2}", reader [0], reader [1], reader [2]);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Console.WriteLine("Using Firebird Failed: {0}", e);
            }
        }
        //*/

        public static void EncryptionThread ()
        {
            string payload = "please encrypt me please";
            byte[] key = GetRandomBytes(16);
            byte[] iv = GetRandomBytes(16);
            byte[] encrypted = Encrypt(payload, key, iv);
            string decrypted = Decrypt(encrypted, key, iv);
            Console.WriteLine ("Hello World! {0}, {1}, {2}", encrypted, decrypted, decrypted == payload);
        }
        
        public static byte[] GetRandomBytes (int size)
        {
            byte[] bytes = new byte[size];
            var rng2 = new RNGCryptoServiceProvider();
            rng2.GetBytes(bytes); 
            return bytes;
        }
        
        public static void CompressThread ()
        {
            string payload = "This is the new payload - compress me please";
            byte[] compressed = Compress(payload);
            string decompressed = Decompress(compressed);
            Console.WriteLine("Gzip Compression {0}, {1}, {2}", compressed, decompressed, decompressed == payload);
        }
        
        public static byte[] Compress (string text)
        {
            byte[] result;
            using (MemoryStream s = new MemoryStream()) {
                using (GZipStream gs = new GZipStream(s, CompressionMode.Compress)) {
                    using (StreamWriter writer = new StreamWriter(gs)) {
                        writer.Write(text);
                    }
                }
                result = s.ToArray();
            }
            return result;
        }
        
        public static string Decompress (byte[] compressed)
        {
            string result;
            using (MemoryStream s = new MemoryStream(compressed)) {
                using (GZipStream gs = new GZipStream(s, CompressionMode.Decompress)) {
                    using (StreamReader reader = new StreamReader(gs)) {
                        result = reader.ReadToEnd();
                    }
                }
            }
            return result;
        }
        
        public static byte[] Encrypt(string text, byte[] key, byte[] iv) {
            byte[] result;
            using (Aes myAes = Aes.Create()) {
                myAes.Key = key;
            myAes.IV = iv;
            ICryptoTransform encryptor = myAes.CreateEncryptor(key, iv);
            using (MemoryStream s = new MemoryStream()) {
                using (CryptoStream cs = new CryptoStream(s, encryptor, CryptoStreamMode.Write)) {
                    using (StreamWriter writer = new StreamWriter(cs)) {
                        writer.Write(text);
                    }
                }
                result = s.ToArray();
            }
        }
        return result;
    }

    public static string Decrypt(byte[] encrypted, byte[] key, byte[] iv) {
        string result;
        using (Aes aes = Aes.Create()) {
            aes.Key = key;
            aes.IV = iv;
            ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
            using (MemoryStream s = new MemoryStream(encrypted)) {
                using (CryptoStream cs = new CryptoStream(s, decryptor, CryptoStreamMode.Read)) {
                    using (StreamReader reader = new StreamReader(cs)) {
                        result = reader.ReadToEnd();
                    }
                }
            }
        }
        return result;
    }
    }
}
