using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
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

        private Dictionary<Extension, XmlDocument> _extensions = new Dictionary<Extension, XmlDocument>();
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
        public Dictionary<Extension, XmlDocument> Extensions
        {
            get
            {
                return _extensions;
            }
        }

        public GenerateModeEnum GenerateMode { get; set; }

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

        public void AddRelation(RelationDefinitionBase r)
        {
            _relations.Add(r);
        }

        public IEnumerable<RelationDefinitionBase> OwnRelations
        {
            get
            {
                return _relations;
            }
        }

        public IEnumerable<RelationDefinitionBase> GetRelations()
        {
            IEnumerable<RelationDefinitionBase> r = _relations;
            foreach (WXMLModel model in Includes)
            {
                r = r.Union(model.GetRelations());
            }
            return r;
        }

        public IEnumerable<RelationDefinitionBase> GetActiveRelations()
        {
            return GetRelations().Where(r => !r.Disabled);
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

        public IEnumerable<EntityDefinition> OwnEntities
        {
            get
            {
                return _entities;
            }
        }

        #endregion Properties

        #region Methods

        public IEnumerable<EntityDefinition> GetDerived(string identifier)
        {
            var be = GetEntities().Where(item => item.BaseEntity != null && item.BaseEntity.Identifier == identifier);

            List<EntityDefinition> derived = new List<EntityDefinition>(be);

            foreach (EntityDefinition d in be)
            {
                foreach (EntityDefinition n in GetDerived(d.Identifier))
                {
                    derived.Insert(0, n);
                }
            }

            return derived;
        }


        public void AddSourceFragment(SourceFragmentDefinition newsf)
        {
            if (GetSourceFragments().Any(item => item.Identifier == newsf.Identifier))
                throw new ArgumentException(string.Format("SourceFragment {0} already in collection", newsf.Identifier));

            _sourceFragments.Add(newsf);
        }

        public void AddType(TypeDefinition type)
        {
            if (GetTypes().Any(item => item.Identifier == type.Identifier))
                throw new ArgumentException(string.Format("Type {0} already in collection", type.Identifier));

            _types.Add(type);
        }

        public TypeDefinition GetOrCreateClrType(Type t)
        {
            TypeDefinition td = GetTypes().FirstOrDefault(item => item.IsClrType && item.ClrType == t);
            if (td == null)
            {
                td = new TypeDefinition(t.ToString(), t);
                _types.Add(td);
            }
            return td;
        }

        public SourceFragmentDefinition GetOrCreateSourceFragment(string selector, string sourceName)
        {
            SourceFragmentDefinition sf = GetSourceFragments().FirstOrDefault(item => item.Selector == selector && item.Name == sourceName);
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
            EntityDefinition entity = GetActiveEntities()
                .SingleOrDefault(match => match.Identifier == entityId);

            if (entity == null && Includes.Count != 0)
                foreach (WXMLModel model in Includes)
                {
                    entity = model.GetEntity(entityId);
                    if (entity != null)
                        break;
                }
            if (entity == null && throwNotFoundException)
                throw new KeyNotFoundException(string.Format("Entity with id '{0}' not found.", entityId));
            return entity;
        }

        public IEnumerable<EntityDefinition> GetEntities()
        {
            IEnumerable<EntityDefinition> e = _entities;
            foreach (WXMLModel model in Includes)
            {
                e = e.Union(model.GetEntities());
            }
            return e;
        }

        public IEnumerable<EntityDefinition> GetActiveEntities()
        {
            IEnumerable<EntityDefinition> e = _entities.Where(item => !item.Disabled);
            foreach (WXMLModel model in Includes)
            {
                e = e.Union(model.GetActiveEntities());
            }
            return e;
        }

        public SourceFragmentDefinition GetSourceFragment(string tableId)
        {
            return GetSourceFragment(tableId, false);
        }

        public SourceFragmentDefinition GetSourceFragment(string tableId, bool throwNotFoundException)
        {
            var table = _sourceFragments.Find(match => match.Identifier == tableId);
            if (table == null && Includes.Count > 0)
                foreach (WXMLModel model in Includes)
                {
                    table = model.GetSourceFragment(tableId, false);
                    if (table != null)
                        break;
                }
            if (table == null && throwNotFoundException)
                throw new KeyNotFoundException(string.Format("SourceFragment with id '{0}' not found.", tableId));
            return table;
        }

        public IEnumerable<SourceFragmentDefinition> GetSourceFragments()
        {
            IEnumerable<SourceFragmentDefinition> sf = _sourceFragments;
            foreach (WXMLModel model in Includes)
            {
                sf = sf.Union(model.GetSourceFragments());
            }
            return sf;
        }

        public IEnumerable<SourceFragmentDefinition> OwnSourceFragments
        {
            get
            {
                return _sourceFragments;
            }
        }

        public TypeDefinition GetType(string typeId, bool throwNotFoundException)
        {
            TypeDefinition type = null;
            if (!string.IsNullOrEmpty(typeId))
            {
                type = _types.Find(match => match.Identifier == typeId);
                if (type == null && Includes.Count != 0)
                    foreach (WXMLModel model in Includes)
                    {
                        type = model.GetType(typeId, false);
                        if (type != null)
                            break;
                    }
                if (throwNotFoundException && type == null)
                    throw new KeyNotFoundException(string.Format("Type with id '{0}' not found.", typeId));
            }
            return type;
        }

        public IEnumerable<TypeDefinition> GetTypes()
        {
            IEnumerable<TypeDefinition> t = _types;
            foreach (WXMLModel model in Includes)
            {
                t = t.Union(model.GetTypes());
            }
            return t;
        }

        public IEnumerable<TypeDefinition> OwnTypes
        {
            get
            {
                return _types;
            }
        }

        #region Merge

        public void Merge(WXMLModel mergeWith)
        {
            MergeTypes(mergeWith);
            MergeTables(mergeWith);
            MergeEntities(mergeWith);
            MergeExtensions(Extensions, mergeWith.Extensions);
        }

        private static void MergeExtensions(IDictionary<Extension, XmlDocument> extensions, Dictionary<Extension, XmlDocument> newExtensions)
        {
            foreach (KeyValuePair<Extension, XmlDocument> extension in newExtensions)
            {
                if (extension.Key.Action == MergeAction.Delete)
                    extensions.Remove(extension.Key);
                else if (!extensions.ContainsKey(extension.Key))
                    extensions.Add(extension.Key, extension.Value);
            }
        }

        private void MergeTables(WXMLModel mergeWith)
        {
            foreach (SourceFragmentDefinition newsf in mergeWith.GetSourceFragments())
            {
                string newsfIdentifier = newsf.Identifier;
                SourceFragmentDefinition sf = GetSourceFragments().SingleOrDefault(item => item.Identifier == newsfIdentifier);
                if (sf != null)
                {
                    if (!string.IsNullOrEmpty(newsf.Name))
                        sf.Name = newsf.Name;

                    if (!string.IsNullOrEmpty(newsf.Selector))
                        sf.Selector = newsf.Selector;
                }
                else
                    AddSourceFragment(newsf);
            }
        }

        private void MergeTypes(WXMLModel model)
        {
            foreach (TypeDefinition newType in model.GetTypes())
            {
                string newTypeIdentifier = newType.Identifier;
                TypeDefinition type = GetTypes().SingleOrDefault(item => item.Identifier == newTypeIdentifier);
                if (type != null)
                {
                    if (type.ToString() != newType.ToString())
                        throw new NotSupportedException(string.Format("Type with identifier {0} already exists.", newTypeIdentifier));
                }
                else
                    AddType(newType);
            }
        }

        private void MergeEntities(WXMLModel mergeWith)
        {
            foreach (EntityDefinition newEntity in mergeWith.GetEntities())
            {
                string newEntityIdentifier = newEntity.Identifier;

                EntityDefinition entity = GetEntities().SingleOrDefault(item => item.Identifier == newEntityIdentifier);
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
                    entity.FamilyName = newEntity.FamilyName;

                    entity.ClearSourceFragments();
                    foreach (SourceFragmentRefDefinition newsf in newEntity.OwnSourceFragments)
                    {
                        //string newsfId = newsf.Identifier;
                        //SourceFragmentRefDefinition sf =
                        //    entity.GetSourceFragments().SingleOrDefault(item => item.Identifier == newsfId);
                        
                        //foreach (PropertyDefinition rp in entity.GetProperties()
                        //    .Where(item=>item.SourceFragment == sf))
                        //{
                        //    if (rp is ScalarPropertyDefinition)
                        //    {
                        //        ScalarPropertyDefinition property = rp as ScalarPropertyDefinition;
                        //        property.SourceField.SourceFragment = newsf;
                        //    }
                        //    else if (rp is EntityPropertyDefinition)
                        //    {
                        //        EntityPropertyDefinition property = rp as EntityPropertyDefinition;
                        //        property.SourceFragment = newsf;
                        //    }
                        //}

                        //entity.RemoveSourceFragment(sf);
                        entity.AddSourceFragment(newsf);
                    }
                    //List<SourceFragmentRefDefinition> torem = new List<SourceFragmentRefDefinition>();
                    //foreach (SourceFragmentRefDefinition newsf in newEntity.GetSourceFragments())
                    //{
                    //    string newsfId = newsf.Identifier;
                    //    SourceFragmentRefDefinition sf =
                    //        entity.GetSourceFragments().SingleOrDefault(item => item.Identifier == newsfId);

                    //    if (sf != null)
                    //    {
                    //        if (newsf.Action == MergeAction.Delete)
                    //        {
                    //            entity.RemoveSourceFragment(sf);
                    //            torem.Add(newsf);
                    //        }
                    //        else if (newsf.AnchorTable != null)
                    //        {
                    //            sf.AnchorTable = newsf.AnchorTable;
                    //            sf.JoinType = newsf.JoinType;
                    //            if (newsf.Conditions.Count > 0)
                    //            {
                    //                foreach (SourceFragmentRefDefinition.Condition c in newsf.Conditions)
                    //                {
                    //                    SourceFragmentRefDefinition.Condition ec =
                    //                        sf.Conditions.SingleOrDefault(item =>
                    //                            item.LeftColumn == c.LeftColumn &&
                    //                            item.RightColumn == c.RightColumn
                    //                        );
                    //                    if (ec != null)
                    //                    {
                    //                        if (c.Action == MergeAction.Delete)
                    //                            sf.Conditions.Remove(ec);
                    //                    }
                    //                    else
                    //                        sf.Conditions.Add(c);
                    //                }
                    //            }
                    //        }
                    //    }
                    //    else
                    //        entity.AddSourceFragment(newsf);
                    //}

                    //foreach (SourceFragmentRefDefinition newsf in torem)
                    //{
                    //    newEntity.RemoveSourceFragment(newsf);
                    //}

                    foreach (PropertyDefinition newProperty in newEntity.GetProperties())
                    {
                        string newPropertyName = newProperty.PropertyAlias;

                        PropertyDefinition rp =
                            entity.GetProperties().SingleOrDefault(item => item.PropertyAlias == newPropertyName);

                        if (rp != null)
                        {
                            if (newProperty.Action == MergeAction.Delete)
                            {
                                entity.RemoveProperty(rp);
                            }
                            else
                            {
                                rp.Name = MergeString(rp, newProperty, item => item.Name);
                                rp.ObsoleteDescripton = MergeString(rp, newProperty, item => item.ObsoleteDescripton);
                                rp.DefferedLoadGroup = MergeString(rp, newProperty, item => item.DefferedLoadGroup);
                                rp.Description = MergeString(rp, newProperty, item => item.Description);
                                if (newProperty.Attributes != Field2DbRelations.None)
                                {
                                    rp.Attributes = newProperty.Attributes;
                                }
                                rp.Group = newProperty.Group ?? rp.Group;
                                rp.PropertyType = newProperty.PropertyType ?? rp.PropertyType;
                                rp.Disabled = newProperty.Disabled;
                                rp.EnablePropertyChanged = newProperty.EnablePropertyChanged;
                                rp.SourceFragment = newProperty.SourceFragment ?? rp.SourceFragment;

                                if (newProperty.FieldAccessLevel != default(AccessLevel))
                                    rp.FieldAccessLevel = newProperty.FieldAccessLevel;

                                if (newProperty.PropertyAccessLevel != default(AccessLevel))
                                    rp.PropertyAccessLevel = newProperty.PropertyAccessLevel;

                                if (newProperty.Obsolete != default(ObsoleteType))
                                    rp.Obsolete = newProperty.Obsolete;

                                if (rp.GetType() != newProperty.GetType())
                                {
                                    PropertyDefinition newProp = null;
                                    if (rp is EntityPropertyDefinition && newProperty is ScalarPropertyDefinition)
                                    {
                                        newProp = new ScalarPropertyDefinition(entity, newPropertyName);
                                        rp.CopyTo(newProp);
                                    }
                                    else if (rp is ScalarPropertyDefinition && newProperty is EntityPropertyDefinition)
                                    {
                                        newProp = new EntityPropertyDefinition(rp as ScalarPropertyDefinition);
                                    }
                                    entity.RemoveProperty(rp);
                                    entity.AddProperty(newProp);
                                    rp = newProp;
                                }

                                if (rp is ScalarPropertyDefinition)
                                {
                                    ScalarPropertyDefinition property = rp as ScalarPropertyDefinition;
                                    property.SourceField.SourceType = MergeString(property, newProperty as ScalarPropertyDefinition, item => item.SourceType);
                                    property.SourceFieldAlias = MergeString(property, newProperty as ScalarPropertyDefinition, item => item.SourceFieldAlias);
                                    property.SourceField.SourceFieldExpression = MergeString(property, newProperty as ScalarPropertyDefinition, item => item.SourceFieldExpression);
                                    property.SourceField.IsNullable = ((ScalarPropertyDefinition) newProperty).IsNullable;
                                    property.SourceField.SourceTypeSize = ((ScalarPropertyDefinition) newProperty).SourceTypeSize ?? property.SourceTypeSize;
                                }
                            }
                        }
                        else
                            entity.AddProperty(newProperty);
                    }

                    MergeExtensions(entity.Extensions, newEntity.Extensions);
                }
                else
                    AddEntity(newEntity);
            }
        }

        private static string MergeString<T>(T existingProperty, T newProperty,
            Func<T, string> accessor)
            where T : PropertyDefinition
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
            return _relations.OfType<RelationDefinition>().Any(item =>
                relation != item && (
                (item.Left.Entity.Identifier == relation.Left.Entity.Identifier && item.Right.Entity.Identifier == relation.Right.Entity.Identifier) ||
                (item.Left.Entity.Identifier == relation.Right.Entity.Identifier && item.Right.Entity.Identifier == relation.Left.Entity.Identifier))
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

            IEnumerator IEnumerable.GetEnumerator()
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

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : class
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

    public class EqualityComparer<T, T2> : IEqualityComparer<T> where T2 : class
    {
        private readonly Func<T, T2> _accessor;

        public EqualityComparer(Func<T, T2> accessor)
        {
            _accessor = accessor;
        }

        public bool Equals(T x, T y)
        {
            return Equals(_accessor(x), _accessor(y));
        }

        public int GetHashCode(T obj)
        {
            return _accessor(obj).GetHashCode();
        }
    }
}
