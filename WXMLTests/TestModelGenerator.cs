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
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), "test");
            SourceView view = p.GetSourceView();

            Assert.AreEqual(143, view.GetColumns().Count());

            Assert.AreEqual(31, view.GetTables().Count());
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
