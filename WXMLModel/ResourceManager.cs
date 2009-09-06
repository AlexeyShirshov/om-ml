using System.Xml.Schema;
using System.Xml;

namespace WXML.Model
{
    internal class ResourceManager
    {
        public static XmlSchema GetXmlSchema(string schemaName)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName;
            resourceName = string.Format("{0}.Schemas.{1}.xsd", assembly.GetName().Name, schemaName);
            XmlSchema schema = new XmlSchema();

            return XmlSchema.Read(assembly.GetManifestResourceStream(resourceName), new ValidationEventHandler(schemaValidationEventHandler));
        }

        public static XmlDocument GetXmlDocument(string documentName)
        {
            string extension = "xml";
            XmlDocument doc = new XmlDocument();
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName;
            resourceName = string.Format("{0}.{1}.{2}", assembly.GetName().Name, documentName, extension);

            doc.Load(assembly.GetManifestResourceStream(resourceName));
            return doc;
        }

        private static void schemaValidationEventHandler(object sender, ValidationEventArgs e)
        {
            
        }
    }
}
