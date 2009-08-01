using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using Worm.CodeGen.Core;
using System.IO;
using WXML.Model;

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
            using (Stream stream = TestOrmXmlParse.GetSampleFileStream())
            {
                TestCodeGen(stream);
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
            OrmXmlDocumentSet ormXmlDocumentSet = null;
            XmlDocument xmlDocument = null;
            using (XmlReader rdr = XmlReader.Create(stream))
            {

                WXMLModel schemaDef = WXMLModel.LoadFromXml(rdr, new TestXmlUrlResolver());
                ormXmlDocumentSet = schemaDef.GetOrmXmlDocumentSet(new WXML.Model.WXMLModelWriterSettings());

            }

            stream.Position = 0;
            XmlDocument doc = new XmlDocument();
            
            doc.Load(stream);
            xmlDocument = ormXmlDocumentSet[0].Document;
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
