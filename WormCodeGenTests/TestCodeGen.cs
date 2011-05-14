using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using LinqToCodedom.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using System.CodeDom;
using System.CodeDom.Compiler;
using WXML.CodeDom;
using WXML.CodeDom.CodeDomExtensions;
using WXML.Model;
using WXML.Model.Descriptors;
using WXMLToWorm;
using System.Reflection;
using LinqToCodedom;
using LinqToCodedom.Generator;

namespace WormCodeGenTests
{
    /// <summary>
    /// Summary description for TestCodeGen
    /// </summary>
    [TestClass]
    public class TestCodeGen
    {
        [TestMethod]
        public void TestNullValue()
        {
            {
                int val0 = default(int);
                int val1 = 1;

                object obj0 = (object) val0;
                object obj1 = (object) val1;

                object defaultObj0 = Activator.CreateInstance(obj0.GetType());
                object defaultObj1 = Activator.CreateInstance(obj1.GetType());
                Assert.AreEqual(defaultObj0, obj0);
                Assert.AreNotEqual(defaultObj1, obj1);
            }
            {
                DateTime val0 = default(DateTime);
                DateTime val1 = DateTime.Now;

                object obj0 = (object) val0;
                object obj1 = (object) val1;

                object defaultObj0 = Activator.CreateInstance(obj0.GetType());
                object defaultObj1 = Activator.CreateInstance(obj1.GetType());
                Assert.AreEqual(defaultObj0, obj0);
                Assert.AreNotEqual(defaultObj1, obj1);
            }
        }

