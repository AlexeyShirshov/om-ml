using System.Reflection;
using System.Xml;
using System.IO;

namespace TestsCodeGenLib
{
    public class Resources
    {
        public static Stream GetXmlDocumentStream(string documentName)
        {
            return GetXmlDocumentStream(documentName, Assembly.GetExecutingAssembly());
        }

        public static Stream GetXmlDocumentStream(string documentName, Assembly assembly)
        {
            const string extension = "xml";

            string resourceName = string.Format("{0}.Examples.{1}.{2}", assembly.GetName().Name, documentName, extension);

            return assembly.GetManifestResourceStream(resourceName);
        }

        public static XmlDocument GetXmlDocument(string documentName)
        {
            XmlDocument doc = new XmlDocument();
            using(Stream stream = GetXmlDocumentStream(documentName))
            {
                doc.Load(stream);    
            }
            
            return doc;
        }
    }
}
