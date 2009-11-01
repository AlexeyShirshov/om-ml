using System;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using TestsCodeGenLib;
using WXML.Model;
using System.Xml;
using WXML.CodeDom.CodeDomExtensions;
using WXML2Linq;
using WXML.Model.Database.Providers;
using WXML.Model.Descriptors;
using WXML.SourceConnector;
using System.Data.Linq;
using System.ComponentModel;

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

                LinqContextGenerator gen = new LinqContextGenerator(model, new WXML.CodeDom.WXMLCodeDomGeneratorSettings());

                //CodeCompileFileUnit u = gen.GetFullSingleUnit(LinqToCodedom.CodeDomGenerator.Language.VB);

                Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.VB));

                Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.CSharp));

                Assert.IsNotNull(gen.Compile(LinqToCodedom.CodeDomGenerator.Language.VB));

                Assert.IsNotNull(gen.Compile(LinqToCodedom.CodeDomGenerator.Language.CSharp));
            }
        }

        [TestMethod]
        public void TestGenerateEmpty()
        {
            WXMLModel model = new WXMLModel();

            Assert.IsNotNull(model);

            model.LinqSettings = new LinqSettingsDescriptor()
            {
                ContextName = "TestCtxDataContext"
            };

            LinqContextGenerator gen = new LinqContextGenerator(model, new WXML.CodeDom.WXMLCodeDomGeneratorSettings());

            Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.VB));

            Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.CSharp));

            Assert.IsNotNull(gen.Compile(LinqToCodedom.CodeDomGenerator.Language.VB));

            Assert.IsNotNull(gen.Compile(LinqToCodedom.CodeDomGenerator.Language.CSharp));
        }

        [TestMethod]
        public void TestCompareLinqCtx()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "ent1,ent2,1to2");

            Assert.AreEqual(3, sv.GetSourceFragments().Count());

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel(false, relation1to1.Default, true, false);

            Assert.AreEqual(3, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());

            model.LinqSettings = new LinqSettingsDescriptor()
            {
                ContextName = "TestCtxDataContext"
            };

            model.Namespace = "LinqCodeGenTests";

            LinqContextGenerator gen = new LinqContextGenerator(model, new WXML.CodeDom.WXMLCodeDomGeneratorSettings());

            Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.CSharp));

            Assembly assembly = gen.Compile(LinqToCodedom.CodeDomGenerator.Language.CSharp);

            Assert.IsNotNull(assembly);

            Type ctxType = assembly.GetType(
                string.IsNullOrEmpty(model.Namespace) ? model.LinqSettings.ContextName
                    : model.Namespace + "." + model.LinqSettings.ContextName
            );

            Assert.IsNotNull(ctxType);

            DataContext ctx = (DataContext)Activator.CreateInstance(ctxType, GetTestDBConnectionString());

            Assert.IsNotNull(ctx);

            ctx.Log = Console.Out;

            TestCtxDataContext realCtx = new TestCtxDataContext(GetTestDBConnectionString());

            Type rctxType = typeof (TestCtxDataContext);

            foreach (PropertyInfo pi in ctxType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                PropertyInfo rpi = rctxType.GetProperties().Single(item => item.Name == pi.Name);

                Assert.AreEqual(rpi.Attributes, pi.Attributes);
                Assert.AreEqual(rpi.CanRead, pi.CanRead);
                Assert.AreEqual(rpi.CanWrite, pi.CanWrite);

                Assert.AreEqual(rpi.GetGetMethod().Attributes, pi.GetGetMethod().Attributes);

                IListSource ent1s = (IListSource)pi.GetValue(ctx, null);

                Assert.IsNotNull(ent1s.GetList());

                Assert.AreEqual(((IListSource)rpi.GetValue(realCtx, null)).GetList().Count, ent1s.GetList().Count);
            }
        }

        [TestMethod]
        public void TestCompareLinqCtxMoreTables()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "ent1,ent2,1to2,ent3,3to3,complex_pk,complex_fk,aspnet_Applications,aspnet_Membership,aspnet_Users");

            Assert.AreEqual(10, sv.GetSourceFragments().Count());

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel(false, relation1to1.Default, false, false);

            Assert.AreEqual(10, model.GetSourceFragments().Count());

            Assert.AreEqual(8, model.GetEntities().Count());

            model.LinqSettings = new LinqSettingsDescriptor
            {
                ContextName = "TestCtxDataContext"
            };

            model.Namespace = "LinqCodeGenTests";

            LinqContextGenerator gen = new LinqContextGenerator(model, new WXML.CodeDom.WXMLCodeDomGeneratorSettings());

            Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.CSharp));

            Assembly assembly = gen.Compile(LinqToCodedom.CodeDomGenerator.Language.CSharp);

            Assert.IsNotNull(assembly);

            Type ctxType = assembly.GetType(
                string.IsNullOrEmpty(model.Namespace) ? model.LinqSettings.ContextName
                    : model.Namespace + "." + model.LinqSettings.ContextName
            );

            Assert.IsNotNull(ctxType);

            DataContext ctx = (DataContext)Activator.CreateInstance(ctxType, GetTestDBConnectionString());

            Assert.IsNotNull(ctx);

            ctx.Log = Console.Out;

            TestCtxDataContext realCtx = new TestCtxDataContext(GetTestDBConnectionString());

            Type rctxType = typeof(TestCtxDataContext);

            foreach (PropertyInfo pi in ctxType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                PropertyInfo rpi = rctxType.GetProperties().SingleOrDefault(item => item.Name == pi.Name);

                Assert.IsNotNull(rpi, "Cannot find property {0}", pi.Name);

                Assert.AreEqual(rpi.Attributes, pi.Attributes);
                Assert.AreEqual(rpi.CanRead, pi.CanRead);
                Assert.AreEqual(rpi.CanWrite, pi.CanWrite);

                Assert.AreEqual(rpi.GetGetMethod().Attributes, pi.GetGetMethod().Attributes);

                IListSource ent1s = (IListSource)pi.GetValue(ctx, null);

                Assert.IsNotNull(ent1s.GetList());

                Assert.AreEqual(((IListSource)rpi.GetValue(realCtx, null)).GetList().Count, ent1s.GetList().Count);
            }
        }

        [TestMethod]
        public void TestAdventureWorksLinq()
        {
            SourceView view;

            BinaryFormatter f = new BinaryFormatter();

            ResolveEventHandler d = null;
            d = (sender, args) =>
            {
                AppDomain.CurrentDomain.AssemblyResolve -= d;
                return typeof(WXMLModel).Assembly;
            };
            AppDomain.CurrentDomain.AssemblyResolve += d;

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream fs = assembly.GetManifestResourceStream(
                string.Format("{0}.Files.{1}", assembly.GetName().Name, "AdventureWorks.sourceview")))
            {
                Assert.IsNotNull(fs);
                view = (SourceView)f.Deserialize(fs);
            }

            Assert.IsNotNull(view);

            WXMLModel model = new WXMLModel
            {
                LinqSettings = new LinqSettingsDescriptor
                {
                    ContextName = "TestCtxDataContext"
                },
                Namespace = "LinqCodeGenTests"
            };

            SourceToModelConnector c = new SourceToModelConnector(view, model);

            c.ApplySourceViewToModel(false, relation1to1.Default, false, false);

            LinqContextGenerator gen = new LinqContextGenerator(model);
            
            Console.WriteLine(gen.GenerateCode(LinqToCodedom.CodeDomGenerator.Language.CSharp));
        }

        public static string GetTestDB()
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Databases\test.mdf"));
        }

        public static string GetTestDBConnectionString()
        {
            return @"Server=.\sqlexpress;AttachDBFileName='" + GetTestDB() + "';User Instance=true;Integrated security=true;";
        }

    }
}
