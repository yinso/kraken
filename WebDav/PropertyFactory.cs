using System;
using System.Collections.Generic;
using System.Xml;

namespace WebDav
{
    public class PropertyFactory
    {
        Dictionary<string, Type> types = new Dictionary<string, Type>();
        public PropertyFactory()
        {
        }

        public void Register(string typeName, Type type) {
            if (types.ContainsKey(typeName))
                throw new Exception("property_factory_duplicate_definition");
            if (!type.IsSubclassOf(typeof(Property)))
                throw new Exception("property_factory_must_be_subclassof_property");
            types[typeName] = type;
        }

        public Property CreatePropety(string typeName)
        {
            if (!types.ContainsKey(typeName))
            {
                return new AnyProperty();
            } 
            Property prop = (Property)Activator.CreateInstance(types[typeName]);
            return prop;
        }

        public Property FromXML(XmlElement xml, XmlNamespaceManager nsMgr)
        {
            // use the original name...
            Property prop = CreatePropety(xml.Name);
            if (prop != null)
            {
                prop.LoadXML(xml, nsMgr);
            }
            return prop;
        }
    }
}

