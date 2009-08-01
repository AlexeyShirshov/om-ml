using System;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using System.CodeDom;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell;
using WXML.Model;
using VSLangProj80;
using System.Collections.Generic;
using WXML.CodeDom;
using WXML.CodeDom.CodeDomExtensions;
using WXMLToWorm;

namespace Worm.CodeGen.VSTool
{
    public class Pair
    {
        //public byte[] Data;
        public CodeCompileFileUnit Unit;
    }

    class Enumer : IEnumerable<Pair>
    {
        private WXMLModel _ormObjectsDef;
        private List<Pair> _units;

        public Enumer(WXMLModel ormObjectsDef, string ext)
        {
            _ormObjectsDef = ormObjectsDef;

            //Create the CodeCompileUnit from the passed-in XML file
            WXMLCodeDomGeneratorSettings settings = new WXMLCodeDomGeneratorSettings();
            settings.EntitySchemaDefClassNameSuffix = "SchemaDef";
            LinqToCodedom.CodeDomGenerator.Language language;
            switch (ext)
            {
                case ".cs":
                    settings.LanguageSpecificHacks = LanguageSpecificHacks.CSharp;
                    language = LinqToCodedom.CodeDomGenerator.Language.CSharp;
                    break;
                case ".vb":
                    settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;
                    language = LinqToCodedom.CodeDomGenerator.Language.VB;
                    break;
                default:
                    throw new NotSupportedException(ext);
                //case ".js":
                //    settings.LanguageSpecificHacks = LanguageSpecificHacks.VisualBasic;
                //    break;
            }
            settings.PrivateMembersPrefix = "m_";
            //settings.Split = false;

            //ormObjectsDef.GenerateSchemaOnly
            WormCodeDomGenerator generator = new WormCodeDomGenerator(ormObjectsDef, settings);

            if (ormObjectsDef.GenerateSingleFile)
            {
                CodeCompileFileUnit compileUnit = generator.GetFullSingleUnit(language);
                _units = new List<Pair>() { new Pair() { Unit = compileUnit } };
            }
            else
            {
                _units = new List<Pair>();
                foreach (var entity in ormObjectsDef.ActiveEntities)
                {
                    _units.Add(new Pair() { Unit = generator.GetEntityCompileUnits(entity.Identifier, language)[0] });
                }
            }
        }

        #region IEnumerable<Pair> Members

        public IEnumerator<Pair> GetEnumerator()
        {
            return _units.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _units.GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// This is the generator class. 
    /// When setting the 'Custom Tool' property of a C#, VB, or J# project item to "XmlClassGenerator", 
    /// the GenerateCode function will get called and will return the contents of the generated file 
    /// to the project system
    /// </summary>
    [ComVisible(true)]
    [Guid("30002F61-C5E0-4446-B306-8DD16358C006")]
    [CodeGeneratorRegistration(typeof(WormEntityClassGenerator), "C# WORM Entity Class Generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(WormEntityClassGenerator), "VB WORM Entity Class Generator", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(WormEntityClassGenerator), "J# WORM Entity Class Generator", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(WormEntityClassGenerator))]
    public class WormEntityClassGenerator :
        VsMultipleFileGenerator.VsMultipleFileGenerator<Pair>
    //Microsoft.Samples.VisualStudio.GeneratorSample.BaseCodeGeneratorWithSite
    {
#pragma warning disable 0414
        //The name of this generator (use for 'Custom Tool' property of project item)
        internal static string name = "WormEntityClassGenerator";
#pragma warning restore 0414

        protected override string GetFileName(Pair element)
        {
            return element.Unit.Filename + GetDefaultExtension();
        }

        public override byte[] GenerateContent(Pair element)
        {
            CodeCompileUnit compileUnit = element.Unit;
            CodeDomProvider provider = GetCodeProvider();

            using (StringWriter writer = new StringWriter(new StringBuilder()))
            {
                CodeGeneratorOptions options = new CodeGeneratorOptions();
                options.BlankLinesBetweenMembers = false;
                options.BracingStyle = "C";

                //Generate the code
                provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);

                if (this.CodeGeneratorProgress != null)
                {
                    //Report that we are done
                    this.CodeGeneratorProgress.Progress(100, 100);
                }
                writer.Flush();

                //Get the Encoding used by the writer. We're getting the WindowsCodePage encoding, 
                //which may not work with all languages
                Encoding enc = Encoding.GetEncoding(writer.Encoding.WindowsCodePage);

                //Get the preamble (byte-order mark) for our encoding
                byte[] preamble = enc.GetPreamble();
                int preambleLength = preamble.Length;

                //Convert the writer contents to a byte array
                byte[] body = enc.GetBytes(writer.ToString());

                //Prepend the preamble to body (store result in resized preamble array)
                Array.Resize<byte>(ref preamble, preambleLength + body.Length);
                Array.Copy(body, 0, preamble, preambleLength, body.Length);

                //Return the combined byte array
                return preamble;
            }
        }

        protected override IEnumerable<Pair> GenerateElements(string inputFileContent)
        {
            try
            {
                //Validate the XML file against the schema
                WXMLModel ormObjectsDef;
                using (StringReader contentReader = new StringReader(inputFileContent))
                {
                    try
                    {
                        using (XmlReader rdr = XmlReader.Create(contentReader))
                        {
                            ormObjectsDef = WXMLModel.LoadFromXml(rdr, new XmlUrlResolver());
                        }
                    }
                    catch (Exception e)
                    {
                        this.GeneratorError(4, e.ToString(), 1, 1);
                        //Returning null signifies that generation has failed
                        return null;
                    }
                }

                return new Enumer(ormObjectsDef, GetDefaultExtension());
            }
            catch (Exception e)
            {
                this.GeneratorError(4, e.ToString(), 1, 1);
                //Returning null signifies that generation has failed
                return null;
            }
        }
    }
}