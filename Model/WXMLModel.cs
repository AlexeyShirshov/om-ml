using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using WXML.Model.Descriptors;
using System.Linq;

namespace WXML.Model
{
	[Serializable]
    public class WXMLModel
    {
        public const string NS_PREFIX = "oos";
        public const string NS_URI = "http://wise-orm.com/WXMLSchema.xsd";

        #region Private Fields

        private readonly List<EntityDescription> _entities;
		private readonly List<SourceFragmentDescription> _sourceFragments;
        private readonly List<RelationDescriptionBase> _relations;
    	//private readonly List<SelfRelationDescription> _selfRelations;
        private readonly List<TypeDescription> _types;
        private readonly IncludesCollection _includes;

	    private readonly List<string> _userComments;
        private readonly List<string> _systemComments;
        private readonly string _appName;
        private readonly string _appVersion;

	    private string _entityBaseTypeName;
		private TypeDescription _entityBaseType;

	    #endregion Private Fields

        public WXMLModel()
        {
            _entities = new List<EntityDescription>();
            _relations = new List<RelationDescriptionBase>();
        	//_selfRelations = new List<SelfRelationDescription>();
        	_sourceFragments = new List<SourceFragmentDescription>();
            _types = new List<TypeDescription>();
            _userComments = new List<string>();
            _systemComments = new List<string>();
            _includes = new IncludesCollection(this);

            Assembly ass = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        	_appName = ass.GetName().Name;
            _appVersion = ass.GetName().Version.ToString(4);
        	EnableReadOnlyPropertiesSetter = false;
            GenerateEntityName = true;
        }

        #region Properties

        public bool GenerateSchemaOnly { get; set; }

        public bool GenerateSingleFile { get; set; }

        public bool AddVersionToSchemaName { get; set; }

        public void ClearEntities()
        {
            _entities.Clear();
        }

        public void AddEntity(EntityDescription e)
        {
            if (_entities.Exists(ee => ee.Identifier == e.Identifier))
                throw new ArgumentException(String.Format("Entity {0} already in collection", e.Identifier));

            _entities.Add(e);
        }

        public void RemoveEntity(EntityDescription e)
        {
            _entities.Remove(e);
        }

        public IEnumerable<EntityDescription> Entities
        {
            get
            {
                return _entities;
            }
        }

        public IEnumerable<EntityDescription> ActiveEntities
		{
			get
			{
				return _entities.FindAll(e => !e.Disabled);
			}
		}

	    public IList<EntityDescription> FlatEntities
	    {
	        get
	        {
	            IList<EntityDescription> baseFlatEntities = ((BaseSchema == null) ? new List<EntityDescription>() : BaseSchema.FlatEntities);
	        	var entities = ActiveEntities;
	        	int count = entities.Count() + ((BaseSchema == null) ? 0 : baseFlatEntities.Count);
	            var list = new List<EntityDescription>(count);
	            list.AddRange(entities);

	            foreach (EntityDescription baseEntityDescription in baseFlatEntities)
	            {
	                string name = baseEntityDescription.Name;
                    if (!list.Exists(entityDescription => entityDescription.Name == name))
                        list.Add(baseEntityDescription);
	            }
	            return list;
	        }
	    }

		public List<SourceFragmentDescription> SourceFragments
		{
			get
			{
				return _sourceFragments;
			}
		}
		
        public List<RelationDescriptionBase> Relations
        {
            get
            {
                return _relations;
            }
        }

        public List<RelationDescriptionBase> ActiveRelations
        {
            get
            {
                return _relations.FindAll(r=>!r.Disabled);
            }
        }

        public List<TypeDescription> Types
        {
            get
            {
                return _types;
            }
        }

	    public string Namespace { get; set; }

	    public string SchemaVersion { get; set; }

	    public List<string> UserComments
        {
            get { return _userComments; }
        }

        public List<string> SystemComments
        {
            get { return _systemComments; }
        }

        public IncludesCollection Includes
        {
            get { return _includes; }
        }

	    public string FileUri { get; set; }

	    public string FileName { get; set; }


	    public WXMLModel BaseSchema { get; protected internal set; }

