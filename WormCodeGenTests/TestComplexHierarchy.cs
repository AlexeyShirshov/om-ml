using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WXML.CodeDom;

namespace WormCodeGenTests
{
	/// <summary>
	/// Summary description for TestEntityBasedClass
	/// </summary>
    [TestClass]
    public class TestComplexHierarchy
    {

        [TestMethod]
        public void Testv2SingleFile()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("v2-schema"))
            {
                TestCodeGen.TestCSCodeInternal(stream, new WXMLCodeDomGeneratorSettings { SingleFile = true });
            }
        }

        [TestMethod]
        public void Testv2()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("v2-schema"))
            {
                TestCodeGen.TestCSCodeInternal(stream);
            }
        }
    }
}
