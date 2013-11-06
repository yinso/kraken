using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace WebDav
{
    class RequestFactory
    {
        Dictionary<string, Type> types = new Dictionary<string, Type>();
        public RequestFactory()
        {
        }
        
        public void Register(string typeName, Type type) {
            if (types.ContainsKey(typeName))
                throw new Exception("request_factory_duplicate_definition");
            if (!type.IsSubclassOf(typeof(Request)))
                throw new Exception("request_factory_must_be_subclassof_request");
            types[typeName] = type;
        }
        
        public Request CreateRequest(string typeName) {
            if (!types.ContainsKey(typeName))
                throw new Exception(string.Format("request_factory_unknown_type: {0}", typeName));
            return (Request)Activator.CreateInstance(types[typeName]);
        }

        internal Request FromXML(XmlDocument document, XmlNamespaceManager nsMgr, Factory factory)
        {
            // first of all - let's get the name of the original element.
            string name = document.DocumentElement.Name;
            Request req = CreateRequest(name);
            if (req != null)
            {
                req.LoadXML(document.DocumentElement, nsMgr, factory);
            }
            return req;
        }
    }
}

