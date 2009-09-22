using System;
using System.IO;
using System.Xml.Schema;

namespace WXML.Model
{
    internal class ResourceManager
    {
        public static XmlSchema GetXmlSchema(string schemaName)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string ass = "WXML.Model";//assembly.GetName().Name;
            string resourceName = string.Format("{0}.Schemas.{1}.xsd", ass, schemaName);
            //XmlSchema schema = new XmlSchema();

            using(Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new WXMLParserException(String.Format("Cannot load resource {0} from assembly {1}", resourceName, assembly.GetName().Name));
                return XmlSchema.Read(stream, null);
            }
        }

        //public static XmlDocument GetXmlDocument(string documentName)
        //{
        //    string extension = "xml";
        //    XmlDocument doc = new XmlDocument();
        //    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
        //    string resourceName;
        //    resourceName = string.Format("{0}.{1}.{2}", assembly.GetName().Name, documentName, extension);

        //    doc.Load(assembly.GetManifestResourceStream(resourceName));
        //    return doc;
        //}

        //private static void schemaValidationEventHandler(object sender, ValidationEventArgs e)
        //{

        //}
    }
}
