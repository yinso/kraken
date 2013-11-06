using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace WebDav
{
    public abstract class Request
    {
        public abstract void LoadXML(XmlElement xml, XmlNamespaceManager nsMgr, Factory factory);

        public int Depth { get ; internal set; }

        public bool NoRoot { get; internal set; }

        public string Resource { get ; internal set; }
    }
}