        [TestMethod]
        public void TestCSCodeSimple()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestCSCodeSuppressed()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestCSCodeGroups()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestVBCodeGroups()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestCSCodeGroupsHideParent()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups2"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestVBCodeGroupsHideParent()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("groups2"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestCSCodeM2MCheck1()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck1"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestVBCodeM2MCheck1()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck1"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        //[ExpectedException(typeof(OrmCodeGenException))]
        public void TestCSCodeM2MCheck2()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck2"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        //[ExpectedException(typeof(OrmCodeGenException))]
        public void TestVBCodeM2MCheck2()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck2"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestCSCodeM2MCheck3()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck3"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestVBCodeM2MCheck3()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck3"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestCSCodeM2MCheck4()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck4"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WXMLException))]
        public void TestVBCodeM2MCheck4()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("m2mCheck4"))
            {
                TestVBCodeInternal(stream);
            }
        }

        public static void TestCSCodeInternal(Stream stream)
        {
            TestCSCodeInternal(stream, new WXMLCodeDomGeneratorSettings());
        }

        public static void TestCSCodeInternal(Stream stream, WXMLCodeDomGeneratorSettings settings)
        {
            CodeDomProvider prov = new Microsoft.CSharp.CSharpCodeProvider();
            settings.LanguageSpecificHacks = LanguageSpecificHacks.CSharp;
            //settings.RemoveOldM2M = true;
			//settings.Split = false;
            CompileCode(prov, settings, XmlReader.Create(stream));

			//stream.Position = 0;

			//settings.Split = true;
			//CompileCode(prov, settings, XmlReader.Create(stream));
           
        }

        public static Assembly TestCSCodeInternal(WXMLModel model, WXMLCodeDomGeneratorSettings settings,
            params CodeCompileUnit[] units)
        {
            var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v3.5" } };
            CodeDomProvider prov = new Microsoft.CSharp.CSharpCodeProvider(providerOptions);
            settings.LanguageSpecificHacks = LanguageSpecificHacks.CSharp;
            //settings.RemoveOldM2M = true;
            //settings.Split = false;
            return CompileCode(prov, true, settings, model, units);

            //stream.Position = 0;

            //settings.Split = true;
            //CompileCode(prov, settings, XmlReader.Create(stream));

        }

        public class TestXmlUrlResolver : XmlUrlResolver
        {
            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                if (absoluteUri.Segments[absoluteUri.Segments.Length - 1].EndsWith(".xml") && !File.Exists(absoluteUri.AbsolutePath))
                {
                    return Resources.GetXmlDocumentStream(Path.GetFileNameWithoutExtension(absoluteUri.AbsolutePath));
                }
                return base.GetEntity(absoluteUri, role, ofObjectToReturn);
            }
        }

        private static void CompileCode(CodeDomProvider prov, WXMLCodeDomGeneratorSettings settings,
            XmlReader reader)
        {
            CompileCode(prov, false, settings, WXMLModel.LoadFromXml(reader, new TestXmlUrlResolver()));
        }


        private static Assembly CompileCode(CodeDomProvider prov, bool v35, WXMLCodeDomGeneratorSettings settings, 
            WXMLModel model, params CodeCompileUnit[] units)
        {            
            WormCodeDomGenerator gen = new WormCodeDomGenerator(model, settings);
            CompilerResults result;
            CompilerParameters prms = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = false,
                TreatWarningsAsErrors = false/*,
                OutputAssembly = "testAssembly.dll"*/
            };
            prms.ReferencedAssemblies.Add("System.dll");
            prms.ReferencedAssemblies.Add("System.Data.dll");
            prms.ReferencedAssemblies.Add("System.XML.dll");
            if (v35)
                prms.ReferencedAssemblies.Add("System.Core.dll");
            if ((settings.GenerateMode.HasValue ? settings.GenerateMode.Value : model.GenerateMode) != GenerateModeEnum.EntityOnly)
            {
                //prms.ReferencedAssemblies.Add("CoreFramework.dll");
                prms.ReferencedAssemblies.Add("Worm.Orm.dll");
                if (model.LinqSettings != null && model.LinqSettings.Enable)
                    prms.ReferencedAssemblies.Add("Worm.Linq.dll");
            }

            prms.TempFiles.KeepFiles = true;

            CodeCompileUnit singleUnit = new CodeCompileUnit();
            if (settings.SingleFile.HasValue ? settings.SingleFile.Value : model.GenerateSingleFile)
            {
                singleUnit = gen.GetFullSingleUnit(typeof(Microsoft.VisualBasic.VBCodeProvider).IsAssignableFrom(prov.GetType()) ? LinqToCodedom.CodeDomGenerator.Language.VB : LinqToCodedom.CodeDomGenerator.Language.CSharp);
                var l = new List<CodeCompileUnit>();
                l.Add(singleUnit);
                if (units != null)
                {
                    l.AddRange(units);
                    foreach (var item in units)
                    {
                        singleUnit.Namespaces.AddRange(item.Namespaces);
                    }
                }
                result = prov.CompileAssemblyFromDom(prms, l.ToArray());
            }
            else
            {
                Dictionary<string,CodeCompileFileUnit> dic =
                    gen.GetCompileUnits(typeof(Microsoft.VisualBasic.VBCodeProvider).IsAssignableFrom(prov.GetType()) ? LinqToCodedom.CodeDomGenerator.Language.VB : LinqToCodedom.CodeDomGenerator.Language.CSharp);
                
                foreach (CodeCompileFileUnit unit in dic.Values)
                {
                    singleUnit.Namespaces.AddRange(unit.Namespaces);
                }
                var l = new List<CodeCompileUnit>(dic.Values.OfType<CodeCompileUnit>());
                if (units != null)
                {
                    l.AddRange(units);
                    foreach (var item in units)
                    {
                        singleUnit.Namespaces.AddRange(item.Namespaces);
                    }
                }
                result = prov.CompileAssemblyFromDom(prms, l.ToArray());
            }

            prov.GenerateCodeFromCompileUnit(singleUnit, Console.Out, new CodeGeneratorOptions());

            if (result.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();
                foreach (CompilerError str in result.Errors)
                {
                    sb.AppendLine(str.ToString());
                }
                Assert.Fail(sb.ToString());
            }

            return result.CompiledAssembly;
        }

        [TestMethod]
        public void TestVBCodeSimple()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestVBCodeSuppressed()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("suppressed"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestVBCodeCopositePK()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("CompositePK"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestCSCodeCopositePK()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("CompositePK"))
            {
                TestCSCodeInternal(stream);
            }
        }

		[TestMethod]
		public void TestVBCodeDefferedLoading()
		{
			using (Stream stream = Resources.GetXmlDocumentStream("deffered"))
			{
				TestVBCodeInternal(stream);
			}
		}

		[TestMethod]
		public void TestCSCodeDefferedLoading()
		{
			using (Stream stream = Resources.GetXmlDocumentStream("deffered"))
			{
				TestCSCodeInternal(stream);
			}
		}

        [TestMethod]
        public void TestCSSchemaBased()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod, Ignore]
        public void TestCSLinqContext()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("linqctx"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod, Ignore]
        public void TestVBLinqContext()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("linqctx"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestTypesInProperties_CS()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestCSCodeInternal(stream, new WXMLCodeDomGeneratorSettings { UseTypeInProps = true });
            }
        }

        [TestMethod]
        public void TestTypesInProperties_VB()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestVBCodeInternal(stream, new WXMLCodeDomGeneratorSettings { UseTypeInProps = true});
            }
        }

        [TestMethod]
        public void TestEntityWithoutPK_CS()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("keyless"))
            {
                TestCSCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestEntityWithoutPK_VB()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("keyless"))
            {
                TestVBCodeInternal(stream);
            }
        }

        [TestMethod]
        public void TestCheckCacheRequired()
        {
            WXMLModel model;
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                model = WXMLModel.LoadFromXml(XmlReader.Create(stream), new TestXmlUrlResolver());
                Assert.IsNotNull(model);
            }

            model.GetActiveEntities().Single(item => item.Identifier == "eAlbum").CacheCheckRequired = true;

            TestCSCodeInternal(model, new WXMLCodeDomGeneratorSettings());
        }

        [TestMethod]
        public void TestGenerateSchemaOnly()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestCSCodeInternal(stream, new WXMLCodeDomGeneratorSettings {GenerateMode = GenerateModeEnum.SchemaOnly });
            }
        }

        [TestMethod]
        public void TestGenerateEntityOnly()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                TestCSCodeInternal(stream, new WXMLCodeDomGeneratorSettings { GenerateMode = GenerateModeEnum.EntityOnly });
            }
        }

        [TestMethod]
        public void TestBaseType()
        {
            WXMLModel model;
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                model = WXMLModel.LoadFromXml(XmlReader.Create(stream), new TestXmlUrlResolver());
                Assert.IsNotNull(model);
            }

            TypeDefinition td = new TypeDefinition("tBase", "test.BaseT", true);
            model.EntityBaseType = td;
            model.UserComments.Add("xxx");
            CodeDomGenerator c = new CodeDomGenerator();
            c.AddNamespace("test")
                .AddClass("BaseT", TypeAttributes.Abstract | TypeAttributes.Public)
                .Inherits(typeof (Worm.Entities.KeyEntity));
            TestCSCodeInternal(model, new WXMLCodeDomGeneratorSettings(), c.GetCompileUnit(CodeDomGenerator.Language.CSharp));
        }

        [TestMethod]
        public void TestWriteSchemaBased()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("SchemaBased"))
            {
                WXMLModel model = WXMLModel.LoadFromXml(XmlReader.Create(stream), new TestXmlUrlResolver());
                Assert.IsNotNull(model);

                var wxmlDocumentSet = model.GetWXMLDocumentSet(new WXMLModelWriterSettings());

                Assert.IsNotNull(wxmlDocumentSet);
            }
        }

        [TestMethod]
        public void TestPureClasses()
        {
            using (Stream stream = Resources.GetXmlDocumentStream("pure-classes"))
            {
                WXMLModel model = WXMLModel.LoadFromXml(XmlReader.Create(stream), new TestXmlUrlResolver());
                Assert.IsNotNull(model);

                var wxmlDocumentSet = model.GetWXMLDocumentSet(new WXMLModelWriterSettings());

                Assert.IsNotNull(wxmlDocumentSet);

                CodeDomGenerator c = new CodeDomGenerator();
                c.AddNamespace("test")
                    .AddInterface(Define.Interface("MyInterface")
                        .AddProperty(typeof(int), MemberAttributes.Public, "ID")
                        .AddProperty(typeof(string), MemberAttributes.Public, "Name")
                        //.AddGetProperty(CodeDom.TypeRef(typeof(IQueryable<>), "MyInterface2"), MemberAttributes.Public, "e4s")
                    )
                    .AddInterface(Define.Interface("MyInterface2")
                        .AddProperty(typeof(int), MemberAttributes.Public, "ID")
                        .AddProperty(CodeDom.TypeRef("MyInterface"), MemberAttributes.Public, "prop1")
                    )
                ;

                Assembly a = TestCSCodeInternal(model, new WXMLCodeDomGeneratorSettings(), 
                    c.GetCompileUnit(CodeDomGenerator.Language.CSharp));

                Assert.IsNotNull(a);

                Type t = a.GetType("e3");

                Assert.IsNotNull(t);

                Assert.IsTrue(t.GetInterfaces().Any(i => i.Name == "MyInterface"));

                a = TestVBCodeInternal(model, new WXMLCodeDomGeneratorSettings(),
                    c.GetCompileUnit(CodeDomGenerator.Language.VB));

                Assert.IsNotNull(a);

                t = a.GetType("e3");

                Assert.IsNotNull(t);

                Assert.IsTrue(t.GetInterfaces().Any(i => i.Name == "MyInterface"));
            }
        }

        public void TestVBCodeInternal(Stream stream)
        {
            TestVBCodeInternal(stream, new WXMLCodeDomGeneratorSettings());
        }

        public void TestVBCodeInternal(Stream stream, WXMLCodeDomGeneratorSettings settings)
        {
            CodeDomProvider prov = new Microsoft.VisualBasic.VBCodeProvider();

            settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;
            //settings.RemoveOldM2M = true;

			//settings.Split = false;
            CompileCode(prov, settings, XmlReader.Create(stream));

			//stream.Position = 0;

			//settings.Split = true;
			//CompileCode(prov, settings, XmlReader.Create(stream));
        }

        public Assembly TestVBCodeInternal(WXMLModel model, WXMLCodeDomGeneratorSettings settings,
            params CodeCompileUnit[] units)
        {
            var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v3.5" } };
            CodeDomProvider prov = new Microsoft.VisualBasic.VBCodeProvider(providerOptions);

            settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;
            //settings.RemoveOldM2M = true;

            //settings.Split = false;
            return CompileCode(prov, true, settings, model, units);
            //stream.Position = 0;

            //settings.Split = true;
            //CompileCode(prov, settings, XmlReader.Create(stream));
        }
    }
}
