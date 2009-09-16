using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class TestCreateScript
    {
        public TestCreateScript()
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
            var p = new MSSQLProvider();
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

            var sv = p.GetSourceView();

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel();

            Assert.AreEqual(28, model.GetActiveEntities().Count());
            Assert.AreEqual(32, model.GetSourceFragments().Count());

            var msc = new ModelToSourceConnector(new SourceView(), model);

            string script = msc.GenerateSourceScript(p, false);

            Assert.IsFalse(string.IsNullOrEmpty(script));

            Assert.AreEqual(32, new Regex("CREATE TABLE ").Matches(script).Count);

            Console.WriteLine(script);
        }

        [TestMethod]
        public void TestGenerateScriptUnify()
        {
            var p = new MSSQLProvider(GetTestDB(), null);

            var sv = p.GetSourceView();

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel(false, relation1to1.Unify, true, true);

            Assert.AreEqual(25, model.GetActiveEntities().Count());
            Assert.AreEqual(32, model.GetSourceFragments().Count());

            var msc = new ModelToSourceConnector(new SourceView(), model);

            string script = msc.GenerateSourceScript(p, false);

            Assert.IsFalse(string.IsNullOrEmpty(script));

            Assert.AreEqual(32, new Regex("CREATE TABLE ").Matches(script).Count);

            Console.WriteLine(script);
        }

        [TestMethod]
        public void TestGenerateScriptHierachy()
        {
            var p = new MSSQLProvider(GetTestDB(), null);

            var sv = p.GetSourceView();

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel(false, relation1to1.Hierarchy, true, true);

            Assert.AreEqual(28, model.GetActiveEntities().Count());
            Assert.AreEqual(32, model.GetSourceFragments().Count());

            var msc = new ModelToSourceConnector(new SourceView(), model);

            string script = msc.GenerateSourceScript(p, false);
            Assert.IsFalse(string.IsNullOrEmpty(script));
            Console.WriteLine(script);

            Assert.AreEqual(sv.GetSourceFragments().Count(), new Regex("CREATE TABLE ").Matches(script).Count);
            IEnumerable<SourceConstraint> pks = sv.GetSourceFragments().SelectMany(item=>item.Constraints.Where(cns=>cns.ConstraintType == SourceConstraint.PrimaryKeyConstraintTypeName));
            Assert.AreEqual(pks.Count(), new Regex("PRIMARY KEY CLUSTERED").Matches(script).Count);
            Assert.AreEqual(2, new Regex("UNIQUE NONCLUSTERED").Matches(script).Count);
            Assert.AreEqual(1, new Regex("UNIQUE CLUSTERED").Matches(script).Count);
            Assert.AreEqual(sv.GetSourceFragments().SelectMany(item => item.Constraints.Where(cns => cns.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName)).Count(), new Regex("FOREIGN KEY").Matches(script).Count);

            msc = new ModelToSourceConnector(sv, model);

            script = msc.GenerateSourceScript(p, false);

            Assert.IsTrue(string.IsNullOrEmpty(script), script);

            RelationDefinitionBase r = model.GetActiveRelations().First(item => item.Constraint == RelationConstraint.PrimaryKey);
            r.Constraint = RelationConstraint.Unique;
            r.SourceFragment.Constraints.Single(item => item.ConstraintType == SourceConstraint.PrimaryKeyConstraintTypeName).ConstraintType = SourceConstraint.UniqueConstraintTypeName;
            
            msc = new ModelToSourceConnector(sv, model);

            script = msc.GenerateSourceScript(p, false);

            Assert.IsTrue(string.IsNullOrEmpty(script), script);
        }

        [TestMethod]
        public void TestGenerateScriptDiff()
        {
            var p = new MSSQLProvider(GetTestDB(), null);

            var sv = p.GetSourceView();

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel(false, relation1to1.Hierarchy, true, true);

            Assert.AreEqual(28, model.GetActiveEntities().Count());
            Assert.AreEqual(32, model.GetSourceFragments().Count());

            EntityPropertyDefinition prop = model.GetActiveEntities().SelectMany(item => item.GetProperties().OfType<EntityPropertyDefinition>()).First();
            SourceConstraint c = new SourceConstraint(SourceConstraint.UniqueConstraintTypeName, "xxx");
            c.SourceFields.AddRange(prop.SourceFields.Cast<SourceFieldDefinition>());
            prop.SourceFragment.Constraints.Add(c);

            var msc = new ModelToSourceConnector(p.GetSourceView(), model);
            var tbl = msc.SourceView.GetSourceFragments().Single(item => item.Selector == prop.SourceFragment.Selector
                && item.Name == prop.SourceFragment.Name);

            string script = msc.GenerateSourceScript(p, false);
            Console.WriteLine(script);
            Assert.IsFalse(string.IsNullOrEmpty(script), script);

            Assert.AreEqual(1, new Regex("ADD CONSTRAINT").Matches(script).Count);
            Assert.AreEqual(0, new Regex("DROP CONSTRAINT").Matches(script).Count);

            c = new SourceConstraint(SourceConstraint.UniqueConstraintTypeName, "xxx");
            c.SourceFields.Add(msc.SourceView.GetSourceFields(tbl).First(item=>item.SourceFieldExpression != prop.SourceFields.First().SourceFieldExpression));
            tbl.Constraints.Add(c);

            script = msc.GenerateSourceScript(p, false);
            Assert.AreEqual(1, new Regex("ADD CONSTRAINT").Matches(script).Count);
            Assert.AreEqual(1, new Regex("DROP CONSTRAINT").Matches(script).Count);
        }

        [TestMethod]
        public void TestGenerateScriptDropConstraint()
        {
            var p = new MSSQLProvider(GetTestDB(), null);

            var sv = p.GetSourceView();

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel(false, relation1to1.Hierarchy, true, true);

            Assert.AreEqual(28, model.GetActiveEntities().Count());
            Assert.AreEqual(32, model.GetSourceFragments().Count());

            EntityPropertyDefinition prop = model.GetActiveEntities().SelectMany(item => item.GetProperties().OfType<EntityPropertyDefinition>()).First();
            SourceConstraint c = new SourceConstraint(SourceConstraint.UniqueConstraintTypeName, "xxx");
            c.SourceFields.AddRange(prop.SourceFields.Cast<SourceFieldDefinition>());
            prop.SourceFragment.Constraints.Add(c);

            var msc = new ModelToSourceConnector(p.GetSourceView(), model);
            var tbl = msc.SourceView.GetSourceFragments().Single(item => item.Selector == prop.SourceFragment.Selector
                && item.Name == prop.SourceFragment.Name);
            tbl.Constraints.Add(new SourceConstraint(SourceConstraint.UniqueConstraintTypeName, "xxx"));

            string script = msc.GenerateSourceScript(p, false);
            Assert.IsFalse(string.IsNullOrEmpty(script), script);

            Assert.AreEqual(1, new Regex("DROP CONSTRAINT").Matches(script).Count);
        }

        [TestMethod]
        public void TestGenerateScriptTwoTables()
        {
            var p = new MSSQLProvider(GetTestDB(), null);

            var sv = p.GetSourceView(null, "ent1, 1to2");

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel(false, relation1to1.Hierarchy, true, true);

            Assert.AreEqual(2, model.GetActiveEntities().Count());
            Assert.AreEqual(2, model.GetSourceFragments().Count());

            var msc = new ModelToSourceConnector(new SourceView(), model);

            string script = msc.GenerateSourceScript(p, false);

            Assert.IsFalse(string.IsNullOrEmpty(script));

            Assert.AreEqual(2, new Regex("CREATE TABLE ").Matches(script).Count);

            Console.WriteLine(script);
        }

        [TestMethod]
        public void TestGenerateComplex()
        {
            SourceView sv = TestsSourceModel.TestsSourceModel.CreateComplexSourceView();

            var model = new WXMLModel();

            var smc = new SourceToModelConnector(sv, model);
            smc.ApplySourceViewToModel();

            Assert.AreEqual(2, model.GetActiveEntities().Count());
            Assert.AreEqual(4, model.GetActiveRelations().Count());

            model.GetActiveRelations().First().Constraint = RelationConstraint.Unique;

            var msc = new ModelToSourceConnector(new SourceView(), model);

            var p = new MSSQLProvider(null, null);
            string script = msc.GenerateSourceScript(p, false);

            Assert.IsFalse(string.IsNullOrEmpty(script));
            Console.WriteLine(script);

            Assert.AreEqual(6, new Regex("CREATE TABLE ").Matches(script).Count);

            Assert.AreEqual(2, new Regex("PRIMARY KEY CLUSTERED").Matches(script).Count);

            Assert.AreEqual(8, new Regex("FOREIGN KEY").Matches(script).Count);

            Assert.AreEqual(1, new Regex("UNIQUE CLUSTERED").Matches(script).Count);
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
