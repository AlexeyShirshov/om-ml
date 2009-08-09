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

                Assert.AreEqual(2, model.Types.Count);

                WXMLModel newModel = new WXMLModel();

                TypeDescription newType = new TypeDescription("tInt16",typeof(short));

                newModel.Types.Add(newType);

                model.Merge(Normalize(newModel));

                Assert.AreEqual(3, model.Types.Count);

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

                EntityDescription entity = model.ActiveEntities.Single(item => item.Identifier == "e1");

                Assert.IsNotNull(entity);

                Assert.AreEqual(2, model.ActiveEntities.Count());

                Assert.AreEqual(2, entity.ActiveProperties.Count());

                WXMLModel newModel = new WXMLModel();

                EntityDescription newEntity = new EntityDescription(entity.Identifier, entity.Name, entity.Namespace, entity.Description, newModel);

                newModel.AddEntity(newEntity);

                TypeDescription tString = model.Types.Single(item => item.Identifier == "tString");

                newModel.Types.Add(tString);

                SourceFragmentRefDescription newTable = entity.GetSourceFragments().First();

                newModel.SourceFragments.Add(newTable);

                newEntity.AddSourceFragment(newTable);

                newEntity.AddProperty(new PropertyDescription(newEntity, "Prop2", "Prop2", null, null, 
                    tString, "prop2", newTable, AccessLevel.Private, AccessLevel.Public));

                model.Merge(Normalize(newModel));

                Assert.AreEqual(2, model.ActiveEntities.Count());

                Assert.AreEqual(3, entity.ActiveProperties.Count());
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

                EntityDescription entity = model.ActiveEntities.Single(item => item.Identifier == "e1");

                Assert.IsNotNull(entity);

                Assert.AreEqual(2, entity.ActiveProperties.Count);

                Assert.AreEqual("Prop1", entity.ActiveProperties.Single(item => item.PropertyAlias == "Prop1").Name);

                WXMLModel newModel = new WXMLModel();

                PropertyDescription oldProp = entity.ActiveProperties.Single(item => item.PropertyAlias == "Prop1").Clone();

                TypeDescription newType = new TypeDescription("tInt16", typeof(short));

                newModel.Types.Add(newType);

                EntityDescription newEntity = new EntityDescription(entity.Identifier, entity.Name, entity.Namespace, entity.Description, newModel);

                newModel.AddEntity(newEntity);

                PropertyDescription newProp = new PropertyDescription(newEntity,"Prop2")
                {
                    PropertyAlias = "Prop1",
                    PropertyType = newType
                };

                newEntity.AddProperty(newProp);

                model.Merge(Normalize(newModel));

                Assert.AreEqual(2, entity.ActiveProperties.Count);

                PropertyDescription renewProp = entity.ActiveProperties.Single(item=>item.PropertyAlias=="Prop1");

                Assert.AreEqual("Prop2", renewProp.Name);

                Assert.AreEqual(oldProp.DbTypeName, renewProp.DbTypeName);
                Assert.AreEqual(oldProp.DbTypeNullable, renewProp.DbTypeNullable);
                Assert.AreEqual(oldProp.DbTypeSize, renewProp.DbTypeSize);
                Assert.AreEqual(oldProp.DefferedLoadGroup, renewProp.DefferedLoadGroup);
                Assert.AreEqual(oldProp.Description, renewProp.Description);
                Assert.AreEqual(oldProp.Disabled, renewProp.Disabled);
                Assert.AreEqual(oldProp.EnablePropertyChanged, renewProp.EnablePropertyChanged);
                Assert.AreEqual(oldProp.FieldAccessLevel, renewProp.FieldAccessLevel);
                Assert.AreEqual(oldProp.FieldAlias, renewProp.FieldAlias);
                Assert.AreEqual(oldProp.FieldName, renewProp.FieldName);
                Assert.AreEqual(oldProp.FromBase, renewProp.FromBase);
                Assert.AreEqual(oldProp.Group, renewProp.Group);
                Assert.AreEqual(oldProp.IsSuppressed, renewProp.IsSuppressed);
                Assert.AreEqual(oldProp.Obsolete, renewProp.Obsolete);
                Assert.AreEqual(oldProp.ObsoleteDescripton, renewProp.ObsoleteDescripton);
                Assert.AreEqual(oldProp.PropertyAccessLevel, renewProp.PropertyAccessLevel);
                Assert.AreEqual(oldProp.PropertyAlias, renewProp.PropertyAlias);
                Assert.AreEqual(oldProp.SourceFragment, renewProp.SourceFragment);
            }
        }
    }
}
