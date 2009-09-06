﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WXML.Model;
using WXML.Model.Database.Providers;
using WXML.Model.Descriptors;
using WXML.SourceConnector;

namespace WXMLTests
{
    /// <summary>
    /// Summary description for MSSQLSourceProvider
    /// </summary>
    [TestClass]
    public class MSSQLSourceProvider
    {
        public MSSQLSourceProvider()
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
        public void TestCreateTable()
        {
            var p = new MSSQLProvider(null, null);
            var sf = new SourceFragmentDefinition("sdfdsf", "tbl", "dbo");
            StringBuilder script = new StringBuilder();
            
            p.GenerateCreateScript(new[]
            {
                new ScalarPropertyDefinition(null, "Prop", "Prop", Field2DbRelations.None,
                    null, new TypeDefinition("dfg", typeof(int)), 
                    new SourceFieldDefinition(sf, "col1"), 
                    AccessLevel.Private, AccessLevel.Public)
            }, script, false);

            Assert.AreEqual(string.Format("CREATE TABLE dbo.tbl(col1 int NULL);{0}{0}", Environment.NewLine), script.ToString());
        }

        [TestMethod]
        public void TestGenerateScript()
        {
            var p = new MSSQLProvider(GetTestDB(), null);

            var sv = p.GetSourceView(null, "ent1");

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel();

            Assert.AreEqual(1, model.GetActiveEntities().Count());
            Assert.AreEqual(1, model.GetSourceFragments().Count());

            var msc = new ModelToSourceConnector(new SourceView(), model);

            string script = msc.GenerateSourceScript(p, false);

            Assert.IsFalse(string.IsNullOrEmpty(script));

            Console.WriteLine(script);
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
