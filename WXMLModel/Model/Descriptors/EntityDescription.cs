using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace WXML.Model.Descriptors
{
    public class EntityDescription
    {
        #region Private Fields
        private readonly string _id;
        private readonly string _name;
        private readonly string _description;
        private readonly List<SourceFragmentRefDescription> _sourceFragments;
        private readonly List<PropertyDescription> _properties;
        private readonly List<string> _suppressedProperties;
        private readonly WXMLModel _model;
        private EntityDescription _baseEntity;
        private Dictionary<string, object> _items = new Dictionary<string,object>();
        private Dictionary<string, XmlDocument> _extensions = new Dictionary<string, XmlDocument>();

        #endregion Private Fields

        public EntityDescription(string id, string name, string nameSpace, string description, WXMLModel ormObjectsDef)
            : this(id, name, nameSpace, description, ormObjectsDef, null)
        {
        }

        public EntityDescription(string id, string name, string nameSpace, string description, WXMLModel ormObjectsDef, EntityDescription baseEntity)
            : this(id, name, nameSpace, description, ormObjectsDef, baseEntity, EntityBehaviuor.ForcePartial)
        {

        }

        public EntityDescription(string id, string name, string nameSpace, string description, WXMLModel ormObjectsDef, EntityDescription baseEntity, EntityBehaviuor behaviour)
        {
            _id = id;
            _name = name;
            _description = description;
            _sourceFragments = new List<SourceFragmentRefDescription>();
            _properties = new List<PropertyDescription>();
            _suppressedProperties = new List<string>();
            _model = ormObjectsDef;
            RawNamespace = nameSpace;
            _baseEntity = baseEntity;
            Behaviour = behaviour;
        }

        #region Properties

        public Dictionary<string, XmlDocument> Extensions
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
        }

        public string Description
        {
            get { return _description; }
        }

        public IEnumerable<SourceFragmentRefDescription> GetSourceFragments()
        {
            if (InheritsBaseTables && _baseEntity != null)
                return _sourceFragments.Union(_baseEntity.GetSourceFragments(),
                  new EqualityComparer<SourceFragmentRefDescription, string>(
                      (item) => item.Identifier)
                );
            else
                return _sourceFragments;
        }

        public IEnumerable<PropertyDescription> GetCompleteProperties()
        {
            if (InheritsBaseTables && _baseEntity != null)
                return Properties.Union(_baseEntity.Properties.Select(item=>item.Clone(this)),
                    new EqualityComparer<PropertyDescription, string>((item) => item.PropertyAlias)
                );
            else
                return Properties; 
        }

        private class EqualityComparer<T, T2> : IEqualityComparer<T> where T2:class 
        {
            private readonly Func<T, T2> _accessor;

            public EqualityComparer(Func<T, T2> accessor)
            {
                _accessor = accessor;
            }

            public bool Equals(T x, T y)
            {
                return _accessor(x) == _accessor(y);
            }

            public int GetHashCode(T obj)
            {
                return _accessor(obj).GetHashCode();
            }
        }
        public void AddSourceFragment(SourceFragmentRefDescription sf)
        {
            CheckSourceFragment(sf);
            _sourceFragments.Add(sf);
        }

        private void CheckSourceFragment(SourceFragmentDescription sf)
        {
            if (!Model.SourceFragments.Any(item => item.Identifier == sf.Identifier))
                throw new ArgumentException(
                    string.Format("SourceFragment {0} not found in Model.SourceFragment collection", sf.Identifier));

            if (GetSourceFragments().Any(item => item.Identifier == sf.Identifier))
                throw new ArgumentException(
                    string.Format("SourceFragment {0} already in SourceFragments collection", sf.Identifier));
        }

        public void InsertSourceFragments(int pos, SourceFragmentRefDescription sf)
        {
            CheckSourceFragment(sf);
            _sourceFragments.Insert(pos, sf);
        }

        public void ClearSourceFragments()
        {
            _sourceFragments.Clear();
        }

        public IEnumerable<PropertyDescription> Properties
        {
            get { return _properties; }
        }

        public List<PropertyDescription> ActiveProperties
        {
            get { return _properties.FindAll(p=>!p.Disabled); }
        }

        public WXMLModel Model
        {
            get { return _model; }
        }

        public bool HasPk
        {
            get
            {
                return GetPKCount(false) > 0;
            }
        }

        public bool HasPkFlatEntity
        {
            get
            {
                return GetPKCount() > 0;
            }
        }



        public bool HasSinglePk
        {
            get
            {
                int s = GetPKCount();
                return (BaseEntity == null && s == 1) || (BaseEntity != null && BaseEntity.HasSinglePk);
            }
        }
        
        #endregion

        public int GetPKCount()
        {
            return GetPKCount(true);
        }

        public int GetPKCount(bool flatEntity)
        {
            var properties = flatEntity ? CompleteEntity.Properties : Properties;
            //int s = 0;
            //foreach (var propertyDescription in properties)
            //{
            //    if (propertyDescription.HasAttribute(Field2DbRelations.PK) 
            //        //&& propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32))
            //        )
            //        s++;
            //}
            //return s;
            return properties.Count(propertyDescription =>
                !propertyDescription.Disabled && propertyDescription.HasAttribute(Field2DbRelations.PK));
        }

        public PropertyDescription GetProperty(string propertyId)
        {
            return GetProperty(propertyId, false);
        }

        public bool IsAssignableFrom(EntityDescription ed)
        {
            if (ed == this)
                return true;
            if (ed.BaseEntity == null)
                return false;
            return IsAssignableFrom(ed.BaseEntity);
        }

        public PropertyDescription GetProperty(string propertyName, bool throwNotFoundException)
        {
            PropertyDescription result = Properties.SingleOrDefault(match => match.Name == propertyName);
            if (result == null && throwNotFoundException)
                throw new KeyNotFoundException(
                    string.Format("Property with name '{0}' in entity '{1}' not found.", propertyName, Identifier));
            return result;
        }

        public SourceFragmentDescription GetSourceFragment(string sourceFragmentId)
        {
            return GetSourceFragment(sourceFragmentId, false);
        }

        public SourceFragmentDescription GetSourceFragment(string tableId, bool throwNotFoundException)
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

        public List<RelationDescription> GetRelations(bool withDisabled)
        {
            List<RelationDescription> l = new List<RelationDescription>();
            foreach (RelationDescriptionBase rel in _model.Relations)
            {
                RelationDescription match = rel as RelationDescription;
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
                    throw new OrmCodeGenException("Существуют дублирующиеся M2M связи." + relationUnique.Key);
            }
            return l;
        }

        private static void FillUniqueRelations<T>(IEnumerable<T> baseEntityRealation, IDictionary<string, int> relationUniques)
            where T : RelationDescriptionBase
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
                    EntityDescription superBaseEntity = relationDescription.UnderlyingEntity.SuperBaseEntity;
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
            foreach (RelationDescriptionBase rel in _model.Relations)
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
                    throw new OrmCodeGenException("Существуют дублирующиеся M2M связи." + relationUnique.Key);
            }
            return l;
        }

        public List<RelationDescriptionBase> GetAllRelations(bool withDisabled)
        {
            List<RelationDescriptionBase> l = new List<RelationDescriptionBase>();

            foreach (RelationDescriptionBase relation in _model.Relations)
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
            get { return string.IsNullOrEmpty(RawNamespace) ? _model.Namespace : RawNamespace; }
            set { RawNamespace = value; }
        }

        public string RawNamespace { get; private set; }

        public EntityDescription BaseEntity
        {
            get { return _baseEntity; }
            set { _baseEntity = value; }
        }

        public EntityDescription SuperBaseEntity
        {
            get
            {
                EntityDescription superbaseEntity;
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

        private readonly List<EntityRelationDescription> _relations = new List<EntityRelationDescription>();

        public ICollection<EntityRelationDescription> EntityRelations
        {
            get
            {
                return _relations;
            }
        }

        public List<EntityRelationDescription> GetEntityRelations(bool withDisabled)
        {
            return _relations.FindAll(r => !r.Disabled);
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

        private static EntityDescription MergeEntities(EntityDescription oldOne, EntityDescription newOne)
        {
            EntityDescription resultOne =
                new EntityDescription(newOne.Identifier, newOne.Name, newOne.Namespace, newOne.Description ?? (oldOne==null?null:oldOne.Description),
                                      newOne.Model);
            if (oldOne != null)
            {
                resultOne.CacheCheckRequired = oldOne.CacheCheckRequired;
                resultOne.Behaviour = oldOne.Behaviour;
                resultOne.MakeInterface = oldOne.MakeInterface;
                resultOne.UseGenerics = oldOne.UseGenerics;
            }
            else
            {
                resultOne.CacheCheckRequired = newOne.CacheCheckRequired;
                resultOne.Behaviour = newOne.Behaviour;
                resultOne.MakeInterface = newOne.MakeInterface;
                resultOne.UseGenerics = newOne.UseGenerics;
            }

            //добавляем новые таблички
            foreach (var newTable in newOne.GetSourceFragments())
            {
                resultOne.AddSourceFragment(newTable);
            }
            // добавляем новые проперти
            foreach (PropertyDescription newProperty in newOne.Properties)
            {
                PropertyDescription prop = newProperty.Clone();
                PropertyDescription newProperty1 = newProperty;
                //if (newOne.SuppressedProperties.Exists(match => match.Name == newProperty1.Name))
                //    prop.IsSuppressed = true;
                resultOne.AddProperty(prop);
            }

            foreach (var newProperty in newOne.SuppressedProperties)
            {
                //PropertyDescription prop = newProperty.Clone();
                resultOne.SuppressedProperties.Add(newProperty);
            }

            if (oldOne != null)
            {
                // добавляем старые таблички, если нужно
                if (newOne.InheritsBaseTables)
                    foreach (var oldTable in oldOne.GetSourceFragments())
                    {
                        var oldTable1 = oldTable;
                        if (!resultOne.GetSourceFragments().Any(tableMatch => oldTable1.Name == tableMatch.Name && oldTable1.Selector == tableMatch.Selector))
                            resultOne.InsertSourceFragments(oldOne.GetSourceFragments().IndexOf(oldTable), oldTable);
                    }

                foreach (var oldProperty in oldOne.SuppressedProperties)
                {
                    //PropertyDescription prop = oldProperty.Clone();
                    resultOne.SuppressedProperties.Add(oldProperty);
                }

                // добавляем старые проперти, если нужно
                foreach (PropertyDescription oldProperty in oldOne.Properties)
                {
                    PropertyDescription newProperty = resultOne.GetProperty(oldProperty.Name);
                    if (newProperty == null || newProperty.Disabled)
                    {
                        SourceFragmentDescription newTable = null;
                        if (oldProperty.SourceFragment != null)
                            newTable = resultOne.GetSourceFragment(oldProperty.SourceFragment.Identifier);
                        TypeDescription newType = oldProperty.PropertyType;
                        PropertyDescription oldProperty1 = oldProperty;
                        //bool isSuppressed =
                        //    resultOne.SuppressedProperties.Exists(match => match.Name == oldProperty1.Name);
                        bool isRefreshed = false;
                        const bool fromBase = true;
                        if (newType.IsEntityType)
                        {
                            TypeDescription newType1 = newType;
                            EntityDescription newEntity =
                                resultOne.Model.ActiveEntities.SingleOrDefault(
                                    matchEntity =>
                                    matchEntity.BaseEntity != null && matchEntity.BaseEntity.Identifier == newType1.Entity.Identifier);
                            if (newEntity != null)
                            {
                                newType = new TypeDescription(newType.Identifier, newEntity);
                                isRefreshed = true;
                            }
                        }
                        resultOne.InsertProperty(resultOne.Properties.Count() - newOne.Properties.Count(),
                            new PropertyDescription(resultOne, oldProperty.Name, oldProperty.PropertyAlias,
                                oldProperty.Attributes,
                                oldProperty.Description,
                                newType,
                                oldProperty.FieldName, newTable, fromBase, oldProperty.FieldAccessLevel, oldProperty.PropertyAccessLevel, isRefreshed));
                    }
                }
            }

            return resultOne;
        }

        public EntityDescription CompleteEntity
        {
            get
            {
                EntityDescription baseEntity = _baseEntity == null ? null : _baseEntity.CompleteEntity;
                return MergeEntities(baseEntity, this);
            }
        }

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

        public PropertyDescription PkProperty
        {
            get
            {
                if (HasSinglePk)
                    foreach (var propertyDescription in CompleteEntity.Properties)
                    {
                        if (propertyDescription.HasAttribute(Field2DbRelations.PK)
                            //&& propertyDescription.PropertyType.IsClrType && propertyDescription.PropertyType.ClrType.IsAssignableFrom(typeof(Int32))
                            )
                            return propertyDescription;
                    }
                throw new InvalidOperationException("Only usable with single PK");
            }
        }

        public IEnumerable<PropertyDescription> PkProperties
        {
            get
            {
                return Properties.Where(p => p.HasAttribute(Field2DbRelations.PK));
            }
        }

        public bool HasDefferedLoadableProperties
        {
            get
            {
                return Properties.Any(p => !p.Disabled && !string.IsNullOrEmpty(p.DefferedLoadGroup));
            }
        }

        public bool HasDefferedLoadablePropertiesInHierarhy
        {
            get
            {
                return CompleteEntity.Properties.Any(p => !p.Disabled && !string.IsNullOrEmpty(p.DefferedLoadGroup));
            }
        }

        public Dictionary<string, List<PropertyDescription>> GetDefferedLoadProperties()
        {
            Dictionary<string, List<PropertyDescription>> groups = new Dictionary<string, List<PropertyDescription>>();

            foreach (var property in Properties)
            {
                if (property.Disabled || string.IsNullOrEmpty(property.DefferedLoadGroup))
                    continue;

                List<PropertyDescription> lst;
                if (!groups.TryGetValue(property.DefferedLoadGroup, out lst))
                    groups[property.DefferedLoadGroup] = lst = new List<PropertyDescription>();

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


        public bool IsMultitable
        {
            get
            {
                bool multitable = false;

                var entity = this;
                do
                {
                    multitable |= entity.CompleteEntity.GetSourceFragments().Count() > 1;
                    entity = entity.BaseEntity;
                } while (!multitable && entity != null);
                return multitable;
            }
        }

        public List<PropertyDescription> GetPropertiesFromBase()
        {
            var be = BaseEntity;

            List<PropertyDescription> baseProperties = new List<PropertyDescription>();

            while (be != null)
            {
                baseProperties.AddRange(be.Properties);
                be = be.BaseEntity;
            }

            return baseProperties;
        }

        public void AddProperty(PropertyDescription pe)
        {
            pe.Entity = this;
            
            CheckProperty(pe);

            _properties.Add(pe);
        }

        private void CheckProperty(PropertyDescription pe)
        {
            if (Properties.Any(item=>item.PropertyAlias==pe.PropertyAlias))
                throw new ArgumentException(string.Format("Property with alias {0} already exists", pe.PropertyAlias));

            if (pe.PropertyType != null && !Model.Types.Any(item=>item.Identifier == pe.PropertyType.Identifier))
                throw new ArgumentException(string.Format("Property {0} has type {1} which is not found in Model.Types collection", pe.PropertyAlias, pe.PropertyType.Identifier));

            if (pe.SourceFragment != null && !GetSourceFragments().Any(item => item.Identifier == pe.SourceFragment.Identifier))
                throw new ArgumentException(string.Format("Property {0} has SourceFragment {1} which is not found in Model.SourceFragments collection", pe.PropertyAlias, pe.SourceFragment.Identifier));
        }

        public void RemoveProperty(PropertyDescription pe)
        {
            pe.Entity = null;
            _properties.Remove(pe);
        }

        public void InsertProperty(int pos, PropertyDescription pe)
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
    }
}
