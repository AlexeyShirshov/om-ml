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

            Assert.AreEqual(1, model.Relations.Count());
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

            Assert.AreEqual(1, aspnet_Applications.EntityRelations.Count());

            Assert.AreEqual(aspnet_Paths, aspnet_Applications.EntityRelations.First().Entity);
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
