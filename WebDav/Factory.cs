using System;
using System.IO;
using System.Xml;

using Kraken.Http;

namespace WebDav
{
    public class Factory
    {
        PropertyFactory propTypes = new PropertyFactory();
        RequestFactory reqTypes = new RequestFactory();
        public Factory()
        {
            reqTypes.Register("D:propfind", typeof(PropFind));
            propTypes.Register("D:creationdate", typeof(CreationDate));
            propTypes.Register("D:displayname", typeof(DisplayName));
            propTypes.Register("D:getcontentlanguage", typeof(GetContentLanguage));
        }

        public void RegisterPropertyType(string name, Type type) {
            propTypes.Register(name, type);
        }

        public void RegisterRequestType(string name, Type type)
        {
            reqTypes.Register(name, type);
        }

        public Request ParseRequest(HttpRequest request)
        {
            XmlDocument document = new XmlDocument();
            XmlNamespaceManager nsMgr;
            document.Load(request.InputStream);
            // we are done loading...
            // time to setup the XmlNamespace...
            nsMgr = new XmlNamespaceManager(document.NameTable);
            nsMgr.AddNamespace("D", "DAV:");
            // right now we'll have to pass two separate objects.
            Request req = reqTypes.FromXML(document, nsMgr, this);
            req.Resource = request.Path; // NOTE - this is an absolute PATH... we should fix our PATH problem...
            string depth = request.Headers ["Depth"];
            switch (depth.ToLower())
            {
                case "0":
                    req.Depth = 0;
                    break;
                case "1":
                    req.Depth = 1;
                    break;
                case "1,noroot":
                    req.Depth = 1;
                    req.NoRoot = true;
                    break;
                case "infinity":
                    req.Depth = Int32.MaxValue;
                    break;
                case "infinity,noroot":
                    req.Depth = Int32.MaxValue;
                    req.NoRoot = true;
                    break;
            }
            return req;
        }

        public Property PropertyFromXML(XmlElement propItem, XmlNamespaceManager nsMgr)
        {
            return propTypes.FromXML(propItem, nsMgr);
        }
    }
}

