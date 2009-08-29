using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WXML.Model.Descriptors;
using WXML.Model.Database.Providers;

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
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), "wtest");
            SourceView view = p.GetSourceView();

            Assert.AreEqual(133, view.GetColumns().Count());

            Assert.AreEqual(32, view.GetTables().Count());
        }

        [TestMethod]
        public void TestSourceViewPatterns()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), "wtest");

            Assert.AreEqual(11, p.GetSourceView(null, "aspnet_%").GetTables().Count());

            Assert.AreEqual(21, p.GetSourceView(null, "!aspnet_%").GetTables().Count());

            Assert.AreEqual(16, p.GetSourceView(null, "!aspnet_%,!ent%").GetTables().Count());

            Assert.AreEqual(1, p.GetSourceView(null, "guid_table").GetTables().Count());

            Assert.AreEqual(1, p.GetSourceView("test", null).GetTables().Count());

            Assert.AreEqual(32, p.GetSourceView("test,dbo", null).GetTables().Count());

            Assert.AreEqual(31, p.GetSourceView("(test)", null).GetTables().Count());
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
