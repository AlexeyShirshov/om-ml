using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestsCodeGenLib;
using WXML.Model;
using WXML.Model.Descriptors;

namespace WXMLTests
{
    /// <summary>
    /// Summary description for TestMerge
    /// </summary>
    [TestClass]
    public class TestMerge
    {
        public TestMerge()
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
        public void TestAddType()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                Assert.AreEqual(2, model.GetTypes().Count());

                WXMLModel newModel = new WXMLModel();

                TypeDefinition newType = new TypeDefinition("tInt16", typeof(short));

                newModel.AddType(newType);

                model.Merge(Normalize(newModel));

                Assert.AreEqual(3, model.GetTypes().Count());

            }
        }

        private WXMLModel Normalize(WXMLModel model)
        {
            XmlDocument xdoc = model.GetXmlDocument();
            Console.WriteLine(xdoc.OuterXml);
            return WXMLModel.LoadFromXml(new XmlNodeReader(xdoc));
        }

        [TestMethod]
        public void TestAddProperty()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                EntityDefinition entity = model.GetActiveEntities().Single(item => item.Identifier == "e1");

                Assert.IsNotNull(entity);

                Assert.AreEqual(2, model.GetActiveEntities().Count());

                Assert.AreEqual(2, entity.GetActiveProperties().Count());

                WXMLModel newModel = new WXMLModel();

                EntityDefinition newEntity = new EntityDefinition(entity.Identifier, entity.Name, entity.Namespace, entity.Description, newModel);

                //newModel.AddEntity(newEntity);

                TypeDefinition tString = model.GetTypes().Single(item => item.Identifier == "tString");

                newModel.AddType(tString);

                SourceFragmentRefDefinition newTable = entity.GetSourceFragments().First();

                newModel.AddSourceFragment(newTable);

                newEntity.AddSourceFragment(newTable);

                newEntity.AddProperty(new ScalarPropertyDefinition(newEntity, "Prop2", "Prop2", Field2DbRelations.None, null,
                    tString, new SourceFieldDefinition(newTable, "prop2"), AccessLevel.Private, AccessLevel.Public));

                model.Merge(Normalize(newModel));

                Assert.AreEqual(2, model.GetActiveEntities().Count());

                Assert.AreEqual(3, entity.GetActiveProperties().Count());
            }
        }

        [TestMethod]
        public void TestAlterProperty()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                EntityDefinition entity = model.GetActiveEntities().Single(item => item.Identifier == "e1");

                Assert.IsNotNull(entity);

                Assert.AreEqual(2, entity.GetActiveProperties().Count());

                Assert.AreEqual("Prop1", entity.GetActiveProperties().Single(item => item.PropertyAlias == "Prop1").Name);

                WXMLModel newModel = new WXMLModel();

                ScalarPropertyDefinition oldProp = ((ScalarPropertyDefinition) entity.GetActiveProperties()
                    .Single(item => item.PropertyAlias == "Prop1")).Clone();

                TypeDefinition newType = new TypeDefinition("tInt16", typeof(short));

                newModel.AddType(newType);

                EntityDefinition newEntity = new EntityDefinition(entity.Identifier, entity.Name, entity.Namespace, entity.Description, newModel);

                //newModel.AddEntity(newEntity);

                ScalarPropertyDefinition newProp = new ScalarPropertyDefinition(newEntity, "Prop2")
                {
                    PropertyAlias = "Prop1",
                    PropertyType = newType
                };

                newEntity.AddProperty(newProp);

                model.Merge(Normalize(newModel));

                Assert.AreEqual(2, entity.GetActiveProperties().Count());

                ScalarPropertyDefinition renewProp = (ScalarPropertyDefinition) entity.GetActiveProperties()
                    .Single(item => item.PropertyAlias == "Prop1");

                Assert.AreEqual("Prop2", renewProp.Name);

                Assert.AreEqual(oldProp.DefferedLoadGroup, renewProp.DefferedLoadGroup);
                Assert.AreEqual(oldProp.Description, renewProp.Description);
                Assert.AreEqual(oldProp.Disabled, renewProp.Disabled);
                Assert.AreEqual(oldProp.EnablePropertyChanged, renewProp.EnablePropertyChanged);
                Assert.AreEqual(oldProp.FieldAccessLevel, renewProp.FieldAccessLevel);
                Assert.AreEqual(oldProp.FromBase, renewProp.FromBase);
                Assert.AreEqual(oldProp.Group, renewProp.Group);
                Assert.AreEqual(oldProp.IsSuppressed, renewProp.IsSuppressed);
                Assert.AreEqual(oldProp.Obsolete, renewProp.Obsolete);
                Assert.AreEqual(oldProp.ObsoleteDescripton, renewProp.ObsoleteDescripton);
                Assert.AreEqual(oldProp.PropertyAccessLevel, renewProp.PropertyAccessLevel);
                Assert.AreEqual(oldProp.PropertyAlias, renewProp.PropertyAlias);
                Assert.AreEqual(oldProp.SourceFragment, renewProp.SourceFragment);

                Assert.AreEqual(oldProp.SourceType, renewProp.SourceType);
                Assert.AreEqual(oldProp.IsNullable, renewProp.IsNullable);
                Assert.AreEqual(oldProp.SourceTypeSize, renewProp.SourceTypeSize);
                Assert.AreEqual(oldProp.SourceFieldAlias, renewProp.SourceFieldAlias);
                Assert.AreEqual(oldProp.SourceFieldExpression, renewProp.SourceFieldExpression);

            }
        }

        [TestMethod]
        public void TestAddEntity()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                Assert.AreEqual(2, model.GetActiveEntities().Count());

                WXMLModel newModel = new WXMLModel();

                EntityDefinition newEntity = new EntityDefinition("ee", "ee", string.Empty, string.Empty, newModel);

                SourceFragmentRefDefinition sf = new SourceFragmentRefDefinition(newModel.GetOrCreateSourceFragment("dbo", "ee"));

                newEntity.AddSourceFragment(sf);

                newEntity.AddProperty(new ScalarPropertyDefinition(newEntity, "ID", "ID", Field2DbRelations.None,
                    string.Empty, newModel.GetOrCreateClrType(typeof(Int32)), new SourceFieldDefinition(sf, "id"), AccessLevel.Private,
                    AccessLevel.Public));

                model.Merge(Normalize(newModel));

                Assert.AreEqual(3, model.GetActiveEntities().Count());

                Assert.AreEqual(1, model.GetActiveEntities().Single(item => item.Identifier == "ee").GetActiveProperties().Count());
            }
        }

        [TestMethod]
        public void TestAlterEntity()
        {
            WXMLModel newModel = GetModel("suppressed");

            Assert.IsNotNull(newModel);

            Assert.AreEqual(2, newModel.GetActiveEntities().Count());

            EntityDefinition e = newModel.GetActiveEntities().Single(item => item.Identifier == "e1");
            EntityDefinition e2 = newModel.GetActiveEntities().Single(item => item.Identifier == "e11");

            Assert.AreEqual(e2.BaseEntity, e);
            Assert.AreEqual("E1", e.Name);

            e.Name = "xxx";

            Assert.AreEqual(e2.BaseEntity, e);

            WXMLModel model = GetModel("suppressed");

            model.Merge(Normalize(newModel));

            Assert.AreEqual(2, model.GetActiveEntities().Count());

            e = model.GetActiveEntities().Single(item => item.Identifier == "e1");
            Assert.AreEqual("xxx", e.Name);
        }

        [TestMethod]
        public void TestMergeExtension()
        {
            WXMLModel newModel = GetModel("extensions");

            Assert.AreEqual(1, newModel.Extensions.Count);

            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml("<root/>");

            newModel.Extensions.Add(new Extension("dfdf"), xdoc);
            WXMLModel model = GetModel("extensions");

            model.Merge(Normalize(newModel));

            Assert.AreEqual(2, model.Extensions.Count);
        }

        //[TestMethod, ExpectedException(typeof(ArgumentException))]
        //public void TestAlterEntity_ChangeTableWrong()
        //{
        //    using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
        //    {
        //        Assert.IsNotNull(stream);

        //        WXMLModel newModel = WXMLModel.LoadFromXml(new XmlTextReader(stream));

        //        Assert.IsNotNull(newModel);

        //        Assert.AreEqual(2, newModel.GetActiveEntities().Count());

        //        EntityDefinition e = newModel.GetActiveEntities().Single(item => item.Identifier == "e11");

        //        Assert.AreEqual(1, e.GetSourceFragments().Count());
        //        Assert.AreEqual(1, newModel.GetSourceFragments().Count());

        //        Assert.AreEqual("tbl1", e.GetSourceFragments().First().Name);
        //        Assert.IsTrue(string.IsNullOrEmpty(e.GetSourceFragments().First().Selector));

        //        foreach (SourceFragmentRefDefinition rsf in e.GetSourceFragments())
        //        {
        //            e.MarkAsDeleted(rsf);
        //        }
        //    }
        //}

        [TestMethod]
        public void TestAlterEntity_ChangeTable()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                Assert.IsNotNull(stream);

                WXMLModel newModel = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(newModel);

                Assert.AreEqual(2, newModel.GetActiveEntities().Count());

                EntityDefinition e = newModel.GetActiveEntities().Single(item => item.Identifier == "e1");

                Assert.AreEqual(1, e.GetSourceFragments().Count());
                Assert.AreEqual(1, newModel.GetSourceFragments().Count());

                Assert.AreEqual("tbl1", e.GetSourceFragments().First().Name);
                Assert.IsTrue(string.IsNullOrEmpty(e.GetSourceFragments().First().Selector));

                SourceFragmentRefDefinition sf = new SourceFragmentRefDefinition(
                    newModel.GetOrCreateSourceFragment("dbo", "table"));

                //foreach (SourceFragmentRefDefinition rsf in e.GetSourceFragments())
                //{
                //    e.MarkAsDeleted(rsf);
                //}
                e.ClearSourceFragments();
                e.AddSourceFragment(sf);

                foreach (PropertyDefinition property in e.GetProperties())
                {
                    property.SourceFragment = sf;
                }

                WXMLModel model = GetModel("suppressed");

                model.Merge(Normalize(newModel));

                e = model.GetActiveEntities().Single(item => item.Identifier == "e1");

                Assert.AreEqual(1, e.GetSourceFragments().Count());
                Assert.AreEqual(2, model.GetSourceFragments().Count());

                Assert.AreEqual("table", e.GetSourceFragments().First().Name);
                Assert.AreEqual("dbo", e.GetSourceFragments().First().Selector);

                e = model.GetActiveEntities().Single(item => item.Identifier == "e11");
                
                Assert.AreEqual(1, e.GetSourceFragments().Count());
                Assert.AreEqual("table", e.GetSourceFragments().First().Name);
                Assert.AreEqual("dbo", e.GetSourceFragments().First().Selector);
            }
        }

        private static WXMLModel GetModel(string fileName)
        {
            using (Stream stream = Resources.GetXmlDocumentStream(fileName))
            {
                Assert.IsNotNull(stream);

                WXMLModel model = WXMLModel.LoadFromXml(new XmlTextReader(stream));

                Assert.IsNotNull(model);

                return model;
            }
        }
    }
}
