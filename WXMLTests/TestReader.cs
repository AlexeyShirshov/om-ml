using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WXML.Model;
using System.Xml;
using WXML.Model.Descriptors;
using TestsCodeGenLib;
using System.IO;

namespace WXMLTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TestReader
    {
        public TestReader()
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
        [Description("Проверка получения свойств")]
        public void TestFillProperties()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();


            WXMLModel ormObjectDef = parser.Model;

            EntityDefinition entity = ormObjectDef.GetEntities()
                .Single(match => match.Identifier == "eArtist" && match.Name == "Artist");

            parser.FillProperties(entity);

            Assert.AreEqual<int>(8, entity.OwnProperties.Count());

            ScalarPropertyDefinition prop = (ScalarPropertyDefinition) entity.GetProperty("ID");
            Assert.IsNotNull(prop);
            //Assert.AreEqual<int>(1, prop.Attributes.Length, "Attributes is undefined");
            Assert.AreEqual<string>("PK", prop.Attributes.ToString(), "Attributes is not correct defined");
            Assert.IsNotNull(prop.SourceFragment, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.SourceFragment.Identifier, "Table.Identifier is undefined");
            Assert.AreEqual<string>("id", prop.SourceFieldExpression, "FieldName is undefined");
            Assert.AreEqual<string>("System.Int32", prop.PropertyType.GetTypeName(null), "PropertyTypeString is undefined");
            Assert.AreEqual<string>("Property ID Description", prop.Description, "Description is undefined");
            Assert.AreEqual<AccessLevel>(AccessLevel.Private, prop.FieldAccessLevel, "FieldAccessLevel");
            Assert.AreEqual<AccessLevel>(AccessLevel.Public, prop.PropertyAccessLevel, "PropertyAccessLevel");
            Assert.AreEqual<string>(prop.Name, prop.PropertyAlias, "PropertyAlias");

            prop = (ScalarPropertyDefinition) entity.GetProperty("Title");
            Assert.IsNotNull(prop);
            //Assert.AreEqual<int>(0, prop.Attributes.Length, "Attributes is undefined");
            Assert.IsNotNull(prop.SourceFragment, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.SourceFragment.Identifier, "Table.Identifier is undefined");
            Assert.AreEqual<string>("name", prop.SourceFieldExpression, "FieldName is undefined");
            Assert.AreEqual<string>("System.String", prop.PropertyType.GetTypeName(null), "PropertyTypeString is undefined");
            Assert.AreEqual<string>("Property Title Description", prop.Description, "Description");
            Assert.AreEqual<AccessLevel>(AccessLevel.Private, prop.FieldAccessLevel, "FieldAccessLevel");
            Assert.AreEqual<AccessLevel>(AccessLevel.Assembly, prop.PropertyAccessLevel, "PropertyAccessLevel");
            Assert.AreEqual<string>(prop.Name, prop.PropertyAlias, "PropertyAlias");

            prop = (ScalarPropertyDefinition) entity.GetProperty("DisplayTitle");
            Assert.IsNull(prop);
            prop = (ScalarPropertyDefinition) entity.GetProperty("DisplayName");
            Assert.IsNotNull(prop);
            Assert.AreEqual<string>("DisplayTitle", prop.Name, "Name is undefined");
            //Assert.AreEqual<int>(0, prop.Attributes.Length, "Attributes is undefined");
            Assert.IsNotNull(prop.SourceFragment, "Table is undefined");
            Assert.AreEqual<string>("tblArtists", prop.SourceFragment.Identifier, "Table.Identifier is undefined");
            Assert.AreEqual<string>("display_name", prop.SourceFieldExpression, "FieldName is undefined");
            Assert.AreEqual<string>("System.String", prop.PropertyType.GetTypeName(null), "PropertyTypeString is undefined");
            Assert.AreEqual<string>("Property Title Description", prop.Description, "Property Title Description");
            Assert.AreEqual<AccessLevel>(AccessLevel.Family, prop.FieldAccessLevel, "FieldAccessLevel");
            Assert.AreEqual<AccessLevel>(AccessLevel.Family, prop.PropertyAccessLevel, "PropertyAccessLevel");
            Assert.AreEqual<string>("DisplayName", prop.PropertyAlias, "PropertyAlias");

            prop = (ScalarPropertyDefinition) entity.GetProperty("Fact");

            //Assert.AreEqual<int>(1, prop.Attributes.Length, "Attributes.Factory absent");
            Assert.AreEqual<string>("Factory", prop.Attributes.ToString(), "Attributes.Factory invalid");

            prop = (ScalarPropertyDefinition) entity.GetProperty("TestInsDef");

            //Assert.AreEqual<int>(1, prop.Attributes.Length, "Attributes.Factory absent");
            Assert.AreEqual<string>("InsertDefault", prop.Attributes.ToString(), "Attributes.InsertDefault invalid");

            prop = (ScalarPropertyDefinition) entity.GetProperty("TestNullabe");

            Assert.AreEqual<Type>(typeof(int?), prop.PropertyType.ClrType);
            Assert.IsFalse(prop.Disabled, "Disabled false");

            prop = (ScalarPropertyDefinition) entity.GetProperty("TestDisabled");
            Assert.IsTrue(prop.Disabled, "Disabled true");
        }

        [TestMethod]
        [Description("Проверка получения свойств")]
        public void TestFillSuppressedProperties()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser = null;
            using (XmlReader rdr = XmlReader.Create(Resources.GetXmlDocumentStream("suppressed")))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();

            WXMLModel ormObjectDef = parser.Model;

            EntityDefinition entity = ormObjectDef.GetEntity("e11");

            parser.FillEntities();

            Assert.AreEqual<int>(1, entity.SuppressedProperties.Count, "SuppressedProperties.Count");

            PropertyDefinition prop = entity.GetProperties()
                .Single(item=>item.PropertyAlias==entity.SuppressedProperties[0]);

            Assert.AreEqual<string>("Prop1", prop.Name, "SuppressedPropertyName");
            Assert.IsTrue(prop.IsSuppressed, "SuppressedPropery.IsSuppressed");

            EntityDefinition completeEntity = entity;//.CompleteEntity;

            prop = completeEntity.GetProperty("Prop1");
            Assert.IsNotNull(prop);
            Assert.IsTrue(prop.IsSuppressed);

            prop = completeEntity.GetProperty("Prop11");
            Assert.IsNotNull(prop);
            Assert.IsFalse(prop.IsSuppressed);

        }

        [TestMethod]
        [Description("Проверка получения свойств")]
        public void TestFillPropertiesWithGroups()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            using (XmlReader rdr = XmlReader.Create(GetFile("groups")))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr, new TestXmlUrlResolver());
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();

            WXMLModel model = parser.Model;

            EntityDefinition entity = model.GetEntities()
                .Single(match => match.Identifier == "e1");

            parser.FillProperties(entity);

            Assert.AreEqual<int>(7, entity.OwnProperties.Count());

            PropertyDefinition prop = entity.GetProperty("Identifier1");
            Assert.IsNull(prop);
            prop = entity.GetProperty("ID");
            Assert.IsNotNull(prop);

            Assert.IsNull(prop.Group);

            prop = entity.GetProperty("prop1");
            Assert.IsNotNull(prop);
            Assert.IsNotNull(prop.Group);
            Assert.AreEqual("grp", prop.Group.Name);
            Assert.IsTrue(prop.Group.Hide);

            prop = entity.GetProperty("prop4");
            Assert.IsNotNull(prop);
            Assert.IsNotNull(prop.Group);
            Assert.AreEqual("grp1", prop.Group.Name);
            Assert.IsFalse(prop.Group.Hide);

        }

        [TestMethod]
        [Description("Проверка загрузки списка таблиц сущности")]
        public void TestFillEntityTables()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser;
            parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();
            parser.FindEntities();


            WXMLModel ormObjectDef = parser.Model;

            EntityDefinition entity = ormObjectDef.GetEntities()
                .Single(match => match.Identifier == "eArtist" && match.Name == "Artist");

            Assert.AreEqual<int>(2, entity.GetSourceFragments().Count());
            Assert.IsTrue(entity.GetSourceFragments().Any(match => match.Identifier.Equals("tblArtists")
                                                                 && match.Name.Equals("artists")));
            Assert.IsTrue(entity.GetSourceFragments().Any(match => match.Identifier.Equals("tblSiteAccess")
                                                                 && match.Name.Equals("sites_access")));
        }

        [TestMethod]
        public void TestFillTypes()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();
            parser.FindEntities();
            parser.FillImports();
            parser.FillTypes();

            WXMLModel ormObjectDef = parser.Model;
            Assert.AreEqual<int>(11, ormObjectDef.GetTypes().Count());
        }

        [TestMethod]
        [Description("Проверка поиска списка сущностей")]
        public void TestFindEntities()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }
            parser.FillSourceFragments();
            parser.FindEntities();

            WXMLModel ormObjectDef = parser.Model;

            Assert.AreEqual<int>(6, ormObjectDef.GetEntities().Count());
            Assert.AreEqual<int>(5, ormObjectDef.GetActiveEntities().Count());
            Assert.IsTrue(ormObjectDef.GetEntities().Any(delegate(EntityDefinition match) { return match.Identifier == "eArtist" && match.Name == "Artist"; }));
            Assert.IsTrue(ormObjectDef.GetEntities().Any(delegate(EntityDefinition match) { return match.Identifier == "eAlbum" && match.Name == "Album"; }));
            Assert.IsTrue(ormObjectDef.GetEntities().Any(delegate(EntityDefinition match) { return match.Identifier == "Album2ArtistRelation" && match.Name == "Album2ArtistRelation"; }));

        }

        [TestMethod]
        [Description("Проверка загрузки списка таблиц из файла")]
        public void TestFillTables()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillSourceFragments();

            WXMLModel ormObjectDef = parser.Model;

            Assert.AreEqual<int>(6, ormObjectDef.GetSourceFragments().Count());
            Assert.IsTrue(ormObjectDef.GetSourceFragments().Any(
                            match => match.Identifier == "tblAlbums" && match.Name == "albums"));
            Assert.IsTrue(ormObjectDef.GetSourceFragments().Any(
                            match => match.Identifier == "tblArtists" && match.Name == "artists"));
            Assert.IsTrue(ormObjectDef.GetSourceFragments().Any(
                            match => match.Identifier == "tblAl2Ar" && match.Name == "al2ar"));
            Assert.IsTrue(ormObjectDef.GetSourceFragments().Any(
                            match => match.Identifier == "tblSiteAccess" && match.Name == "sites_access"));

        }

        [TestMethod]
        [Description("Проверка загрузки описателей схемы")]
        public void TestFillFileDescription()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            parser.FillFileDescriptions();

            WXMLModel ormObjectDef = parser.Model;

            Assert.AreEqual<string>("XMedia.Framework.Media.Objects", ormObjectDef.Namespace);
            Assert.AreEqual<string>("1", ormObjectDef.SchemaVersion);
        }

        [TestMethod]
        [Description("Проверка загрузки xml документа с валидацией")]
        public void TestReadXml()
        {
            Worm_CodeGen_Core_OrmXmlParserAccessor parser = null;
            using (XmlReader rdr = XmlReader.Create(GetSampleFileStream()))
            {
                object privateParser = Worm_CodeGen_Core_OrmXmlParserAccessor.CreatePrivate(rdr, null);
                parser = new Worm_CodeGen_Core_OrmXmlParserAccessor(privateParser);
                parser.Read();
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(GetSampleFileStream());

            Assert.AreEqual<string>(doc.OuterXml, parser.SourceXmlDocument.OuterXml);

        }

        [TestMethod]
        public void TestExtensions()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("extensions"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                Assert.IsNotNull(model.Extensions[new Extension("x")]);

                XmlDocument xdoc = model.Extensions[new Extension("x")];

                Assert.IsNotNull(xdoc);

                Assert.AreEqual("greeting", xdoc.DocumentElement.Name);
                Assert.AreEqual("hi!", xdoc.DocumentElement.InnerText);

                EntityDefinition e11 = model.GetEntities().Single(e => e.Identifier == "e11");

                Assert.IsNotNull(e11.Extensions[new Extension("x")]);

                xdoc = e11.Extensions[new Extension("x")];

                Assert.AreEqual("greeting", xdoc.DocumentElement.Name);
                Assert.AreEqual("hi!", xdoc.DocumentElement.InnerText);

            }
        }

        [TestMethod]
        public void TestSchemaBased()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestFileEquality(stream, "SchemaBasedNewVersion");
            }
        }

        [TestMethod]
        public void TestGroupEquality()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups"))
            {
                TestFileEquality(stream, "groups");
            }
        }

        [TestMethod]
        public void TestExtensionSave()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml("<greeting>hi!</greeting>");

                model.Extensions[new Extension("f")] = xdoc;

                XmlDocument res = model.GetXmlDocument();

                Assert.IsNotNull(res);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(res.NameTable);
                nsmgr.AddNamespace("x", WXMLModel.NS_URI);

                XmlElement extension = res.SelectSingleNode("/x:WXMLModel/x:extensions/x:extension[@name='f']", nsmgr) as XmlElement;
                Assert.IsNotNull(extension);

                Assert.AreEqual("greeting", extension.ChildNodes[0].Name);
                Assert.AreEqual("hi!", extension.ChildNodes[0].InnerText);

            }
        }

        [TestMethod]
        public void TestSchemaBasedNewVersion()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBasedNewVersion"))
            {
                TestFileEquality(stream, "SchemaBasedNewVersion");
            }
        }

        [TestMethod]
        public void TestLinqContext()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("linq-context"))
            {
                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                WXMLDocumentSet wxmlDocumentSet = model.GetWXMLDocumentSet(new WXMLModelWriterSettings());

                Assert.IsNotNull(wxmlDocumentSet);
            }
        }

        private static void TestFileEquality(Stream stream, string actualResourceName)
        {
            WXMLDocumentSet wxmlDocumentSet;
            using (XmlReader rdr = XmlReader.Create(stream))
            {

                WXMLModel model = WXMLModel.LoadFromXml(rdr, new TestXmlUrlResolver());
                wxmlDocumentSet = model.GetWXMLDocumentSet(new WXMLModelWriterSettings());

            }

            XmlDocument xmlDocument = wxmlDocumentSet[0].Document;
            xmlDocument.RemoveChild(xmlDocument.DocumentElement.PreviousSibling);

            Assert.AreEqual<string>(Resources.GetXmlDocument(actualResourceName).OuterXml, xmlDocument.OuterXml);
        }


        public class TestXmlUrlResolver : XmlUrlResolver
        {
            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                if (absoluteUri.Segments[absoluteUri.Segments.Length - 1].EndsWith(".xml") && !File.Exists(absoluteUri.AbsolutePath))
                {
                    return GetFile(Path.GetFileNameWithoutExtension(absoluteUri.AbsolutePath));
                    //return
                    //    File.OpenRead(@"C:\Projects\Framework\Worm\Worm-XMediaDependent\TestsCodeGenLib\" +
                    //                  absoluteUri.Segments[absoluteUri.Segments.Length - 1]);
                }
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }

        public static Stream GetSampleFileStream()
        {
            const string name = "SchemaBased";
            return GetFile(name);
        }

        private static Stream GetFile(string name)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();

            string resourceName = string.Format("{0}.TestFiles.{1}.xml", assembly.GetName().Name, name);
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}
