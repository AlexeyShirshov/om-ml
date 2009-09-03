using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace WXML.Model.Descriptors
{
    public class EntityDefinition
    {
        #region Private Fields
        private readonly string _id;
        private string _name;
        private string _description;
        private readonly List<SourceFragmentRefDefinition> _sourceFragments;
        private readonly List<PropertyDefinition> _properties;
        private readonly List<string> _suppressedProperties;
        internal WXMLModel _model;
        private EntityDefinition _baseEntity;
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();
        private readonly Dictionary<Extension, XmlDocument> _extensions = new Dictionary<Extension, XmlDocument>();

        #endregion Private Fields

        public EntityDefinition(string id, string name, string @namespace)
            : this(id, name, @namespace, null, null, null)
        {
        }

        public EntityDefinition(string id, string name, string @namespace, string description, WXMLModel model)
            : this(id, name, @namespace, description, model, null)
        {
        }

        public EntityDefinition(string id, string name, string @namespace, string description, WXMLModel model, EntityDefinition baseEntity)
            : this(id, name, @namespace, description, model, baseEntity, EntityBehaviuor.ForcePartial)
        {

        }

        public EntityDefinition(string id, string name, string @namespace, string description, 
            WXMLModel model, EntityDefinition baseEntity, EntityBehaviuor behaviour)
        {
            _id = id;
            _name = name;
            _description = description;
            _sourceFragments = new List<SourceFragmentRefDefinition>();
            _properties = new List<PropertyDefinition>();
            _suppressedProperties = new List<string>();
            _model = model;
            EntitySpecificNamespace = @namespace;
            _baseEntity = baseEntity;
            Behaviour = behaviour;

            if (model != null && !model.GetEntities().Any(item=>item.Identifier == id))
                model.AddEntity(this);

        }

        #region Properties
        public MergeAction Action { get; set; }

        public Dictionary<Extension, XmlDocument> Extensions
        {
            get
            {
                return _extensions;
            }
        }

        public string Identifier
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public IEnumerable<SourceFragmentRefDefinition> GetSourceFragments()
        {
            if (InheritsBaseTables && _baseEntity != null)
                return _sourceFragments.Union(_baseEntity.GetSourceFragments(),
                    new EqualityComparer<SourceFragmentRefDefinition, string>((item) => item.Identifier)
                ).OrderBy(item=>_sourceFragments.Any(p=>p.Identifier == item.Identifier)?2:1);
            return _sourceFragments;
        }

        public IEnumerable<PropertyDefinition> GetProperties()
        {
            if (_baseEntity != null)
                return 
                    SelfProperties
                        .Union(_baseEntity.GetProperties().Select(item => item.Clone(this)),
                            new EqualityComparer<PropertyDefinition, string>(item=>item.PropertyAlias))
                        .OrderBy(item=>SelfProperties.Any(p=>p.Identifier == item.Identifier)?2:1);
            return SelfProperties;
        }

        public void RemoveSourceFragment(SourceFragmentRefDefinition sf)
        {
            int index = _sourceFragments.IndexOf(item=>item.Identifier == sf.Identifier);
            if (index >= 0)
                _sourceFragments.RemoveAt(index);
        }

        public void AddSourceFragment(SourceFragmentRefDefinition sf)
        {
            CheckSourceFragment(sf);
            _sourceFragments.Add(sf);
        }

        private void CheckSourceFragment(SourceFragmentDefinition sf)
        {
            if (Model != null && !Model.GetSourceFragments().Any(item => item.Identifier == sf.Identifier))
                throw new ArgumentException(
                    string.Format("SourceFragment {0} not found in Model.SourceFragment collection", sf.Identifier));

            if (GetSourceFragments().Any(item => item.Identifier == sf.Identifier))
                throw new ArgumentException(
                    string.Format("SourceFragment {0} already in SourceFragments collection", sf.Identifier));
        }

        public void InsertSourceFragments(int pos, SourceFragmentRefDefinition sf)
        {
            CheckSourceFragment(sf);
            _sourceFragments.Insert(pos, sf);
        }

        public void ClearSourceFragments()
        {
            _sourceFragments.Clear();
        }

        public IEnumerable<PropertyDefinition> SelfProperties
        {
            get { return _properties; }
        }

        public IEnumerable<PropertyDefinition> GetActiveProperties()
        {
            return GetProperties().Where(p => !p.Disabled);
        }

        public WXMLModel Model
        {
            get { return _model; }
        }

        //public bool HasPk
        //{
        //    get
        //    {
        //        return GetPKCount(false) > 0;
        //    }
        //}

        //public bool HasPkFlatEntity
        //{
        //    get
        //    {
        //        return GetPKCount() > 0;
        //    }
        //}

        //public bool HasSinglePk
        //{
        //    get
        //    {
        //        int s = GetPKCount();
        //        return (BaseEntity == null && s == 1) || (BaseEntity != null && BaseEntity.HasSinglePk);
        //    }
        //}

        #endregion

        //public int GetPKCount()
        //{
        //    return GetPKCount(true);
        //}

        //public int GetPKCount(bool flatEntity)
        //{
        //    var properties = flatEntity ? GetProperties() : SelfProperties;
        //    //int s = 0;
        //    //foreach (var propertyDescription in properties)
        //    //{
        //    //    if (propertyDescription.HasAttribute(Field2DbRelations.PK) 
        //    //        //&& propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32))
        //    //        )
        //    //        s++;
        //    //}
        //    //return s;
        //    return properties.Count(propertyDescription =>
        //        !propertyDescription.Disabled && propertyDescription.HasAttribute(Field2DbRelations.PK));
        //}

        public PropertyDefinition GetProperty(string propertyId)
        {
            return GetProperty(propertyId, false);
        }

        public bool IsAssignableFrom(EntityDefinition ed)
        {
            if (ed == this)
                return true;
            if (ed.BaseEntity == null)
                return false;
            return IsAssignableFrom(ed.BaseEntity);
        }

        public PropertyDefinition GetProperty(string propertyId, bool throwNotFoundException)
        {
            PropertyDefinition result = GetProperties().SingleOrDefault(match => match.Identifier == propertyId);
            if (result == null && throwNotFoundException)
                throw new KeyNotFoundException(
                    string.Format("Property with name '{0}' in entity '{1}' not found.", propertyId, Identifier));
            return result;
        }

        public SourceFragmentDefinition GetSourceFragment(string sourceFragmentId)
        {
            return GetSourceFragment(sourceFragmentId, false);
        }

        public SourceFragmentDefinition GetSourceFragment(string tableId, bool throwNotFoundException)
        {
            //System.Text.RegularExpressions.Match nameMatch = Worm.CodeGen.Core.Model.GetNsNameMatch(tableId);
            //string localTableId = tableId;
            //if(nameMatch.Success && nameMatch.Groups["name"].Success)
            //{
            //    localTableId = nameMatch.Groups["name"].Value;
            //}
            var table = GetSourceFragments().SingleOrDefault(match => match.Identifier == tableId);

            if (table == null && throwNotFoundException)
                throw new KeyNotFoundException(
                    string.Format("SourceFragment with id '{0}' in entity '{1}' not found.", tableId, Identifier));
            return table;
        }

        public List<RelationDefinition> GetRelations(bool withDisabled)
        {
            List<RelationDefinition> l = new List<RelationDefinition>();
            foreach (RelationDefinitionBase rel in _model.Relations)
            {
                RelationDefinition match = rel as RelationDefinition;
                if (match != null && (match.IsEntityTakePart(this)) &&
                        (!match.Disabled || withDisabled))
                {
                    l.Add(match);
                }
            }
            Dictionary<string, int> relationUniques = new Dictionary<string, int>();
            FillUniqueRelations(l, relationUniques);
            if (BaseEntity != null)
            {
                var baseEntityRealation = from r in BaseEntity.GetRelations(withDisabled)
                                          where !l.Contains(r)
                                          select r;
                FillUniqueRelations(baseEntityRealation, relationUniques);
            }
            foreach (var relationUnique in relationUniques)
            {
                if (relationUnique.Value > 1)
                    throw new WXMLException("Существуют дублирующиеся M2M связи." + relationUnique.Key);
            }
            return l;
        }

        private static void FillUniqueRelations<T>(IEnumerable<T> baseEntityRealation, IDictionary<string, int> relationUniques)
            where T : RelationDefinitionBase
        {
            foreach (var relationDescription in baseEntityRealation)
            {
                string key = string.Join("$$$", new[]
	                                                {
	                                                    relationDescription.SourceFragment.Name,
	                                                    relationDescription.Left.ToString(),
	                                                    relationDescription.Right.ToString(),
	                                                });
                if (relationDescription.UnderlyingEntity != null)
                {
                    EntityDefinition superBaseEntity = relationDescription.UnderlyingEntity.RootEntity;
                    key += "$" + (superBaseEntity == null ? relationDescription.UnderlyingEntity.Name : superBaseEntity.Name);
                }

                int val;
                if (!relationUniques.TryGetValue(key, out val))
                    val = 0;
                relationUniques[key] = ++val;

            }
        }

        public List<SelfRelationDescription> GetSelfRelations(bool withDisabled)
        {
            List<SelfRelationDescription> l = new List<SelfRelationDescription>();
            foreach (RelationDefinitionBase rel in _model.Relations)
            {
                SelfRelationDescription match = rel as SelfRelationDescription;
                if (match != null && (match.IsEntityTakePart(this)) &&
                        (!match.Disabled || withDisabled))
                {
                    l.Add(match);
                }
            }
            Dictionary<string, int> relationUniques = new Dictionary<string, int>();
            FillUniqueRelations(l, relationUniques);
            if (BaseEntity != null)
            {
                var baseEntityRealation = BaseEntity.GetRelations(withDisabled);
                FillUniqueRelations(baseEntityRealation, relationUniques);
            }
            foreach (var relationUnique in relationUniques)
            {
                if (relationUnique.Value > 1)
                    throw new WXMLException("Существуют дублирующиеся M2M связи." + relationUnique.Key);
            }
            return l;
        }

        public List<RelationDefinitionBase> GetAllRelations(bool withDisabled)
        {
            List<RelationDefinitionBase> l = new List<RelationDefinitionBase>();

            foreach (RelationDefinitionBase relation in _model.Relations)
            {
                if (relation.IsEntityTakePart(this) && (!relation.Disabled || withDisabled))
                {
                    l.Add(relation);
                }
            }
            return l;
        }

        public string Namespace
        {
            get { return string.IsNullOrEmpty(EntitySpecificNamespace) && _model != null ? _model.Namespace : EntitySpecificNamespace; }
            set { EntitySpecificNamespace = value; }
        }

        public string EntitySpecificNamespace { get; private set; }

        public EntityDefinition BaseEntity
        {
            get { return _baseEntity; }
            set
            {
                if (_baseEntity != null && _baseEntity.Identifier == Identifier)
                    throw new ArgumentException(string.Format("Entity {0} cannot inherits from self", Identifier));
                _baseEntity = value;
            }
        }

        public EntityDefinition RootEntity
        {
            get
            {
                EntityDefinition superbaseEntity;
                for (superbaseEntity = this;
                     superbaseEntity.BaseEntity != null;
                     superbaseEntity = superbaseEntity.BaseEntity)
                {

                }
                if (superbaseEntity == this)
                    superbaseEntity = null;
                return superbaseEntity;
            }
        }

        private readonly List<EntityRelationDefinition> _relations = new List<EntityRelationDefinition>();
        private string _familyName;

        public IEnumerable<EntityRelationDefinition> EntityRelations
        {
            get
            {
                return _relations;
            }
        }

        public IEnumerable<EntityRelationDefinition> GetEntityRelations(bool withDisabled)
        {
            return EntityRelations.Where(r => !r.Disabled);
        }

        //public string QualifiedIdentifier
        //{
        //    get
        //    {
        //        return
        //            (Model != null && !string.IsNullOrEmpty(Model.NS))
        //                ? Model.NS + ":" + Identifier
        //                : Identifier;
        //    }
        //}

/*
        private static EntityDefinition MergeEntities(EntityDefinition baseEntity, EntityDefinition newOne)
        {
            EntityDefinition resultOne =
                new EntityDefinition(newOne.Identifier, newOne.Name, newOne.Namespace, newOne.Description ?? (baseEntity == null ? null : baseEntity.Description),
                                      newOne.Model);
            //if (baseEntity != null)
            //{
            //    resultOne.CacheCheckRequired = baseEntity.CacheCheckRequired;
            //    resultOne.Behaviour = baseEntity.Behaviour;
            //    resultOne.MakeInterface = baseEntity.MakeInterface;
            //    resultOne.UseGenerics = baseEntity.UseGenerics;
            //}
            //else
            //{
                resultOne.CacheCheckRequired = newOne.CacheCheckRequired;
                resultOne.Behaviour = newOne.Behaviour;
                resultOne.MakeInterface = newOne.MakeInterface;
                resultOne.UseGenerics = newOne.UseGenerics;
            //}

            //добавляем новые таблички
            foreach (var newTable in newOne.GetSourceFragments())
            {
                resultOne.AddSourceFragment(new SourceFragmentRefDefinition(newTable));
            }
            // добавляем новые проперти
            foreach (PropertyDefinition newProperty in newOne.SelfProperties)
            {
                PropertyDefinition prop = newProperty.Clone();
                //PropertyDefinition newProperty1 = newProperty;
                //if (newOne.SuppressedProperties.Exists(match => match.Name == newProperty1.Name))
                //    prop.IsSuppressed = true;
                resultOne.AddProperty(prop);
            }

            foreach (var newProperty in newOne.SuppressedProperties)
            {
                //PropertyDescription prop = newProperty.Clone();
                resultOne.SuppressedProperties.Add(newProperty);
            }

            if (baseEntity != null)
            {
                // добавляем старые таблички, если нужно
                if (newOne.InheritsBaseTables)
                    foreach (var oldTable in baseEntity.GetSourceFragments())
                    {
                        var oldTable1 = oldTable;
                        if (!resultOne.GetSourceFragments().Any(tableMatch => oldTable1.Name == tableMatch.Name && oldTable1.Selector == tableMatch.Selector))
                            resultOne.InsertSourceFragments(baseEntity.GetSourceFragments().IndexOf(oldTable), oldTable);
                    }

                foreach (var oldProperty in baseEntity.SuppressedProperties)
                {
                    //PropertyDescription prop = oldProperty.Clone();
                    resultOne.SuppressedProperties.Add(oldProperty);
                }

                // добавляем старые проперти, если нужно
                foreach (PropertyDefinition oldProperty in baseEntity.SelfProperties)
                {
                    PropertyDefinition newProperty = resultOne.GetProperty(oldProperty.Identifier);
                    if (newProperty == null || newProperty.Disabled)
                    {
                        SourceFragmentDefinition newTable = null;
                        if (oldProperty.SourceFragment != null)
                            newTable = resultOne.GetSourceFragment(oldProperty.SourceFragment.Identifier);
                        TypeDefinition newType = oldProperty.PropertyType;
                        PropertyDefinition oldProperty1 = oldProperty;
                        //bool isSuppressed =
                        //    resultOne.SuppressedProperties.Exists(match => match.Name == oldProperty1.Name);
                        //bool isRefreshed = false;
                        //const bool fromBase = true;
                        if (newType.IsEntityType)
                        {
                            string newTypeEntityIdentifier = newType.Entity.Identifier;
                            EntityDefinition newEntity = resultOne.Model.GetActiveEntities().SingleOrDefault(
                                item => item.BaseEntity != null && 
                                item.BaseEntity.Identifier == newTypeEntityIdentifier
                            );
                            if (newEntity != null)
                            {
                                newType = new TypeDefinition(newType.Identifier, newEntity);
                                //isRefreshed = true;
                            }
                        }
                        resultOne.InsertProperty(resultOne.SelfProperties.Count() - newOne.SelfProperties.Count(),
                            new PropertyDefinition(resultOne, oldProperty.Name, oldProperty.PropertyAlias,
                                oldProperty.Attributes,
                                oldProperty.Description,
                                newType,
                                oldProperty.FieldName, newTable, oldProperty.FieldAccessLevel, oldProperty.PropertyAccessLevel));
                    }
                }
            }

            return resultOne;
        }
*/

        //public EntityDefinition CompleteEntity
        //{
        //    get
        //    {
        //        EntityDefinition baseEntity = _baseEntity == null ? null : _baseEntity.CompleteEntity;
        //        return MergeEntities(baseEntity, this);
        //    }
        //}

        public EntityBehaviuor Behaviour { get; set; }

        public List<string> SuppressedProperties
        {
            get { return _suppressedProperties; }
        }

        public bool InheritsBaseTables { get; set; }

        public bool UseGenerics { get; set; }

        public bool MakeInterface { get; set; }

        public bool Disabled { get; set; }

        public bool CacheCheckRequired { get; set; }

        public bool EnableCommonEventRaise
        {
            get
            {
                return _model.EnableCommonPropertyChangedFire && !_properties.Exists(prop => prop.EnablePropertyChanged);
            }
        }

        //public PropertyDefinition PkProperty
        //{
        //    get
        //    {
        //        if (HasSinglePk)
        //            foreach (var propertyDescription in CompleteEntity.Properties)
        //            {
        //                if (propertyDescription.HasAttribute(Field2DbRelations.PK)
        //                    //&& propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32))
        //                    )
        //                    return propertyDescription;
        //            }
        //        throw new InvalidOperationException("Only usable with single PK");
        //    }
        //}

        public IEnumerable<ScalarPropertyDefinition> GetPkProperties()
        {
            return GetProperties()
                .Where(p => !p.Disabled && p.HasAttribute(Field2DbRelations.PK))
                .Cast<ScalarPropertyDefinition>();
        }

        public bool HasDefferedLoadableProperties
        {
            get
            {
                return SelfProperties.Any(p => !p.Disabled && !string.IsNullOrEmpty(p.DefferedLoadGroup));
            }
        }

        public bool HasDefferedLoadablePropertiesInHierarhy
        {
            get
            {
                return GetProperties().Any(p => !p.Disabled && !string.IsNullOrEmpty(p.DefferedLoadGroup));
            }
        }

        public Dictionary<string, List<PropertyDefinition>> GetDefferedLoadProperties()
        {
            Dictionary<string, List<PropertyDefinition>> groups = new Dictionary<string, List<PropertyDefinition>>();

            foreach (var property in GetProperties())
            {
                if (property.Disabled || string.IsNullOrEmpty(property.DefferedLoadGroup))
                    continue;

                List<PropertyDefinition> lst;
                if (!groups.TryGetValue(property.DefferedLoadGroup, out lst))
                    groups[property.DefferedLoadGroup] = lst = new List<PropertyDefinition>();

                lst.Add(property);
            }
            return groups;
            //var res = new List<PropertyDescription[]>();
            //foreach (var list in groups.Values)
            //{
            //    res.Add(list.ToArray());
            //}
            //return res.ToArray();
        }

        public bool IsImplementMultitable
        {
            get
            {
                bool multitable = false;

                var entity = _baseEntity;
                while (!multitable && entity != null)
                {
                    multitable = entity.GetSourceFragments().Count() > 1;
                    entity = entity.BaseEntity;
                };
                return multitable;
                //return GetSourceFragments().Count() > 1;
            }
        }

        public IEnumerable<PropertyDefinition> GetPropertiesFromBase()
        {
            var be = BaseEntity;

            List<PropertyDefinition> baseProperties = new List<PropertyDefinition>();

            while (be != null)
            {
                baseProperties.AddRange(be.SelfProperties);
                be = be.BaseEntity;
            }

            return baseProperties;
            //return GetProperties().Except(SelfProperties);
        }

        public void AddProperty(PropertyDefinition pe)
        {
            CheckProperty(pe);

            pe.Entity = this;

            _properties.Add(pe);
        }

        private void CheckProperty(PropertyDefinition pe)
        {
            if (string.IsNullOrEmpty(pe.Name))
                throw new ArgumentException(string.Format("Property {0} has no Name", pe.Identifier));

            if (string.IsNullOrEmpty(pe.PropertyAlias))
                throw new ArgumentException(string.Format("Property {0} has no PropertyAlias", pe.Identifier));

            if (SelfProperties.Any(item => item.PropertyAlias == pe.PropertyAlias))
            {
                string t = pe.Entity==null?"unknown":pe.Entity.Identifier;
                
                throw new ArgumentException(string.Format(
                    "Property with alias {0} already exists in type {1}. Added from {2}", 
                    pe.PropertyAlias, Identifier, t));
            }

            if (SelfProperties.Any(item => item.Name == pe.Name))
            {
                string t = pe.Entity == null ? "unknown" : pe.Entity.Identifier;

                throw new ArgumentException(string.Format(
                    "Property with name {0} already exists in type {1}. Added from {2}",
                    pe.Name, Identifier, t));
            }

            if (Model != null && pe.PropertyType != null && !Model.GetTypes().Any(item => item.Identifier == pe.PropertyType.Identifier))
                throw new ArgumentException(string.Format("Property {0} has type {1} which is not found in Model.Types collection", pe.PropertyAlias, pe.PropertyType.Identifier));

            if (pe.SourceFragment != null && !GetSourceFragments().Any(item => item.Identifier == pe.SourceFragment.Identifier))
                throw new ArgumentException(string.Format("Property {0} has SourceFragment {1} which is not found in SourceFragments collection", pe.PropertyAlias, pe.SourceFragment.Identifier));
        }

        public void RemoveProperty(PropertyDefinition pe)
        {
            int index = _properties.IndexOf(item=>item.Identifier == pe.Identifier);
            if (index >= 0)
            {
                pe.Entity = null;
                _properties.RemoveAt(index);
            }
        }

        public void InsertProperty(int pos, PropertyDefinition pe)
        {
            pe.Entity = this;
            CheckProperty(pe);
            _properties.Insert(pos, pe);
        }

        public Dictionary<string, object> Items
        {
            get
            {
                return _items;
            }
        }

        public string FamilyName
        {
            get
            {
                return string.IsNullOrEmpty(_familyName) ? Name : _familyName;
            }
            set
            {
                _familyName = value;
            }
        }

        public IEnumerable<SourceFragmentRefDefinition> SelfSourceFragments
        {
            get
            {
                return _sourceFragments;
            }
        }

        //public void MarkAsDeleted(SourceFragmentRefDefinition sf)
        //{
        //    int index = _sourceFragments.IndexOf(item => item.Identifier == sf.Identifier);
        //    if (index >= 0)
        //        _sourceFragments[index].Action = MergeAction.Delete;
        //    else
        //        throw new ArgumentException(string.Format("SourceFragment {0} not found among SelfSourceFragments. Probably InheritsBase is true. Consider remove table inheritance and copy base tables to entity.", sf.Identifier));
        //}

        public void AddEntityRelations(EntityRelationDefinition entityRelation)
        {
            _relations.Add(entityRelation);
        }
    }
}
