using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WXML.Model;
using WXML.Model.Database.Providers;
using WXML.Model.Descriptors;
using WXML.SourceConnector;

namespace WXMLTests.MSSQLSourceProvider
{
    /// <summary>
    /// Summary description for TestScriptAlter
    /// </summary>
    [TestClass]
    public class TestScriptAlter
    {
        public TestScriptAlter()
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
        public void TestAlter()
        {
            var p = new MSSQLProvider(GetTestDB(), null);

            var sv = p.GetSourceView();

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel(false, relation1to1.Hierarchy, true, true, false);

            Assert.AreEqual(28, model.GetActiveEntities().Count());
            Assert.AreEqual(32, model.GetSourceFragments().Count());

            var msc = new ModelToSourceConnector(new SourceView(), model);
            foreach (SourceFragmentDefinition sf in sv.GetSourceFragments().ToArray())
            {
                foreach (SourceFieldDefinition field in sv.GetSourceFields(sf).Where(item=>!item.IsFK))
                {
                    msc.SourceView.SourceFields.Add(new SourceFieldDefinition(new SourceFragmentDefinition(field.SourceFragment.Identifier,
                            field.SourceFragment.Name, field.SourceFragment.Selector), field.SourceFieldExpression, field.SourceType, field.SourceTypeSize, field.IsNullable, field.IsAutoIncrement, field.DefaultValue));
                    break;
                }
            }

            string script = msc.GenerateSourceScript(p, false);
            Assert.IsFalse(string.IsNullOrEmpty(script));
            Console.WriteLine(script);

            Assert.AreEqual(4, new Regex("CREATE TABLE ").Matches(script).Count);
            Assert.AreEqual(26, new Regex("ALTER TABLE ").Matches(script.Remove(script.IndexOf("--Creating primary keys"))).Count);
            IEnumerable<SourceConstraint> pks = sv.GetSourceFragments().SelectMany(item => item.Constraints.Where(cns => cns.ConstraintType == SourceConstraint.PrimaryKeyConstraintTypeName));
            Assert.AreEqual(pks.Count(), new Regex("PRIMARY KEY CLUSTERED").Matches(script).Count);
            Assert.AreEqual(2, new Regex("UNIQUE NONCLUSTERED").Matches(script).Count);
            Assert.AreEqual(1, new Regex("UNIQUE CLUSTERED").Matches(script).Count);
            Assert.AreEqual(sv.GetSourceFragments().SelectMany(item => item.Constraints.Where(cns => cns.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName)).Count(), new Regex("FOREIGN KEY").Matches(script).Count);
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
