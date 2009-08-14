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

        private readonly List<EntityDefinition> _entities;
		private readonly List<SourceFragmentDefinition> _sourceFragments;
        private readonly List<RelationDefinitionBase> _relations;
    	//private readonly List<SelfRelationDescription> _selfRelations;
        private readonly List<TypeDefinition> _types;
        private readonly IncludesCollection _includes;

	    private readonly List<string> _userComments;
        private readonly List<string> _systemComments;
        private readonly string _appName;
        private readonly string _appVersion;

	    private string _entityBaseTypeName;
		private TypeDefinition _entityBaseType;

        private Dictionary<string, XmlDocument> _extensions = new Dictionary<string, XmlDocument>();
	    #endregion Private Fields

        public WXMLModel()
        {
            _entities = new List<EntityDefinition>();
            _relations = new List<RelationDefinitionBase>();
        	//_selfRelations = new List<SelfRelationDescription>();
        	_sourceFragments = new List<SourceFragmentDefinition>();
            _types = new List<TypeDefinition>();
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
        public Dictionary<string, XmlDocument> Extensions
        {
            get
            {
                return _extensions;
            }
        }

        public bool GenerateSchemaOnly { get; set; }

        public bool GenerateSingleFile { get; set; }

        public bool AddVersionToSchemaName { get; set; }

        public void ClearEntities()
        {
            _entities.Clear();
        }

        public void AddEntity(EntityDefinition e)
        {
            if (_entities.Exists(ee => ee.Identifier == e.Identifier))
                throw new ArgumentException(String.Format("Entity {0} already in collection", e.Identifier));

            //if (e.Model != this)
            //    throw new InvalidOperationException(string.Format("Entity {0} belongs to another model", e.Identifier));

            _entities.Add(e);
            e._model = this;
        }

        public void RemoveEntity(EntityDefinition e)
        {
            _entities.Remove(e);
        }

        public IEnumerable<EntityDefinition> Entities
        {
            get
            {
                return _entities;
            }
        }

        public IEnumerable<EntityDefinition> ActiveEntities
		{
			get
			{
				return _entities.FindAll(e => !e.Disabled);
			}
		}

	    public IList<EntityDefinition> FlatEntities
	    {
	        get
	        {
	            IList<EntityDefinition> baseFlatEntities = ((BaseSchema == null) ? new List<EntityDefinition>() : BaseSchema.FlatEntities);
	        	var entities = ActiveEntities;
	        	int count = entities.Count() + ((BaseSchema == null) ? 0 : baseFlatEntities.Count);
	            var list = new List<EntityDefinition>(count);
	            list.AddRange(entities);

	            foreach (EntityDefinition baseEntityDescription in baseFlatEntities)
	            {
	                string name = baseEntityDescription.Name;
                    if (!list.Exists(entityDescription => entityDescription.Name == name))
                        list.Add(baseEntityDescription);
	            }
	            return list;
	        }
	    }

		public List<SourceFragmentDefinition> SourceFragments
		{
			get
			{
				return _sourceFragments;
			}
		}
		
        public List<RelationDefinitionBase> Relations
        {
            get
            {
                return _relations;
            }
        }

        public List<RelationDefinitionBase> ActiveRelations
        {
            get
            {
                return _relations.FindAll(r=>!r.Disabled);
            }
        }

        public List<TypeDefinition> Types
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

	    public TypeDefinition EntityBaseType
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

        public TypeDefinition GetOrCreateClrType(Type t)
        {
            TypeDefinition td = Types.FirstOrDefault(item => item.IsClrType && item.ClrType == t);
            if (td == null)
            {
                td = new TypeDefinition(t.ToString(), t);
                _types.Add(td);
            }
            return td;
        }

        public SourceFragmentDefinition GetOrCreateSourceFragment(string selector, string sourceName)
        {
            SourceFragmentDefinition sf = SourceFragments.FirstOrDefault(item => item.Selector == selector && item.Name == sourceName);
            if (sf == null)
            {
                sf = new SourceFragmentDefinition(selector + "." + sourceName, sourceName, selector);
                _sourceFragments.Add(sf);
            }
            return sf;
        }

	    public EntityDefinition GetEntity(string entityId)
        {
            return GetEntity(entityId, false);
        }

        public EntityDefinition GetEntity(string entityId, bool throwNotFoundException)
        {
            EntityDefinition entity = ActiveEntities
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

        public SourceFragmentDefinition GetSourceFragment(string tableId)
        {
            return GetSourceFragment(tableId, false);
        }

        public SourceFragmentDefinition GetSourceFragment(string tableId, bool throwNotFoundException)
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

        public TypeDefinition GetType(string typeId, bool throwNotFoundException)
        {
            TypeDefinition type = null;
            if (!string.IsNullOrEmpty(typeId))
            {
                type = Types.Find(delegate(TypeDefinition match) { return match.Identifier == typeId; });
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

        #region Merge
        public void Merge(WXMLModel mergeWith)
        {
            MergeTypes(mergeWith);
            MergeTables(mergeWith);
            MergeEntities(mergeWith);
            MergeExtensions(Extensions, mergeWith.Extensions);
        }

        private static void MergeExtensions(Dictionary<string, XmlDocument> extensions, Dictionary<string, XmlDocument> newExtensions)
        {
            foreach (KeyValuePair<string, XmlDocument> extension in newExtensions)
            {
                if (!extensions.ContainsKey(extension.Key))
                    extensions.Add(extension.Key, extension.Value);
            }
        }

        private void MergeTables(WXMLModel mergeWith)
        {
            foreach (SourceFragmentDefinition newsf in mergeWith.SourceFragments)
            {
                string newsfIdentifier = newsf.Identifier;
                SourceFragmentDefinition sf = SourceFragments.SingleOrDefault(item => item.Identifier == newsfIdentifier);
                if (sf != null)
                {
                    if (!string.IsNullOrEmpty(newsf.Name))
                        sf.Name = newsf.Name;

                    if (!string.IsNullOrEmpty(newsf.Selector))
                        sf.Selector = newsf.Selector;
                }
                else
                    SourceFragments.Add(newsf);
            }
        }

	    private void MergeTypes(WXMLModel model)
	    {
	        foreach (TypeDefinition newType in model.Types)
	        {
	            string newTypeIdentifier = newType.Identifier;
	            TypeDefinition type = Types.SingleOrDefault(item => item.Identifier == newTypeIdentifier);
                if (type != null)
                {
                    if (type.ToString() != newType.ToString())
                        throw new NotSupportedException(string.Format("Type with identifier {0} already exists.", newTypeIdentifier));
                }
                else
                    Types.Add(newType);
	        }
	    }

	    private void MergeEntities(WXMLModel mergeWith)
	    {
	        foreach (EntityDefinition newEntity in mergeWith.Entities)
	        {
	            string newEntityIdentifier = newEntity.Identifier;

	            EntityDefinition entity = Entities.SingleOrDefault(item => item.Identifier == newEntityIdentifier);
	            if (entity != null)
	            {
                    if (!string.IsNullOrEmpty(newEntity.Name))
                        entity.Name = newEntity.Name;

                    entity.Namespace = newEntity.Namespace;
                    entity.BaseEntity = newEntity.BaseEntity;
	                entity.Behaviour = newEntity.Behaviour;
	                entity.CacheCheckRequired = newEntity.CacheCheckRequired;
	                entity.Description = newEntity.Description;
	                entity.Disabled = newEntity.Disabled;
	                entity.InheritsBaseTables = newEntity.InheritsBaseTables;
	                entity.MakeInterface = newEntity.MakeInterface;
	                entity.UseGenerics = newEntity.UseGenerics;

	                foreach (SourceFragmentRefDefinition newsf in newEntity.GetSourceFragments())
	                {
	                    string newsfId = newsf.Identifier;
	                    SourceFragmentRefDefinition sf =
	                        entity.GetSourceFragments().SingleOrDefault(item => item.Identifier == newsfId);

                        if (sf != null)
                        {
                            if (newsf.AnchorTable != null)
                            {
                                sf.AnchorTable = newsf.AnchorTable;
                                sf.JoinType = newsf.JoinType;
                                if (newsf.Conditions.Count > 0)
                                {
                                    sf.Conditions.Clear();
                                    sf.Conditions.AddRange(newsf.Conditions);
                                }
                            }
                        }
                        else
                            entity.AddSourceFragment(newsf);
	                }

                    foreach (PropertyDefinition newProperty in newEntity.GetProperties())
                    {
                        string newPropertyName = newProperty.PropertyAlias;

                        PropertyDefinition property =
                            entity.GetProperties().SingleOrDefault(item => item.PropertyAlias == newPropertyName);

                        if (property != null)
                        {
                            property.DbTypeName = MergeString(property, newProperty, (item) => item.DbTypeName);
                            property.DefferedLoadGroup = MergeString(property, newProperty, (item) => item.DefferedLoadGroup);
                            property.Description = MergeString(property, newProperty, (item) => item.Description);
                            property.FieldAlias = MergeString(property, newProperty, (item) => item.FieldAlias);
                            property.FieldName = MergeString(property, newProperty, (item) => item.FieldName);
                            property.Name = MergeString(property, newProperty, (item) => item.Name);
                            property.ObsoleteDescripton = MergeString(property, newProperty, (item) => item.ObsoleteDescripton);

                            //List<string> newAttributes = new List<string>();

                            //if (property.Attributes != )
                            //    newAttributes.AddRange(property.Attributes);
                            if (newProperty.Attributes != Field2DbRelations.None)
                            {
                                //newAttributes.AddRange(newProperty.Attributes.Where(item=>!newAttributes.Contains(item)));
                                property.Attributes = newProperty.Attributes;
                            }

                            property.DbTypeNullable = newProperty.DbTypeNullable ?? property.DbTypeNullable;
                            property.DbTypeSize = newProperty.DbTypeSize ?? property.DbTypeSize;
                            property.Group = newProperty.Group ?? property.Group;
                            property.PropertyType = newProperty.PropertyType ?? property.PropertyType;
                            property.SourceFragment = newProperty.SourceFragment ?? property.SourceFragment;

                            property.Disabled = newProperty.Disabled;
                            property.EnablePropertyChanged = newProperty.EnablePropertyChanged;
                            //property.IsSuppressed = newProperty.IsSuppressed;
                            property.FromBase = newProperty.FromBase;

                            if (newProperty.FieldAccessLevel != default(AccessLevel))
                                property.FieldAccessLevel = newProperty.FieldAccessLevel;

                            if (newProperty.PropertyAccessLevel != default(AccessLevel))
                                property.PropertyAccessLevel = newProperty.PropertyAccessLevel;

                            if (newProperty.Obsolete != default(ObsoleteType))
                                property.Obsolete = newProperty.Obsolete;

                        }
                        else
                            entity.AddProperty(newProperty);
                    }

                    MergeExtensions(entity.Extensions, entity.Extensions);
                }
	            else
	                AddEntity(newEntity);
	        }
	    }

        private static string MergeString(PropertyDefinition existingProperty, PropertyDefinition newProperty,
            Func<PropertyDefinition, string> accessor)
        {
            return string.IsNullOrEmpty(accessor(newProperty))
              ? accessor(existingProperty)
              : accessor(newProperty);
        }
        
	    #endregion
        
        public RelationDefinitionBase GetSimilarRelation(RelationDefinitionBase relation)
        {
            return _relations.Find(relation.Similar);
        }

        public bool HasSimilarRelationM2M(RelationDefinition relation)
        {
            return _relations.OfType<RelationDefinition>().Any((RelationDefinition match)=>
                relation != match && (
                (match.Left.Entity.Identifier == relation.Left.Entity.Identifier && match.Right.Entity.Identifier == relation.Right.Entity.Identifier) ||
                (match.Left.Entity.Identifier == relation.Right.Entity.Identifier && match.Right.Entity.Identifier == relation.Left.Entity.Identifier))
            );
        }

        public static WXMLModel LoadFromXml(string fileName)
        {
            using (XmlTextReader reader = new XmlTextReader(fileName))
            {
                return LoadFromXml(reader, null);
            }
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

        public WXMLDocumentSet GetWXMLDocumentSet(WXMLModelWriterSettings settings)
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
            WXMLDocumentSet set = GetWXMLDocumentSet(settings);
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

            #region IEnumerable<Model> Members

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

    public static class Ext
    {
        public static int IndexOf<T>(this IEnumerable<T> source, T element) where T : class
        {
            if (source == null)
                return -1;
            if (element == null)
                return -1;
            int i = 0;
            foreach (T item in source)
            {
                if (item.Equals(element))
                    return i;
                i++;
            }
            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T,bool> predicate) where T : class
        {
            if (source == null)
                return -1;

            int i = 0;
            foreach (T item in source)
            {
                if (predicate(item))
                    return i;
                i++;
            }
            return -1;
        }
    }
}
