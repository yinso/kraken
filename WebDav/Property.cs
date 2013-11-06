using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace WebDav
{
    public abstract class Property
    {
        public abstract void LoadXML(XmlElement xml, XmlNamespaceManager nsMgr);
    }

    public class CreationDate : Property {
        public DateTime Inner { get ; set; }

        public CreationDate()
        {
        }

        public override void LoadXML(XmlElement element, XmlNamespaceManager nsMgr)
        {
            // see if we can parse the date.
            if (!string.IsNullOrEmpty(element.InnerText))
            {
                Inner = DateTime.Parse(element.InnerText);
            }
        }

        public override string ToString()
        {
            return string.Format("<D:creationdate>{0}</D:creationdate>", Inner);
        }
    }

    public class DisplayName : Property {
        public string Inner { get ; set; }
        
        public DisplayName()
        {
        }
        
        public override void LoadXML(XmlElement element, XmlNamespaceManager nsMgr)
        {
            // see if we can parse the date.
            if (!string.IsNullOrEmpty(element.InnerText))
            {
                Inner = element.InnerText;
            }
        }
        
        public override string ToString()
        {
            return string.Format("<D:displayname>{0}</D:displayname>", Inner);
        }
    }
    
    public class GetContentLanguage : Property {
        public string Inner { get ; set; }
        
        public GetContentLanguage()
        {
        }
        
        public override void LoadXML(XmlElement element, XmlNamespaceManager nsMgr)
        {
            // see if we can parse the date.
            if (!string.IsNullOrEmpty(element.InnerText))
            {
                Inner = element.InnerText;
            }
        }
        
        public override string ToString()
        {
            return string.Format("<D:getcontentlanguage>{0}</D:getcontentlanguage>", Inner);
        }
    }
    




    public class AnyProperty : Property {

        public XmlElement Inner;

        XmlNamespaceManager nsMgr;

        public AnyProperty()
        {
        }

        public override void LoadXML(XmlElement xml, XmlNamespaceManager nsMgr)
        {
            Inner = xml;
            this.nsMgr = nsMgr;
        }

        public XmlNode SelectSingleNode(string xPath)
        {
            return Inner.SelectSingleNode(xPath, nsMgr);
        }

        public XmlNodeList SelectNodes(string xPath) {
            return Inner.SelectNodes(xPath, nsMgr);
        }

        public override string ToString()
        {
            return Inner.OuterXml;
        }
    }
}

