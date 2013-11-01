using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Kraken.Util;

namespace Kraken.Unique
{
    public class Dupe : IEnumerable<string> {

        List<string> paths = new List<string>();

        public string Checksum { get ; internal set; }

        public Dupe(string checksum) {
            Checksum = checksum;
        }

        public void Add(string path) {
            paths.Add(path);
        }

        public bool isDuplicate {
            get { return paths.Count > 1; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator() {
            return paths.GetEnumerator();
        }

    }

    public class Unique
    {
        // we should hold the following.
        // source to hash mapping.
        // hash to source mapping.
        // 
        // we should also map directories.
        // 
        // for now we'll hold the whole thing in memory.
        // 
        // 
        Dictionary<string, string> pathToHash = new Dictionary<string, string>();
        // this one below WILL duplicate...
        Dictionary<string, Dupe> hashToDupe = new Dictionary<string, Dupe>();

        List<Dupe> duplicates = new List<Dupe>();

        public Unique()
        {

        }

        public void Process(string path)
        {
            if (Directory.Exists(path))
            {
                ProcessDiretory(path);
            } else if (File.Exists(path))
            {
                ProcessFile(path);
            } else
            {
                throw new Exception(string.Format("path_not_found: {0}", path));
            }
        }

        public void ProcessDiretory(string dirPath)
        {
            Console.WriteLine("Process Directory: {0}", dirPath);
            foreach (string file in Directory.GetFiles(dirPath))
            {
                ProcessFile(file);
            }

            foreach (string dir in Directory.GetDirectories(dirPath))
            {
                ProcessDiretory(dir);
            }
        }

        public void ProcessFile(string filePath)
        {
            Console.WriteLine("Process File: {0}", filePath);
            if (pathToHash.ContainsKey(filePath)) // we've visited this file before... shouldn't get here.
                return;
            string checksum = ChecksumUtil.ComputeChecksum(ChecksumType.SHA1, filePath);
            pathToHash [filePath] = checksum;
            if (hashToDupe.ContainsKey(checksum))
            {
                if (!hashToDupe [checksum].isDuplicate)
                    duplicates.Add(hashToDupe [checksum]);
                hashToDupe [checksum].Add(filePath);
            } else
            {
                hashToDupe[checksum] = new Dupe(checksum);
                hashToDupe[checksum].Add(filePath);
            }
        }

        public void ShowDupes()
        {
            foreach (Dupe dupe in duplicates)
            {
                Console.WriteLine("Duplicates: {0}", dupe.Checksum);
                foreach(string filePath in dupe) {
                    Console.WriteLine("   {0}", filePath);
                }
                Console.WriteLine("");
            }
        }

    }
}

