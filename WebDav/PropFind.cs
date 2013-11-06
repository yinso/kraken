using System;
using System.Collections.Generic;
using System.Xml;

namespace WebDav
{
    public class PropFind : Request
    {
        // can take in one of the following.
        // propname
        // allprop
        // prop(any+)

        public bool PropName { get; set; }
        public bool AllProp { get; set; }
        public List<Property> PropList { get; private set; }
        public PropFind()
        {
            PropList = new List<Property>();
        }

        public override void LoadXML(XmlElement element, XmlNamespaceManager nsMgr, Factory factory) {
            if (element.SelectSingleNode("//D:allprop", nsMgr) != null)
            {
                AllProp = true;
            } else if (element.SelectSingleNode("//D:propname", nsMgr) != null)
            {
                PropName = true;
            } else // we should parse the list of the properties... and add them into propList.
            {
                XmlElement prop = (XmlElement)element.SelectSingleNode("//D:prop", nsMgr);
                if (prop != null) {
                    foreach (XmlElement propItem in prop.ChildNodes) {
                        PropList.Add(factory.PropertyFromXML(propItem, nsMgr));
                    }
                } else {
                }
            }
        }
    }
}

