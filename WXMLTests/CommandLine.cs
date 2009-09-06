using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommandLine.Utility;

namespace WXMLTests
{
    /// <summary>
    /// Summary description for CommandLine
    /// </summary>
    [TestClass]
    public class CommandLine
    {
        public CommandLine()
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
        public void TestParseArgs()
        {
            string[] ss =
                @"-f:Admin.xml -l:cs -rm -sp -sF -pmp:m_ -o:Admin\Objects\".Split(' ');
            Arguments args = new Arguments(ss);

            Assert.AreEqual("Admin.xml", args["f"]);
            Assert.AreEqual("cs", args["l"]);
            Assert.AreEqual("m_", args["pmp"]);
            Assert.AreEqual(@"Admin\Objects\", args["o"]);

            Assert.AreEqual("true", args["rm"]);
            Assert.AreEqual("true", args["sp"]);
            string v;
            Assert.IsTrue(args.TryGetParam("sF", out v) && v == "true");

            Assert.IsNull(args["asa"]);

            Assert.AreEqual("world", new Arguments("-hello world".Split(' '))["hello"]);

            Assert.AreEqual("true", new Arguments("-hello".Split(' '))["hello"]);
        }
    }
}
