using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TestsCodeGenLib;
using WXML.Model;
using System.Xml;
using WXML.CodeDom.CodeDomExtensions;
using LinqCodeGenerator;

namespace LinqCodeGenTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CodeGenTests
    {
        public CodeGenTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestGenerate()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("sample1"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                LinqCodeDomGenerator gen = new LinqCodeDomGenerator(model, new WXML.CodeDom.WXMLCodeDomGeneratorSettings());

                //CodeCompileFileUnit u = gen.GetFullSingleUnit(LinqToCodedom.CodeDomGenerator.Language.VB);

                Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.VB));

                Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.CSharp));

                Assert.IsNotNull(gen.Compile(LinqToCodedom.CodeDomGenerator.Language.VB));

                Assert.IsNotNull(gen.Compile(LinqToCodedom.CodeDomGenerator.Language.CSharp));
            }
        }
    }
}
