using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using System.IO;
using WXML.Model;
using WXML.Model.Descriptors;

namespace TestsCodeGenLib
{
    /// <summary>
    /// Summary description for TestOrmXmlGenerator
    /// </summary>
    [TestClass]
    public class TestOrmXmlGenerator
    {

        [TestMethod]
        public void TestGenerate()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestCodeGen(stream);
            }
        }

        [TestMethod]
        public void TestExtensionSave()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml("<greeting>hi!</greeting>");

                model.Extensions[new Extension("f")] = xdoc;

                XmlDocument res = model.GetXmlDocument();

                Assert.IsNotNull(res);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(res.NameTable);
                nsmgr.AddNamespace("x", WXMLModel.NS_URI);

                XmlElement extension = res.SelectSingleNode("/x:WXMLModel/x:extensions/x:extension[@name='f']", nsmgr) as XmlElement;
                Assert.IsNotNull(extension);
                
                Assert.AreEqual("greeting", extension.ChildNodes[0].Name);
                Assert.AreEqual("hi!", extension.ChildNodes[0].InnerText);

            }
        }

        [TestMethod]
        [Ignore]
        public void TestIncludeCodeGen()
        {
            using (Stream stream = System.IO.File.Open(@"doc.xml", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                TestCodeGen(stream);
            }
        }

        private static void TestCodeGen(Stream stream)
        {
            WXMLDocumentSet wxmlDocumentSet;
            using (XmlReader rdr = XmlReader.Create(stream))
            {

                WXMLModel schemaDef = WXMLModel.LoadFromXml(rdr, new TestXmlUrlResolver());
                wxmlDocumentSet = schemaDef.GetWXMLDocumentSet(new WXML.Model.WXMLModelWriterSettings());

            }

            stream.Position = 0;
            XmlDocument doc = new XmlDocument();
            
            doc.Load(stream);
            XmlDocument xmlDocument = wxmlDocumentSet[0].Document;
            xmlDocument.RemoveChild(xmlDocument.DocumentElement.PreviousSibling);

            Assert.AreEqual<string>(doc.OuterXml, xmlDocument.OuterXml);
        }


        public class TestXmlUrlResolver : XmlUrlResolver
        {
            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                if(absoluteUri.Segments[absoluteUri.Segments.Length-1].EndsWith(".xml"))
                {
                    return
                        File.OpenRead(@"C:\Projects\Framework\Worm\Worm-XMediaDependent\TestsCodeGenLib\" +
                                      absoluteUri.Segments[absoluteUri.Segments.Length - 1]);
                }
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }

    }
}
