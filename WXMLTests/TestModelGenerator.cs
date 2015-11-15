using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WXML.Model;
using WXML.Model.Descriptors;
using WXML.Model.Database.Providers;
using WXML.SourceConnector;
using System.Xml.Serialization;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

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
        public void TestSerializeSourceView()
        {
            MSSQLProvider p = new MSSQLProvider(GetTestDB(), null);
            SourceView view = p.GetSourceView();

            //XmlSerializer s = new XmlSerializer(typeof(SourceView));

            //s.Serialize(Console.Out, view);

            BinaryFormatter f = new BinaryFormatter();

            using (MemoryStream ms = new MemoryStream())
            {
                f.Serialize(ms, view);

                ms.Position = 0;

                view = (SourceView)f.Deserialize(ms);
            }

            Assert.AreEqual(133, view.SourceFields.Count());

            Assert.AreEqual(32, view.GetSourceFragments().Count());

            foreach (SourceFragmentDefinition fragment in view.GetSourceFragments())
            {
                Assert.IsTrue(view.GetSourceFields(fragment).Count() > 0);
            }
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

            c.ApplySourceViewToModel(false, relation1to1.Unify, true, true, false);

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

            SourceFieldDefinition fld = sv.SourceFields.Find(item => item.SourceFieldExpression == "[Description]");

            sv.SourceFields.Remove(fld);

            Assert.AreEqual(3, sv.SourceFields.Count);

            c.ApplySourceViewToModel(true, relation1to1.Default, true, true, false);

            Assert.AreEqual(3, model.GetActiveEntities().First().GetActiveProperties().Count());

            sv.SourceFields.Add(fld);

            Assert.AreEqual(4, sv.SourceFields.Count);

            c.ApplySourceViewToModel(true, relation1to1.Default, true, true, false);

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

            c.ApplySourceViewToModel(true, relation1to1.Default, true, true, false);

            Assert.AreEqual(3, model.GetEntity("e_dbo_aspnet_Paths").GetActiveProperties().Count());
        }

        [TestMethod]
        public void TestM2MSimilarRelations()
        {
            SourceView sv = CreateComplexSourceView();

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();
        }

        public static SourceView CreateComplexSourceView()
        {
            SourceView sv = new SourceView();
            SourceFragmentDefinition sf1 = new SourceFragmentDefinition("tbl1", "tbl1", "dbo");
            SourceFragmentDefinition sf2 = new SourceFragmentDefinition("tbl2", "tbl2", "dbo");
            SourceFragmentDefinition sf3 = new SourceFragmentDefinition("tbl3", "tbl3", "dbo");
            SourceFragmentDefinition sf4 = new SourceFragmentDefinition("tbl4", "tbl4", "dbo");
            SourceFragmentDefinition sf5 = new SourceFragmentDefinition("tbl5", "tbl5", "dbo");
            SourceFragmentDefinition sf6 = new SourceFragmentDefinition("tbl6", "tbl6", "dbo");

            SourceFieldDefinition pkField = new SourceFieldDefinition(sf1, "id", "int") { IsNullable = false };
            sv.SourceFields.Add(pkField);

            SourceFieldDefinition pkField2 = new SourceFieldDefinition(sf2, "id", "int") { IsNullable = false };
            sv.SourceFields.Add(pkField2);

            sv.SourceFields.Add(new SourceFieldDefinition(sf3, "prop1_id", "int"));
            sv.SourceFields.Add(new SourceFieldDefinition(sf3, "prop2_id", "int"));

            sv.SourceFields.Add(new SourceFieldDefinition(sf4, "prop1_id", "int"));
            sv.SourceFields.Add(new SourceFieldDefinition(sf4, "prop2_id", "int"));

            sv.SourceFields.Add(new SourceFieldDefinition(sf5, "prop1_id", "int"));
            sv.SourceFields.Add(new SourceFieldDefinition(sf5, "prop2_id", "int"));

            sv.SourceFields.Add(new SourceFieldDefinition(sf6, "prop1_id", "int"));
            sv.SourceFields.Add(new SourceFieldDefinition(sf6, "prop2_id", "int"));

            SourceConstraint pk = new SourceConstraint(SourceConstraint.PrimaryKeyConstraintTypeName, "pk1");
            pk.SourceFields.Add(pkField);
            sf1.Constraints.Add(pk);

            SourceConstraint pk2 = new SourceConstraint(SourceConstraint.PrimaryKeyConstraintTypeName, "pk2");
            pk2.SourceFields.Add(pkField2);
            sf2.Constraints.Add(pk2);

            {
                SourceConstraint fk1 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl3_fk1");
                fk1.SourceFields.Add(sv.GetSourceFields(sf3).Single(item => item.SourceFieldExpression == "prop1_id"));

                SourceConstraint fk2 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl3_fk2");
                fk2.SourceFields.Add(sv.GetSourceFields(sf3).Single(item => item.SourceFieldExpression == "prop2_id"));

                sf3.Constraints.Add(fk1);
                sf3.Constraints.Add(fk2);

                sv.References.Add(new SourceReferences(pk, fk1, pkField,
                    sv.GetSourceFields(sf3).Single(item => item.SourceFieldExpression == "prop1_id")
                ));

                sv.References.Add(new SourceReferences(pk2, fk2, pkField2,
                    sv.GetSourceFields(sf3).Single(item => item.SourceFieldExpression == "prop2_id")
                ));
            }
            {
                SourceConstraint fk1 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl4_fk1");
                fk1.SourceFields.Add(sv.GetSourceFields(sf4).Single(item => item.SourceFieldExpression == "prop1_id"));

                SourceConstraint fk2 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl4_fk2");
                fk2.SourceFields.Add(sv.GetSourceFields(sf4).Single(item => item.SourceFieldExpression == "prop2_id"));

                sf4.Constraints.Add(fk1);
                sf4.Constraints.Add(fk2);

                sv.References.Add(new SourceReferences(pk, fk1, pkField,
                    sv.GetSourceFields(sf4).Single(item => item.SourceFieldExpression == "prop1_id")
                ));

                sv.References.Add(new SourceReferences(pk2, fk2, pkField2,
                    sv.GetSourceFields(sf4).Single(item => item.SourceFieldExpression == "prop2_id")
                ));
            }
            {
                SourceConstraint fk1 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl5_fk1");
                fk1.SourceFields.Add(sv.GetSourceFields(sf5).Single(item => item.SourceFieldExpression == "prop1_id"));

                SourceConstraint fk2 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl5_fk2");
                fk2.SourceFields.Add(sv.GetSourceFields(sf5).Single(item => item.SourceFieldExpression == "prop2_id"));

                sf5.Constraints.Add(fk1);
                sf5.Constraints.Add(fk2);

                sv.References.Add(new SourceReferences(pk, fk1, pkField,
                    sv.GetSourceFields(sf5).Single(item => item.SourceFieldExpression == "prop1_id")
                ));

                sv.References.Add(new SourceReferences(pk, fk2, pkField,
                    sv.GetSourceFields(sf5).Single(item => item.SourceFieldExpression == "prop2_id")
                ));
            }
            {
                SourceConstraint fk1 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl6_fk1");
                fk1.SourceFields.Add(sv.GetSourceFields(sf6).Single(item => item.SourceFieldExpression == "prop1_id"));

                SourceConstraint fk2 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "tbl6_fk2");
                fk2.SourceFields.Add(sv.GetSourceFields(sf6).Single(item => item.SourceFieldExpression == "prop2_id"));

                sf6.Constraints.Add(fk1);
                sf6.Constraints.Add(fk2);

                sv.References.Add(new SourceReferences(pk, fk1, pkField,
                    sv.GetSourceFields(sf6).Single(item => item.SourceFieldExpression == "prop1_id")
                ));

                sv.References.Add(new SourceReferences(pk, fk2, pkField,
                    sv.GetSourceFields(sf6).Single(item => item.SourceFieldExpression == "prop2_id")
                ));
            }

            return sv;
        }

        [TestMethod]
        public void TestO2MSimilarRelations()
        {
            SourceView sv = new SourceView();
            SourceFragmentDefinition sf1 = new SourceFragmentDefinition("tbl1", "tbl1", "dbo");
            SourceFragmentDefinition sf2 = new SourceFragmentDefinition("tbl2", "tbl2", "dbo");

            SourceFieldDefinition pkField = new SourceFieldDefinition(sf1, "id", "int") { IsNullable = false };

            sv.SourceFields.Add(pkField);
            sv.SourceFields.Add(new SourceFieldDefinition(sf2, "id", "int") { IsNullable = false });
            sv.SourceFields.Add(new SourceFieldDefinition(sf2, "prop1_id", "int"));
            sv.SourceFields.Add(new SourceFieldDefinition(sf2, "prop2_id", "int"));

            SourceConstraint pk = new SourceConstraint(SourceConstraint.PrimaryKeyConstraintTypeName, "pk1");
            pk.SourceFields.Add(pkField);

            SourceConstraint pk2 = new SourceConstraint(SourceConstraint.PrimaryKeyConstraintTypeName, "pk2");
            pk2.SourceFields.Add(sv.GetSourceFields(sf2).Single(item => item.SourceFieldExpression == "id"));

            SourceConstraint fk1 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "fk1");
            fk1.SourceFields.Add(sv.GetSourceFields(sf2).Single(item => item.SourceFieldExpression == "prop1_id"));

            SourceConstraint fk2 = new SourceConstraint(SourceConstraint.ForeignKeyConstraintTypeName, "fk2");
            fk2.SourceFields.Add(sv.GetSourceFields(sf2).Single(item => item.SourceFieldExpression == "prop2_id"));

            sf1.Constraints.Add(pk);
            sf2.Constraints.Add(pk2);
            sf2.Constraints.Add(fk1);
            sf2.Constraints.Add(fk2);

            sv.References.Add(new SourceReferences(pk, fk1, pkField,
                sv.GetSourceFields(sf2).Single(item => item.SourceFieldExpression == "prop1_id")
            ));

            sv.References.Add(new SourceReferences(pk, fk2, pkField,
                sv.GetSourceFields(sf2).Single(item => item.SourceFieldExpression == "prop2_id")
            ));

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(sv, model);

            c.ApplySourceViewToModel();
        }

        //[TestMethod]
        //public void TestAW()
        //{
        //    MSSQLProvider p = new MSSQLProvider(".\\sqlexpress", "AdventureWorks");
        //    SourceView view = p.GetSourceView();

        //    BinaryFormatter f = new BinaryFormatter();

        //    using (FileStream fs = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "AdventureWorks.sourceview"), FileMode.CreateNew))
        //    {
        //        f.Serialize(fs, view);
        //    }
        //}

        [TestMethod]
        public void TestAdventureWorks()
        {
            SourceView view;

            BinaryFormatter f = new BinaryFormatter { AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };

            ResolveEventHandler d = null;
            d = (sender, args) =>
            {
                AppDomain.CurrentDomain.AssemblyResolve -= d;
                return typeof(WXMLModel).Assembly;
            };
            AppDomain.CurrentDomain.AssemblyResolve += d;

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream fs = assembly.GetManifestResourceStream(
                string.Format("{0}.TestFiles.{1}", assembly.GetName().Name, "AdventureWorks.sourceview")))
            {
                Assert.IsNotNull(fs);
                view = (SourceView)f.Deserialize(fs);
            }

            Assert.IsNotNull(view);

            WXMLModel model = new WXMLModel();

            SourceToModelConnector c = new SourceToModelConnector(view, model);

            c.ApplySourceViewToModel(false, relation1to1.Default, true, true, false);

            Assert.AreEqual(70, model.GetActiveEntities().Count());

            Assert.AreEqual(70, model.GetSourceFragments().Count());

            model = new WXMLModel();
            c = new SourceToModelConnector(view, model);
            c.ApplySourceViewToModel(false, relation1to1.Unify, true, true, false);

            Assert.AreEqual(67, model.GetActiveEntities().Count());

            Assert.AreEqual(70, model.GetSourceFragments().Count());

            model = new WXMLModel();
            c = new SourceToModelConnector(view, model);
            c.ApplySourceViewToModel(false, relation1to1.Hierarchy, true, true, false);

            Assert.AreEqual(70, model.GetActiveEntities().Count());

            Assert.AreEqual(70, model.GetSourceFragments().Count());
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
