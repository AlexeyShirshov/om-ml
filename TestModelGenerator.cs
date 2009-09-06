using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WXML.Model;
using WXML.Model.Descriptors;
using WXML.Model.Database.Providers;
using WXML.SourceConnector;

namespace TestsSourceModel
{
    /// <summary>
    /// Summary description for TestOrmXmlGenerator
    /// </summary>
    [TestClass]
    public class TestsSourceModel
    {
        [TestMethod]
        public void TestSourceView()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);
            SourceView view = p.GetSourceView();

            Assert.AreEqual(133, view.SourceFields.Count());

            Assert.AreEqual(32, view.GetSourceFragments().Count());
        }

        [TestMethod]
        public void TestSourceViewPatterns()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            Assert.AreEqual(11, p.GetSourceView(null, "aspnet_%").GetSourceFragments().Count());

            Assert.AreEqual(21, p.GetSourceView(null, "(aspnet_%)").GetSourceFragments().Count());

            Assert.AreEqual(16, p.GetSourceView(null, "(aspnet_%,ent%)").GetSourceFragments().Count());

            Assert.AreEqual(1, p.GetSourceView(null, "guid_table").GetSourceFragments().Count());

            Assert.AreEqual(1, p.GetSourceView("test", null).GetSourceFragments().Count());

            Assert.AreEqual(32, p.GetSourceView("test,dbo", null).GetSourceFragments().Count());

            Assert.AreEqual(31, p.GetSourceView("(test)", null).GetSourceFragments().Count());

            Assert.AreEqual(3, p.GetSourceView(null, "ent1,ent2,1to2").GetSourceFragments().Count());
        }

        [TestMethod]
        public void TestFillModel()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "ent1,ent2,1to2");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(3, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());

            Assert.IsNotNull(model.GetEntity("e_dbo_ent1"));
            
            Assert.IsNotNull(model.GetEntity("e_dbo_ent2"));

            Assert.AreEqual(1, model.GetEntity("e_dbo_ent1").GetProperties().Count());

            Assert.AreEqual(1, model.GetEntity("e_dbo_ent1").GetPkProperties().Count());

            Assert.IsTrue(model.GetEntity("e_dbo_ent1").GetPkProperties().First().HasAttribute(Field2DbRelations.PrimaryKey));

            Assert.AreEqual(2, model.GetEntity("e_dbo_ent2").GetProperties().Count());

            Assert.AreEqual(1, model.GetRelations().Count());
        }

        [TestMethod]
        public void TestFillModel2()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "ent1,1to2");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(2, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());
        }

        [TestMethod]
        public void TestFillModel3()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "complex_fk");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(1, model.GetSourceFragments().Count());

            Assert.AreEqual(1, model.GetEntities().Count());
        }

        [TestMethod]
        public void TestFillModel4()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "aspnet_Membership, 3to3");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(2, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());
        }

        [TestMethod]
        public void TestFillHierarchy()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "aspnet_Membership, aspnet_Users");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(2, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());

            EntityDefinition membership = model.GetEntity("e_dbo_aspnet_Membership");
            Assert.IsNotNull(membership);
            
            EntityDefinition users = model.GetEntity("e_dbo_aspnet_Users");
            Assert.IsNotNull(users);

            Assert.AreEqual(membership.BaseEntity, users);

            Assert.AreEqual(2, membership.GetSourceFragments().Count());

            Assert.AreEqual(1, membership.OwnSourceFragments.Count());

            Assert.AreEqual(1, users.GetSourceFragments().Count());

            Assert.AreEqual(1, users.OwnSourceFragments.Count());

            Assert.IsNull(users.BaseEntity);
        }

        [TestMethod]
        public void TestFillUnify()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "aspnet_Membership, aspnet_Users");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel(false, relation1to1.Unify, true, true);

            Assert.AreEqual(2, model.GetSourceFragments().Count());

            Assert.AreEqual(1, model.GetEntities().Count());

            Assert.AreEqual(2, model.GetEntities().Single().GetSourceFragments().Count());
        }

        [TestMethod]
        public void TestFillModelRelations()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "aspnet_Applications,aspnet_Paths");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(2, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());

            var aspnet_Applications = model.GetEntity("e_dbo_aspnet_Applications");
            Assert.IsNotNull(aspnet_Applications);

            var aspnet_Paths = model.GetEntity("e_dbo_aspnet_Paths");
            Assert.IsNotNull(aspnet_Paths);

            Assert.IsNotNull(aspnet_Paths.GetProperty("Application"));
            Assert.IsNotNull(aspnet_Paths.GetProperty("PathId"));
            Assert.IsNotNull(aspnet_Paths.GetProperty("Path"));
            Assert.IsNotNull(aspnet_Paths.GetProperty("LoweredPath"));

            Assert.AreEqual(1, aspnet_Applications.One2ManyRelations.Count());

            Assert.AreEqual(aspnet_Paths, aspnet_Applications.One2ManyRelations.First().Entity);
        }

        [TestMethod]
        public void TestAddNewEntities()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "ent1,ent2,1to2");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(3, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());

            sv = p.GetSourceView(null, "aspnet_Applications");

            c = new SourceToModelConnector(sv, model);
            c.ApplySourceViewToModel();

            Assert.AreEqual(4, model.GetSourceFragments().Count());

            Assert.AreEqual(3, model.GetEntities().Count());
        }

        [TestMethod]
        public void TestAlterEntities()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "ent1,ent2,1to2");

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);
            c.ApplySourceViewToModel();

            Assert.AreEqual(3, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());

            sv = p.GetSourceView(null, "ent1,ent2,1to2");

            c = new SourceToModelConnector(sv, model);
            c.ApplySourceViewToModel();

            Assert.AreEqual(3, model.GetSourceFragments().Count());

            Assert.AreEqual(2, model.GetEntities().Count());
        }

        [TestMethod]
        public void TestDropProperty()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "aspnet_Applications");

            Assert.AreEqual(4, sv.SourceFields.Count);

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(4, model.GetActiveEntities().First().GetActiveProperties().Count());

            SourceFieldDefinition fld = sv.SourceFields.Find(item=>item.SourceFieldExpression == "[Description]");

            sv.SourceFields.Remove(fld);

            Assert.AreEqual(3, sv.SourceFields.Count);

            c.ApplySourceViewToModel(true, relation1to1.Default, true, true);

            Assert.AreEqual(3, model.GetActiveEntities().First().GetActiveProperties().Count());

            sv.SourceFields.Add(fld);

            Assert.AreEqual(4, sv.SourceFields.Count);

            c.ApplySourceViewToModel(true, relation1to1.Default, true, true);

            Assert.AreEqual(4, model.GetActiveEntities().First().GetActiveProperties().Count());
        }

        [TestMethod]
        public void TestDropEntityProperty()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);

            SourceView sv = p.GetSourceView(null, "aspnet_Paths, aspnet_Applications");

            Assert.AreEqual(8, sv.SourceFields.Count);

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();

            Assert.AreEqual(4, model.GetEntity("e_dbo_aspnet_Paths").GetActiveProperties().Count());

            Assert.IsTrue(sv.SourceFields.Remove(sv.SourceFields.Find(item => item.SourceFieldExpression == "[ApplicationId]" && item.SourceFragment.Name == "[aspnet_Paths]")));

            Assert.AreEqual(7, sv.SourceFields.Count);

            c.ApplySourceViewToModel(true, relation1to1.Default, true, true);

            Assert.AreEqual(3, model.GetEntity("e_dbo_aspnet_Paths").GetActiveProperties().Count());
        }

        [TestMethod]
        public void TestM2MSimilarRelations()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TestO2MSimilarRelations()
        {
            Assert.Inconclusive();
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