	    public TypeDescription EntityBaseType
		{
			get
			{
				if (_entityBaseType == null && !string.IsNullOrEmpty(_entityBaseTypeName))
					_entityBaseType = GetType(_entityBaseTypeName, false);
				return _entityBaseType;
			}
			set 
			{
				_entityBaseType = value;
				if (_entityBaseType != null)
					_entityBaseTypeName = _entityBaseType.Identifier;
			}
		}

		protected internal string EntityBaseTypeName
		{
			get
			{
				if (!string.IsNullOrEmpty(_entityBaseTypeName))
					_entityBaseType = GetType(_entityBaseTypeName, false);
				return _entityBaseTypeName;
			}
			set
			{
				_entityBaseTypeName = value;
				_entityBaseType = GetType(_entityBaseTypeName, false);
			}
		}

	    public bool EnableCommonPropertyChangedFire { get; set; }

	    public bool EnableReadOnlyPropertiesSetter { get; set; }

	    public LinqSettingsDescriptor LinqSettings { get; set; }

        public bool GenerateEntityName
        {
            get;
            set;
        }

	    //[XmlIgnore]
        //public List<SelfRelationDescription> SelfRelations
        //{
        //    get { return _selfRelations; }
        //}

    	#endregion Properties

        #region Methods

        public EntityDescription GetEntity(string entityId)
        {
            return GetEntity(entityId, false);
        }

        public EntityDescription GetEntity(string entityId, bool throwNotFoundException)
        {
            EntityDescription entity = ActiveEntities
                .SingleOrDefault(match => match.Identifier == entityId);
            
            if(entity == null && Includes.Count != 0)
                foreach (WXMLModel objectsDef in Includes)
                {
                    entity = objectsDef.GetEntity(entityId);
                    if (entity != null)
                        break;
                }
            if (entity == null && throwNotFoundException)
                throw new KeyNotFoundException(string.Format("Entity with id '{0}' not found.", entityId));
            return entity;
        }

        public SourceFragmentDescription GetSourceFragment(string tableId)
        {
            return GetSourceFragment(tableId, false);
        }

        public SourceFragmentDescription GetSourceFragment(string tableId, bool throwNotFoundException)
        {
            var table = SourceFragments.Find(match => match.Identifier == tableId);
            if(table == null && Includes.Count > 0)
                foreach (WXMLModel objectsDef in Includes)
                {
                    table = objectsDef.GetSourceFragment(tableId, false);
                    if (table != null)
                        break;
                }
            if (table == null && throwNotFoundException)
                throw new KeyNotFoundException(string.Format("SourceFragment with id '{0}' not found.", tableId));
            return table;
        }

        public TypeDescription GetType(string typeId, bool throwNotFoundException)
        {
            TypeDescription type = null;
            if (!string.IsNullOrEmpty(typeId))
            {
                type = Types.Find(delegate(TypeDescription match) { return match.Identifier == typeId; });
                if (type == null && Includes.Count != 0)
                    foreach (WXMLModel objectsDef in Includes)
                    {
                        type = objectsDef.GetType(typeId, false);
                        if (type != null)
                            break;
                    }
                if (throwNotFoundException && type == null)
                    throw new KeyNotFoundException(string.Format("Type with id '{0}' not found.", typeId));
            }
            return type;
        }

        //private OrmObjectsDef GetSearchScope(string name, out string localName)
        //{
        //    return GetSearchScope(name, out localName, false);
        //}

        //internal OrmObjectsDef GetSearchScope(string name, out string localName, bool throwNotFondException)
        //{
        //    Match nameMatch = GetNsNameMatch(name);
        //    OrmObjectsDef searchScope = this;
        //    if(nameMatch.Success)
        //    {
                
        //        if(nameMatch.Groups["prefix"].Success)
        //        {
        //            string prefix = nameMatch.Groups["prefix"].Value;
        //            ImportDescription import = Includes[prefix];
        //            if(import == null)
        //                if(throwNotFondException)
        //                    throw new KeyNotFoundException(string.Format("Import with prefix '{0}' not found.", prefix));
        //                else
        //                {
        //                    localName = null;
        //                    return null;
        //                }
        //            searchScope = import.Content;
                    
        //        }
        //        localName = nameMatch.Groups["name"].Value;
        //    }
        //    else
        //    {
        //        localName = name;
        //    }
        //    return searchScope;
        //}

