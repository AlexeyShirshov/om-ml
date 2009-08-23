using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.CodeDom
{
    public class WXMLCodeDomGeneratorSettings
    {
        private bool _split = false;
        private string _entitySchemaDefClassNameSuffix = "SchemaDef";
        private string _privateMembersPrefix = "_";
        private string _fileNamePrefix = string.Empty;
        private string _fileNameSuffix = string.Empty;
        private string _classNamePrefix = string.Empty;
        private string _classNameSuffix = string.Empty;
        private bool _useTypeInProps;
        //private bool _removeOldm2m;
        //private OrmObjectGeneratorBehaviour _behaviour = OrmObjectGeneratorBehaviour.Objects;
        private LanguageSpecificHacks _languageSpecificHacks; 

        //public bool IsPartial
        //{
        //    get { return _behaviour == OrmObjectGeneratorBehaviour.PartialObjects || _split || Partial; }
        //}

        public bool UseTypeInProps
        {
            get
            {
                return _useTypeInProps;
            }
            set
            {
                _useTypeInProps = value;
            }
        }

        //public bool RemoveOldM2M
        //{
        //    get { return _removeOldm2m; }
        //    set { _removeOldm2m = value; }
        //}

        //public bool OnlySchema { get; set; }

		[Obsolete("Don't even think about entity's class and entity's schema def class declaration in separate files.", true)]
        public bool Split
        {
            get { return _split; }
            set { _split = value; }
        }

        public string EntitySchemaDefClassNameSuffix
        {
            get { return _entitySchemaDefClassNameSuffix; }
            set { _entitySchemaDefClassNameSuffix = value; }
        }

        //public OrmObjectGeneratorBehaviour Behaviour
        //{
        //    get { return _behaviour; }
        //    set { _behaviour = value; }
        //}

        public LanguageSpecificHacks LanguageSpecificHacks
        {
            get { return _languageSpecificHacks; }
            set { _languageSpecificHacks = value; }
        }

        public string PrivateMembersPrefix
        {
            get { return _privateMembersPrefix; }
            set { _privateMembersPrefix = value; }
        }

        public string FileNamePrefix
        {
            get { return _fileNamePrefix; }
            set { _fileNamePrefix = value; }
        }

        public string FileNameSuffix
        {
            get { return _fileNameSuffix; }
            set { _fileNameSuffix = value; }
        }

        public string ClassNamePrefix
        {
            get { return _classNamePrefix; }
            set { _classNamePrefix = value; }
        }

        public string ClassNameSuffix
        {
            get { return _classNameSuffix; }
            set { _classNameSuffix = value; }
        }

        //public bool Partial
        //{
        //    get { return _partial; }
        //    set { _partial = value; }
        //}
    }

    //public enum OrmObjectGeneratorBehaviour
    //{
    //    Objects,
    //    PartialObjects
    //}
    [Flags]
    public enum LanguageSpecificHacks
    {
        None = 0,
        /// <summary>
        /// Generic члены производных классов требует наличия констрейтов
        /// </summary>
        DerivedGenericMembersRequireConstraits = 0x0001,
        /// <summary>
        /// Генерировать методы вместо параметризованых пропертей
        /// </summary>
        MethodsInsteadParametrizedProperties = 0x0002,
        AddOptionsStrict = 0x0004,
        OptionsStrictOn = 0x0008,
        AddOptionsExplicit = 0x0010,
        OptionsExplicitOn = 0x0020,
        GenerateCSUsingStatement = 0x0040,
        GenerateVBUsingStatement = 0x0080,
        /// <summary>
        /// Безопасная распаковка переменных с кастом в энам
        /// </summary>
        SafeUnboxToEnum = 0x0100,
		GenerateCsIsStatement = 0x0200,
		GenerateVbTypeOfIsStatement = 0x0400,
		GenerateCsAsStatement = 0x0800,
		GenerateVbTryCastStatement = 0x1000,
		GenerateCsLockStatement = 0x2000,
		GenerateVbSyncLockStatement = 0x4000,
        GenerateCsXorStatement = 0x8000,
        GenerateVbXorStatement = 0x10000,
        GenerateCsForeachStatement = 0x20000,
        GenerateVbForeachStatement = 0x40000,
        AllowPartialInterfaces = 0x80000,

        CSharp = MethodsInsteadParametrizedProperties | GenerateCSUsingStatement | SafeUnboxToEnum | GenerateCsAsStatement | GenerateCsIsStatement | GenerateCsLockStatement | GenerateCsXorStatement | GenerateCsForeachStatement | AllowPartialInterfaces,
        VisualBasic = DerivedGenericMembersRequireConstraits | AddOptionsExplicit | AddOptionsStrict | OptionsExplicitOn | OptionsStrictOn | GenerateVBUsingStatement | GenerateVbTryCastStatement | GenerateVbTypeOfIsStatement | GenerateVbSyncLockStatement | GenerateVbXorStatement | GenerateVbForeachStatement,
    }

}
