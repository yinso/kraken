using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Kraken.Util;

namespace Kraken.Unique
{
    public class Dupe : IEnumerable<Node> {

        List<Node> paths = new List<Node>();

        public string Checksum { get ; internal set; }

        public bool IsDirectory { get; internal set; }

        public bool IsFile { get { return !IsDirectory; } }

        public Dupe(string checksum, bool isDir) {
            Checksum = checksum;
            IsDirectory = isDir;
        }

        public void Add(Node node) {
            paths.Add(node);
            node.Dupe = this;
        }

        public bool IsSubDupe
        {
            get
            {
                if (!IsDupe)
                    return false;
                foreach (Node node in paths) {
                    if (node.IsTop)
                        return false;
                    else if (!node.Parent.IsDupe)
                        return false;
                }
                return true;
            }
        }

        public bool IsDupe {
            get { return paths.Count > 1; }
        }

        public List<Dupe> GetSubDupes() {
            if (!IsDupe)
                throw new Exception("not_a_duplicate");
            if (!IsDirectory)
                throw new Exception("subdupes_only_work_on_directories");
            Node node = paths[0];
            List<Dupe> children = new List<Dupe>();
            foreach (Node child in node) {
                children.Add(child.Dupe);
            }
            return children;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<Node> GetEnumerator() {
            return paths.GetEnumerator();
        }

        public override string ToString()
        {
            if (IsDupe)
            {
                string[] dupes = new string[paths.Count];
                for (int i = 0; i < paths.Count; ++i) { 
                    dupes[i] = paths[i].FilePath;
                }
                return string.Format("<Dupe:{0}>", string.Join(",", dupes));
            } else {
                return "<Dupe:none>";
            }
        }

    }

    public class Node : IEnumerable<Node> {
        List<Node> children = new List<Node>();

        public string FilePath { get; internal set; }

        public string FileName
        {
            get
            {
                return Path.GetFileName(FilePath);
            }
        }

        public string Checksum { get; internal set; }

        public bool IsDirectory { get; internal set; }

        List<string> checksums = new List<string>();

        public bool IsDupe { 
            get {
                if (Dupe == null)
                    return false;
                return Dupe.IsDupe; // the question is - will I have recursive values here?
            } 
        }

        public Dupe Dupe { get; internal set; } // this is the tricky part... it might return null.

        public Node Parent { get; internal set; }

        public bool IsTop { get { return Parent == null; } }

        public Node(string filePath, string checksum, bool isDirectory) {
            FilePath = filePath;
            Checksum = checksum;
            IsDirectory = isDirectory;
        }

        public void AddChildren(Node node)
        {
            if (!IsDirectory)
                throw new Exception("not_a_directory");
            children.Add(node);
            checksums.Add(node.Checksum);
            node.Parent = this;
        }

        public void ComputeChildrenChecksum()
        {
            string content = string.Join("", checksums.ToArray());
            Checksum = ChecksumUtil.ComputeChecksumOfString(ChecksumType.SHA1, content);
        }

        internal Node()
        {
        }

        public IEnumerator<Node> GetEnumerator() {
            return children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            return string.Format("<{0}>", FilePath);
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
        Dictionary<string, Node> pathToHash = new Dictionary<string, Node>();
        // this one below WILL duplicate...
        Dictionary<string, Dupe> hashToDupe = new Dictionary<string, Dupe>();

        List<Dupe> duplicates = new List<Dupe>();

        List<Node> parentNodes = new List<Node>();

        public Unique()
        {

        }

        public void Process(string path)
        {
            if (Directory.Exists(path))
            {
                ProcessDiretory(path);
            } else
            {
                throw new Exception(string.Format("path_not_found: {0}", path));
            }
        }

        public Node ProcessDiretory(string dirPath)
        {
            /// how to calculate the hash of the whole directory?
            /// it is comprised of the hash of the files + the subdirectories.
            Console.WriteLine("Process Directory: {0}", dirPath);
            Node current = new Node();
            current.IsDirectory = true;
            current.FilePath = dirPath;
            foreach (string file in Directory.GetFiles(dirPath))
            {
                current.AddChildren(ProcessFile(file));
            }

            foreach (string dir in Directory.GetDirectories(dirPath))
            {
                current.AddChildren(ProcessDiretory(dir));
            }

            current.ComputeChildrenChecksum();
            AddDupe(dirPath, current, true);
            return current;
        }

        public Node ProcessFile(string filePath)
        {
            Console.WriteLine("Process File: {0}", filePath);
            if (pathToHash.ContainsKey(filePath)) // we've visited this file before... shouldn't get here.
                return pathToHash[filePath];
            string checksum = ChecksumUtil.ComputeChecksum(ChecksumType.SHA1, filePath);
            Node current = new Node(filePath, checksum, false);
            AddDupe(filePath, current, false);
            return current;
        }

        public void AddDupe(string path, Node node, bool isDir)
        {
            pathToHash [path] = node;
            string checksum = node.Checksum;
            if (hashToDupe.ContainsKey(checksum))
            {
                if (!hashToDupe [checksum].IsDupe)
                    duplicates.Add(hashToDupe [checksum]);
                hashToDupe [checksum].Add(node);
            } else
            {
                hashToDupe[checksum] = new Dupe(checksum, isDir);
                hashToDupe[checksum].Add(node);
            }
        }

        public void ShowDupesHTML()
        {
            NormalizeDupes();
            string tempFilePath = Guid.NewGuid().ToString() + ".html";
            using (FileStream fs = File.Open(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter writer = new StreamWriter(fs)) {
                    writer.Write("<html><head><title>Duplicate Files and Directories</title>" +
                                "</head>" +
                                "<body><h3>Duplicate Files and Directories</h3><ul>");
                    foreach (Dupe dupe in duplicates) {
                        WriteDupe(dupe, writer, false);
                    }
                    writer.Write("</ul></body></html>");
                }
            }
            System.Diagnostics.Process.Start(tempFilePath);
        }

        void NormalizeDupes() {
            foreach (Dupe dupe in duplicates.ToArray())
                if (dupe.IsSubDupe)
                    duplicates.Remove(dupe);
        }

        void WriteDupe(Dupe dupe, StreamWriter writer, bool subDupe)
        {
            writer.Write("<li class='");
            writer.Write(dupe.IsDirectory ? "directory" : "file");
            writer.Write("'><p><b>");
            writer.Write(dupe.IsDirectory ? "Directories" : "Files"); // everything under the directory is a dupe.
            writer.Write("</b> (Checksum: ");
            writer.Write(dupe.Checksum);
            writer.Write(")</p><ul>");
            foreach (Node node in dupe)
            {
                writer.Write("<li><a href=\"file://");
                writer.Write(node.FilePath);
                writer.Write("\">");
                writer.Write(subDupe ? node.FileName : node.FilePath);
                writer.Write("</a>");
                writer.Write("</li>");
            }

            writer.Write("</ul>");
            if (dupe.IsDirectory)
            {
                writer.Write("<ul>");
                foreach (Dupe child in dupe.GetSubDupes()) {
                    WriteDupe(child, writer, true);
                }
                writer.Write("</ul>");
            }
            writer.Write("</li>");
        }
    }
}

