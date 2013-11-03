using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

using Kraken.Util;

namespace Kraken.Core
{
    public class RootPathMap // it somehow feels like this is warranted.
    {
        Dictionary<string, string> inner = new Dictionary<string, string>();
        public RootPathMap(NameValueCollection settings)
        {
            foreach (string key in settings.AllKeys)
            {
                foreach (string val in settings.GetValues(key)) {
                    string root = UriUtil.URLDecode(key);
                    string path = UriUtil.URLDecode(val);
                    if (inner.ContainsKey(root)) {
                        throw new Exception(string.Format("duplicate_root_path: {0}", root));
                    } else {
                        inner[root] = path;
                    }
                }
            }
        }

        public RootPathMap() {

        }

        public NameValueCollection ToSettings()
        {
            NameValueCollection col = new NameValueCollection();
            foreach (KeyValuePair<string, string> keyval in inner)
            {
                col[UriUtil.URLEncode(keyval.Key)] = UriUtil.URLEncode(keyval.Value);
            }
            return col;
        }

        public void Add(string name, string path)
        {
            if (!Directory.Exists(path))
            {
                throw new Exception(string.Format("root_path_must_be_folder: {0}", path));
            } else if (inner.ContainsKey(name)) {
                if (inner[name] != path) {
                    throw new Exception(string.Format("duplicate_root_path_name: {0}", name));
                }
            } else
            {
                inner[name] = path;
            }
        }

        public string MapPath(string filePath)
        {
            // our goal is to see if the filePath has one of the rootPath has a prefix...
            foreach (KeyValuePair<string, string> keyval in inner)
            {
                if (filePath.IndexOf(keyval.Value) == 0) { // match!
                    return FileUtil.ChangePathDirectory(filePath, keyval.Value);
                }
            }
            throw new Exception(string.Format("file_not_under_managed_rootPaths: {0}", filePath));
        }
    }
}

