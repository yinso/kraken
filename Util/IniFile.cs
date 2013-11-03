using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Kraken.Util
{
    /// <summary>
    /// Ini file.
    /// 
    /// A very simple ini file parser & serializer.
    /// 
    /// </summary>
    public class IniFile 
    {
        string filePath;

        Regex sectionRegex = new Regex(@"^\[\s*([^\]]+)\s*\]");

        Regex keyvalRegex = new Regex(@"^([^\=\s]+)\s*\=\s*([^\;\#]+)");

        Dictionary<string, NameValueCollection> sections = new Dictionary<string, NameValueCollection>();

        NameValueCollection currentSection;

        public IniFile(string path)
        {
            filePath = path;
            loadIniFile();
        }

        void loadIniFile() {
            FileUtil.EnsurePathDirectory(filePath);
            using (FileStream fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read)) {
                parseFile(fs);
            }
        }

        public NameValueCollection GetSection(string sectionName) {
            return sections[sectionName];
        }

        public void SetSection(string sectionName, NameValueCollection col)
        {
            sections[sectionName] = col;
        }

        public bool Contains(string sectionName, string key) {
            return Get(sectionName, key) != null;
        }

        public string Get(string sectionName, string key)
        {
            if (!sections.ContainsKey(sectionName))
            {
                return null;
            }
            return sections[sectionName][key];
        }

        public void Add(string sectionName, string key, string val)
        {
            NameValueCollection current;
            if (!sections.ContainsKey(sectionName))
            {
                sections [sectionName] = new NameValueCollection();
                current = sections [sectionName];
            } else
            {
                current = sections[sectionName];
            }
            current.Add(key, val);
        }

        public void Save()
        {
            Save(filePath);
        }

        public void Save(string filePath)
        {
            this.filePath = filePath;
            // this will save it back to the original place.
            // we'll save it atomically...
            string tempFile = FileUtil.TempFilePath(filePath);
            using (FileStream fs = File.Open(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                Serialize(fs);
                FileUtil.Rename(tempFile, filePath);
            }
        }

        public void Serialize(Stream s)
        {
            foreach (KeyValuePair<string, NameValueCollection> section in sections)
            {
                serializeSectionName(section.Key, s);
                serializeSectionKeyVals(section.Value, s);
            }
        }

        void serializeSectionName(string name, Stream s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format("[{0}]\r\n", name));
            s.Write(bytes, 0, bytes.Length);
        }

        void serializeSectionKeyVals(NameValueCollection keyvals, Stream s)
        {
            foreach (string key in keyvals.AllKeys)
            {
                foreach (string val in keyvals.GetValues(key)) {
                    serializeKeyVal(key, val, s);
                }
            }
        }

        void serializeKeyVal(string key, string val, Stream s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format(" {0} = {1}\r\n", key, val));
            s.Write(bytes, 0, bytes.Length);
        }

        void parseFile(FileStream fs) {
            using (Reader reader = new Reader(fs)) {
                while (reader.PeekByte() != -1) {
                    string line = reader.ReadLine().Trim();
                    // we'll now need to be able to handle the line and see what it means.
                    // 1 - there might be spaces surrounding it... we'll go ahead and trim
                    if (line == "") { // empty line // skip.
                        continue;
                    } else if (line.IndexOf(";") == 0 || line.IndexOf("#") == 0) { // comment line
                        continue;
                    } else if (line.IndexOf("[") == 0) { // a section start.
                        handleSection(line);
                    } else { // key/value line...
                        handleKeyVal(line);
                    }
                }
            }
        }

        void handleSection(string line)
        {
            Match match = sectionRegex.Match(line);
            if (match.Success)
            {
                string sectionName = match.Groups[1].Value;
                if (!sections.ContainsKey(sectionName)) {
                    sections[sectionName] = new NameValueCollection();
                }
                currentSection = sections[sectionName];
            }
        }

        void handleKeyVal(string line)
        {
            if (currentSection == null)
                throw new Exception("inifile_keyval_must_belong_to_section");
            Match match = keyvalRegex.Match(line);
            if (match.Success)
            {
                string key = match.Groups[1].Value;
                string val = match.Groups[2].Value;
                currentSection.Add(key, val);
            }
        }
    }
}

