using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WXML.Model;
using WXMLToWorm;
using WXML.CodeDom;

namespace TestsCodeGenLib
{
	/// <summary>
	/// Summary description for TestEntityBasedClass
	/// </summary>
	[TestClass]
	public class TestEntityBasedClass
	{

        //[TestMethod]
        //public void TestEntityBasedClassWithClass()
        //{
        //    Model odef;
        //    using (Stream stream = GetSampleFileStream("EntityBasedClassFiles.EntityBasedClassSample.class.xml"))
        //    {
        //        using (XmlReader reader = XmlReader.Create(stream))
        //        {
        //            odef = Model.LoadFromXml(reader);
        //        }
        //    }
        //    OrmCodeDomGeneratorSettings settings = new OrmCodeDomGeneratorSettings();
        //    settings.LanguageSpecificHacks = LanguageSpecificHacks.CSharp;
        //    CodeDomProvider prov = new Microsoft.CSharp.CSharpCodeProvider();

        //    CompileCode(odef, prov, settings);

        //    prov = new Microsoft.VisualBasic.VBCodeProvider();
        //    settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;

        //    CompileCode(odef, prov, settings);
        //}

		private void CompileCode(WXMLModel odef, CodeDomProvider prov, WXMLCodeDomGeneratorSettings settings)
		{
			WormCodeDomGenerator gen = new WormCodeDomGenerator(odef, settings);
            Dictionary<string, WXML.CodeDom.CodeDomExtensions.CodeCompileFileUnit> dic =
                gen.GetFullDom(typeof(Microsoft.VisualBasic.VBCodeProvider).IsAssignableFrom(prov.GetType()) ? LinqToCodedom.CodeDomGenerator.Language.VB : LinqToCodedom.CodeDomGenerator.Language.CSharp);

			CompilerParameters prms = new CompilerParameters();
			prms.GenerateExecutable = false;
			prms.GenerateInMemory = true;
			prms.IncludeDebugInformation = false;
			prms.TreatWarningsAsErrors = false;
			prms.OutputAssembly = "testAssembly.dll";
			prms.ReferencedAssemblies.Add("System.dll");
			prms.ReferencedAssemblies.Add("System.Data.dll");
			prms.ReferencedAssemblies.Add("System.XML.dll");
			prms.ReferencedAssemblies.Add("CoreFramework.dll");
			prms.ReferencedAssemblies.Add("Worm.Orm.dll");
			prms.TempFiles.KeepFiles = true;

			CodeCompileUnit[] units = new CodeCompileUnit[dic.Values.Count + 1];
			int idx = 0;

			CodeCompileUnit baseClassUnit = new CodeCompileUnit();
			CodeNamespace baseClassNS = new CodeNamespace("OrmCodeGenTests");
			baseClassUnit.Namespaces.Add(baseClassNS);

			CodeTypeDeclaration baseClass = new CodeTypeDeclaration("TestOrmBase");
			CodeTypeParameter baseClassTypePrm = new CodeTypeParameter("T");

				
			CodeTypeReference baseClassBase = new CodeTypeReference("Worm.Entities.OrmBaseT");
			baseClassBase.TypeArguments.Add("T");
			baseClassTypePrm.HasConstructorConstraint = true;
			baseClass.TypeParameters.Add(baseClassTypePrm);
			baseClassTypePrm.Constraints.Add(baseClassBase);

			CodeConstructor baseClassCtor = new CodeConstructor();
			baseClassCtor.Attributes = MemberAttributes.Public;
			baseClass.Members.Add(baseClassCtor);
			baseClassCtor = new CodeConstructor();
			baseClassCtor.Attributes = MemberAttributes.Public;
			baseClassCtor.Parameters.Add(new CodeParameterDeclarationExpression(typeof (Int32), "id"));
            baseClassCtor.Parameters.Add(new CodeParameterDeclarationExpression("Worm.Cache.CacheBase", "cache"));
			baseClassCtor.Parameters.Add(new CodeParameterDeclarationExpression("Worm.ObjectMappingEngine", "schema"));
			baseClassCtor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("id"));
			baseClassCtor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("cache"));
			baseClassCtor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("schema"));
			baseClass.Members.Add(baseClassCtor);

			baseClass.BaseTypes.Add(baseClassBase);
			baseClass.Attributes = MemberAttributes.Public;
			baseClassNS.Types.Add(baseClass);

			units[idx++] = baseClassUnit;
			foreach (CodeCompileUnit unit in dic.Values)
			{
				units[idx++] = unit;
			}

			CompilerResults result = prov.CompileAssemblyFromDom(prms, units);
			foreach (CompilerError error in result.Errors)
			{
				Assert.IsTrue(error.IsWarning, error.ToString());
			}
		}
	}
}
