using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Http
{
    public class MimeTypeRegistry
    {
        NameValueCollection extensions = new NameValueCollection();
        NameValueCollection mimetypes = new NameValueCollection();
        Regex spaceRE = new Regex(@"\s+");
        public MimeTypeRegistry()
        {
            foreach (string line in File.ReadAllLines("./mime.types")) {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#")) { // a comment
                    continue;
                } else {
                    string[] typeInfo = spaceRE.Split(trimmed);
                    if (typeInfo.Length > 1) {
                        for (int i = 1; i < typeInfo.Length; ++i) {
                            mimetypes.Add(typeInfo[0], typeInfo[i]);
                            extensions.Add(typeInfo[i], typeInfo[0]);
                        }
                    }
                }
            }
        }

        public string PathToMimeType(string path) {
            string extension = Path.GetExtension(path);
            return extensions[extension.Substring(1)];
        }
    }
}