        //internal static Match GetNsNameMatch(string name)
        //{
        //    Regex regex = new Regex(@"^(?:(?'prefix'[\w]{1,}):){0,1}(?'name'[\w\d-_.]+){1}$");
        //    return regex.Match(name);
        //}

        public RelationDescriptionBase GetSimilarRelation(RelationDescriptionBase relation)
        {
            return _relations.Find(delegate(RelationDescriptionBase match)
                                      {
                                          return
                                              relation.Similar(match);
                                      });
        }

        public bool HasSimilarRelationM2M(RelationDescription relation)
        {
            return _relations.OfType<RelationDescription>().Any((RelationDescription match)=>
                relation != match && (
                (match.Left.Entity.Identifier == relation.Left.Entity.Identifier && match.Right.Entity.Identifier == relation.Right.Entity.Identifier) ||
                (match.Left.Entity.Identifier == relation.Right.Entity.Identifier && match.Right.Entity.Identifier == relation.Left.Entity.Identifier))
            );
        }

        public static WXMLModel LoadFromXml(XmlReader reader)
        {
            return LoadFromXml(reader, null);
        }

        public static WXMLModel LoadFromXml(XmlReader reader, XmlResolver xmlResolver)
        {
            WXMLModel odef = WXMLModelReader.Parse(reader, xmlResolver);
            odef.CreateSystemComments();
            return odef;
        }

        public OrmXmlDocumentSet GetOrmXmlDocumentSet(WXMLModelWriterSettings settings)
        {
            CreateSystemComments();

            return WXMLModelWriter.Generate(this, settings);
        }

        private void CreateSystemComments()
        {
            AssemblyName executingAssemblyName = Assembly.GetExecutingAssembly().GetName();
            SystemComments.Clear();
            SystemComments.Add(string.Format("This file was generated by {0} v{1} application({3} v{4}).{2}", _appName, _appVersion, Environment.NewLine, executingAssemblyName.Name, executingAssemblyName.Version));
            SystemComments.Add(string.Format("By user '{0}' at {1:G}.{2}", Environment.UserName, DateTime.Now, Environment.NewLine));
        }

        public XmlDocument GetXmlDocument()
        {
            WXMLModelWriterSettings settings = new WXMLModelWriterSettings();
            OrmXmlDocumentSet set = GetOrmXmlDocumentSet(settings);
            return set[0].Document;
        }

        #endregion Methods

        public class IncludesCollection : IEnumerable<WXMLModel>
        {
            private readonly List<WXMLModel> m_list;
            private readonly WXMLModel _baseObjectsDef;

            public IncludesCollection(WXMLModel baseObjectsDef)
            {
                m_list = new List<WXMLModel>();
                _baseObjectsDef = baseObjectsDef;
            }

            public void Add(WXMLModel objectsDef)
            {
                if (IsSchemaPresentInTree(objectsDef))
                    throw new ArgumentException(
                        "Given objects definition object already present in include tree.");
                objectsDef.BaseSchema = _baseObjectsDef;
                m_list.Add(objectsDef);
            }

            public void Remove(WXMLModel objectsDef)
            {
                objectsDef.BaseSchema = null;
                m_list.Remove(objectsDef);
            }

            public void Clear()
            {
                m_list.Clear();
            }

            public int Count
            {
                get
                {
                    return m_list.Count;
                }
            }

            public WXMLModel this[int index]
            {
                get
                {
                    return m_list[index];
                }
                set
                {
                    m_list[index].BaseSchema = null;
                    m_list[index] = value;
                }
            }

            public int IndexOf(WXMLModel objectsDef)
            {
                return m_list.IndexOf(objectsDef);
            }

            protected bool IsSchemaPresentInTree(WXMLModel objectsDef)
            {
                if (m_list.Contains(objectsDef))
                    return true;
                foreach (WXMLModel ormObjectsDef in m_list)
                {
                    return ormObjectsDef.Includes.IsSchemaPresentInTree(objectsDef);
                }
                return false;
            }




            #region IEnumerable<OrmObjectsDef> Members

            public IEnumerator<WXMLModel> GetEnumerator()
            {
                return m_list.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return m_list.GetEnumerator();
            }

            #endregion
        }
    }
}
