using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.CodeDom;
using WXML.Model.Descriptors;
using System.Xml;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using WXML.Model;
using WXML.CodeDom;
using WXMLToWorm;

namespace WormCodeGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Worm code generator utility.");
            Console.WriteLine();

			CommandLine.Utility.Arguments cmdLine = new CommandLine.Utility.Arguments(args);

            string outputLanguage;
            string outputFolder;
            WXMLModel ormObjectsDef;
            string inputFilename;
            CodeDomProvider codeDomProvider;
            bool split, separateFolder;
            string[] skipEntities;
            string[] processEntities;
            WXMLCodeDomGeneratorSettings settings = new WXMLCodeDomGeneratorSettings();
            bool validateOnly, testRun;

            if (cmdLine["?"] != null || cmdLine["h"] != null || cmdLine["help"] != null || args == null || args.Length == 0)
            {
                Console.WriteLine("Command line parameters:");
                Console.WriteLine("  -f\t- source xml file");
                Console.WriteLine("  -v\t- validate input file against schema only");
                Console.WriteLine("  -t\t- test run (generate files in memory)");
                Console.WriteLine("  -l\t- code language [cs, vb] (\"cs\" by default)");
                //Console.WriteLine("  -p\t- generate partial classes (\"false\" by default)");
                Console.WriteLine("  -sp\t- split entity class and entity's schema definition\n\t\t  class code by diffrent files (\"false\" by default)");
                Console.WriteLine("  -sk\t- skip entities");
                Console.WriteLine("  -e\t- entities to process");
                //Console.WriteLine("  -cB\t- behaviour of class codegenerator\n\t\t  [Objects, PartialObjects] (\"Objects\" by default)");
                Console.WriteLine("  -sF\t- create folder for each entity.");
				Console.WriteLine("  -o\t- output files folder.");
                Console.WriteLine("  -pmp\t- private members prefix (\"_\" by default)");
                Console.WriteLine("  -cnP\t- class name prefix (null by default)");
                Console.WriteLine("  -cnS\t- class name suffix (null by default)");
                Console.WriteLine("  -fnP\t- file name prefix (null by default)");
                Console.WriteLine("  -fnS\t- file name suffix (null by default)");
                Console.WriteLine("  -propsT\t- use type instead of entity name in props (false by default)");
                Console.WriteLine("  -rm\t- remove old m2m methods (false by default)");
                Console.WriteLine("  -of\t- generate one file (false by default)");
                //Console.WriteLine("  -so\t- generate entity schema only (false by default)");
                return;
            }

            if (cmdLine["f"] != null)
                inputFilename = cmdLine["f"];
            else
            {
                Console.WriteLine("Please give 'f' parameter");
                return;
            }

            validateOnly = (cmdLine["v"] != null);
            testRun = (cmdLine["t"] != null);

            if (cmdLine["l"] != null)
                outputLanguage = cmdLine["l"];
            else
                outputLanguage = "CS";

            if (cmdLine["sF"] != null)
                separateFolder = true;
            else
                separateFolder = false;
            LanguageSpecificHacks languageHacks = LanguageSpecificHacks.None;
            if(outputLanguage.ToUpper() == "VB")
            {
                codeDomProvider = new VBCodeProvider();
                languageHacks = LanguageSpecificHacks.VisualBasic;
            }
            else if(outputLanguage.ToUpper() == "CS")
            {
                codeDomProvider = new CSharpCodeProvider();
                languageHacks = LanguageSpecificHacks.CSharp;
            }
            else
            {
                Console.WriteLine("Error: incorrect value in \"l\" parameter.");
                return;
            }

            if(cmdLine["sp"] != null)
                split = true;
            else
                split = false;


            if (cmdLine["o"] != null)
                outputFolder = cmdLine["o"];
            else
            {
                outputFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(outputFolder))
                {
                    outputFolder = System.IO.Path.GetPathRoot(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }
            }

            if (cmdLine["sk"] != null)
                skipEntities = cmdLine["sk"].Split(',');
            else
                skipEntities = new string[] { };

            if (cmdLine["e"] != null)
                processEntities = cmdLine["e"].Split(',');
            else
                processEntities = new string[] { };

            if(cmdLine["pmp"] != null)
            {
                settings.PrivateMembersPrefix = cmdLine["pmp"];
            }

            if(cmdLine["fnP"] != null)
            {
                settings.FileNamePrefix = cmdLine["fnP"];
            }
            if (cmdLine["fnS"] != null)
            {
                settings.FileNameSuffix = cmdLine["fnS"];
            }

            if (cmdLine["cnP"] != null)
            {
                settings.ClassNamePrefix = cmdLine["cnP"];
            }
            if (cmdLine["cnS"] != null)
            {
                settings.ClassNameSuffix = cmdLine["cnS"];
            }

            if (cmdLine["propsT"] != null)
            {
                settings.UseTypeInProps = true;
            }

            if (cmdLine["rm"] != null)
            {
                settings.RemoveOldM2M = true;
            }

            //if (cmdLine["os"] != null)
            //{
            //    settings.OnlySchema = true;
            //}

            bool oneFile = false;
            if (cmdLine["of"] != null)
            {
                oneFile = true;
            }

            if(!System.IO.File.Exists(inputFilename))
            {
                Console.WriteLine("Error: source file {0} not found.", inputFilename);
                return;
            }

            //if (!System.IO.Directory.Exists(outputFolder))
            //{
            //    Console.WriteLine("Error: output folder not found.");
            //    return;
            //}
            if(string.IsNullOrEmpty(System.IO.Path.GetDirectoryName(outputFolder)))
                outputFolder = System.IO.Path.GetPathRoot(outputFolder + System.IO.Path.DirectorySeparatorChar.ToString());
            else
                outputFolder = System.IO.Path.GetDirectoryName(outputFolder + System.IO.Path.DirectorySeparatorChar.ToString());

            try
            {
                Console.Write("Parsing file '{0}'...   ", inputFilename);
                using (XmlReader rdr = XmlReader.Create(inputFilename))
                {
                    ormObjectsDef = WXMLModel.LoadFromXml(rdr, new XmlUrlResolver());
                }
                Console.WriteLine("done!");
                if(validateOnly)
                {
                    Console.WriteLine("Input file validation success.");
                    return;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("error: {0}", exc.Message);
                Console.WriteLine("callstack: {0}", exc.StackTrace);
                if(exc.InnerException != null)
                    Console.WriteLine("error: {0}", exc.InnerException.Message);
                return;
            }

            if(!Directory.Exists(outputFolder))
            {
                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (Exception)
                {
                }
            }

            var gen = new WXMLToWorm.WormCodeDomGenerator(ormObjectsDef, settings);

            
			//settings.Split = split;
            settings.LanguageSpecificHacks = languageHacks;

            Console.WriteLine("Generation entities from file '{0}' using these settings:", inputFilename);
            Console.WriteLine("  Output folder: {0}", System.IO.Path.GetFullPath(outputFolder));
            Console.WriteLine("  Language: {0}", outputLanguage.ToLower());
            Console.WriteLine("  Split files: {0}", split);
            Console.WriteLine("  Skip entities: {0}", string.Join(" ", skipEntities));
            Console.WriteLine("  Process entities: {0}", string.Join(" ", processEntities));


            if (oneFile)
            {
                string privateFolder = outputFolder + System.IO.Path.DirectorySeparatorChar.ToString();

                CodeCompileUnit unit = gen.GetFullSingleUnit(typeof(Microsoft.VisualBasic.VBCodeProvider).IsAssignableFrom(codeDomProvider.GetType()) ? LinqToCodedom.CodeDomGenerator.Language.VB : LinqToCodedom.CodeDomGenerator.Language.CSharp);

                if (!System.IO.Directory.Exists(privateFolder))
                    System.IO.Directory.CreateDirectory(privateFolder);

                string outputFileName = System.IO.Path.GetFullPath(privateFolder + System.IO.Path.DirectorySeparatorChar.ToString() +
                        System.IO.Path.GetFileNameWithoutExtension(inputFilename));
                
                GenerateCode(codeDomProvider, unit, outputFileName, testRun);

                Console.WriteLine();

                Console.WriteLine("Result:");

                Console.WriteLine("\t{0} single generated.");
            }
            else
                GenerateMultipleFilesOutput(outputFolder, ormObjectsDef, codeDomProvider, separateFolder, 
                    skipEntities, processEntities, testRun, gen);
        }

        private static void GenerateMultipleFilesOutput(string outputFolder, WXMLModel ormObjectsDef, 
            CodeDomProvider codeDomProvider, bool separateFolder, 
            IEnumerable<string> skipEntities, IEnumerable<string> processEntities, bool testRun, 
            WormCodeDomGenerator gen)
        {
            List<string> errorList = new List<string>();
            int totalEntities = 0;
            int totalFiles = 0;
            foreach (EntityDefinition entity in ormObjectsDef.FileEntities)
            {
                //bool skip = false;
                //if (processEntities.Length != 0)
                //{
                //    skip = true;
                //    foreach (string processEntityId in processEntities)
                //    {
                //        if (processEntityId == entity.Identifier)
                //        {
                //            skip = false;
                //            break;
                //        }
                //    }
                //}
                //foreach (string skipEntityId in skipEntities)
                //{
                //    if (skipEntityId == entity.Identifier)
                //    {
                //        skip = true;
                //        break;
                //    }
                //}

                if (skipEntities.Contains(entity.Identifier) || processEntities.Contains(entity.Identifier))
                    continue;

                string privateFolder;
                if (separateFolder)
                    privateFolder = outputFolder + Path.DirectorySeparatorChar.ToString() + entity.Name + Path.DirectorySeparatorChar;
                else
                    privateFolder = outputFolder + Path.DirectorySeparatorChar.ToString();

                var units = gen.GetEntityCompileUnits(entity.Identifier, typeof(VBCodeProvider).IsAssignableFrom(codeDomProvider.GetType()) ? LinqToCodedom.CodeDomGenerator.Language.VB : LinqToCodedom.CodeDomGenerator.Language.CSharp);

                Console.Write(".");

                if (!Directory.Exists(privateFolder))
                    Directory.CreateDirectory(privateFolder);

                foreach (var unit in units)
                {
                    Console.Write(".");
                    try
                    {
                        GenerateCode(codeDomProvider, unit, Path.GetFullPath(privateFolder + Path.DirectorySeparatorChar.ToString() + unit.Filename), testRun);
                        Console.Write(".");
                        totalFiles++;
                    }
                    catch (Exception exc)
                    {
                        Console.Write(".");
                        errorList.Add(
                            string.Format("Entity: {0}; file: {1}; message: {2}", entity.Identifier, unit.Filename, exc.Message));
                    }
                }
                totalEntities++;
            }

            try
            {
                var ctx = gen.GetLinqContext();
                if (ctx != null)
                {
                    GenerateCode(codeDomProvider, ctx,
                                 Path.GetFullPath(outputFolder +
                                                            Path.DirectorySeparatorChar.ToString() +
                                                            ctx.Filename), testRun);
                    Console.Write(".");
                    totalFiles++;
                }
            }
            catch (Exception exc)
            {
                Console.Write(".");
                errorList.Add(
                    string.Format("Linq context file failed to generate: {0}", exc.Message));
            }

            Console.WriteLine();

            Console.WriteLine("Result:");
            Console.WriteLine("\t {0} entities processed", totalEntities);
            Console.WriteLine("\t {0} files generated", totalFiles);
            Console.WriteLine("\t {0} errors encountered", errorList.Count);
            if (errorList.Count != 0)
            {
                Console.WriteLine("Errors:");
                foreach (string s in errorList)
                {
                    Console.WriteLine("\t" + s);
                    for (int i = 0; i < Console.WindowWidth; i++)
                    {
                        Console.Write("-");
                    }
                    Console.WriteLine();
                }
            }
        }

        public static void GenerateCode(CodeDomProvider provider, CodeCompileUnit compileUnit, string filename, bool testRun)
        {
            String sourceFile;
            if (provider.FileExtension[0] == '.')
            {
                sourceFile = filename + provider.FileExtension;
            }
            else
            {
                sourceFile = filename + "." + provider.FileExtension;
            }
            Stream stream = null;
            try
            {

                if (testRun)
                    stream = new MemoryStream();
                else
                    stream = new FileStream(sourceFile, FileMode.Create, FileAccess.Write);

                using (StreamWriter sw = new StreamWriter(stream))
                {
                    using (IndentedTextWriter tw = new IndentedTextWriter(sw, "\t"))
                    {
                        CodeGeneratorOptions opts = new CodeGeneratorOptions();
                        opts.ElseOnClosing = false;
                        opts.BracingStyle = "C";
                        opts.ElseOnClosing = false;
                        opts.IndentString = "\t";
                        opts.VerbatimOrder = false;

                        provider.GenerateCodeFromCompileUnit(compileUnit, tw, opts);
                        tw.Close();
                    }
                }
            }
            finally
            {
                if (stream != null)
                    (stream as IDisposable).Dispose();
            }
        }

    }
}
