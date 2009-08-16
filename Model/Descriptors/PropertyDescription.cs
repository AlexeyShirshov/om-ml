using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WXML.Model.Descriptors
{
    public class PropertyGroup
    {
        public string Name
        {
            get;
            set;
        }

        public bool Hide
        {
            get;
            set;
        }
    }

    public class PropertyDefinition : ICloneable
    {
        private string _name;
        private string _propertyAlias;
        //private string[] _attributes;
        private Field2DbRelations _attributes;
        private string _description;
        private TypeDefinition _type;
        private string _fieldName;
        private SourceFragmentDefinition _table;
        //private bool _fromBase;
        private AccessLevel _fieldAccessLevel;
        private AccessLevel _propertyAccessLevel;
        //private bool _isSuppressed;

        public PropertyDefinition(EntityDefinition entity, string name)
            : this(entity, name, name, Field2DbRelations.None, null, null, null, null, AccessLevel.Private, AccessLevel.Public)
        {
        }

        public PropertyDefinition(string name)
            : this(null, name, name, Field2DbRelations.None, null, null, null, null, AccessLevel.Private, AccessLevel.Public)
        {
        }

        //public PropertyDefinition(EntityDefinition entity, string name, string alias, Field2DbRelations attributes,
        //    string description, TypeDefinition type, string fieldname, SourceFragmentDefinition table,
        //    AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
        //    : this(entity, name, alias, attributes, description, type, fieldname, table, fieldAccessLevel, propertyAccessLevel)
        //{
        //}

        public PropertyDefinition(string name, string alias, Field2DbRelations attributes, string description,
            TypeDefinition type, string fieldname, SourceFragmentDefinition table,
            AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
            : this(null, name, alias, attributes, description, type, fieldname, table, fieldAccessLevel, propertyAccessLevel)
        {
        }

        public PropertyDefinition(EntityDefinition entity, string name, string alias, Field2DbRelations attributes, 
            string description, TypeDefinition type, string fieldname, SourceFragmentDefinition table, 
            AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
        {
            _name = name;
            _propertyAlias = string.IsNullOrEmpty(alias) ? name : alias;
            _attributes = attributes;
            _description = description;
            _type = type;
            _fieldName = fieldname;
            _table = table;
            //_fromBase = fromBase;
            _fieldAccessLevel = fieldAccessLevel;
            _propertyAccessLevel = propertyAccessLevel;
            //_isSuppressed = isSuppressed;
            //NeedReplace = isRefreshed;
            Entity = entity;
        }

        public string Identifier
        {
            get
            {
                return _propertyAlias;
            }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public Field2DbRelations Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public TypeDefinition PropertyType
        {
            get { return _type; }
            set { _type = value; }
        }

        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }

        public SourceFragmentDefinition SourceFragment
        {
            get { return _table; }
            set { _table = value; }
        }

        public bool FromBase
        {
            get
            {
                return !Entity.SelfProperties.Any(item=>!item.Disabled && item.Identifier == Identifier);
            }
            //set { _fromBase = value; }
        }

        public string PropertyAlias
        {
            get { return string.IsNullOrEmpty(_propertyAlias) ? _name : _propertyAlias; }
            set { _propertyAlias = value; }
        }

        public AccessLevel FieldAccessLevel
        {
            get { return _fieldAccessLevel; }
            set { _fieldAccessLevel = value; }
        }

        public AccessLevel PropertyAccessLevel
        {
            get { return _propertyAccessLevel; }
            set { _propertyAccessLevel = value; }
        }

        public bool IsSuppressed
        {
            get
            {
                return Entity.SuppressedProperties.Exists(item => item == PropertyAlias);
            }
            //set { _isSuppressed = value; }
        }
        public MergeAction Action { get; set; }

        public TypeDefinition NeedReplace()
        {
            if (FromBase && PropertyType.IsEntityType)
            {
                var e = Entity.Model.GetDerived(PropertyType.Entity.Identifier).FirstOrDefault(item =>
                     !item.Disabled && item.Model.SchemaVersion == Entity.Model.SchemaVersion &&
                     item.FamilyName == PropertyType.Entity.FamilyName
                );

                if (e != null)
                    return Entity.Model.GetTypes().SingleOrDefault(
                        item => item.IsEntityType && item.Entity.Identifier == e.Identifier) ??
                        new TypeDefinition(e.Identifier, e);
            }
            return null;
        }

        public EntityDefinition Entity { get; set; }

        public bool Disabled { get; set; }

        public ObsoleteType Obsolete { get; set; }

        public string ObsoleteDescripton { get; set; }

        public bool EnablePropertyChanged { get; set; }

        public string DbTypeName { get; set; }

        public int? DbTypeSize { get; set; }

        public bool? DbTypeNullable { get; set; }

        public PropertyGroup Group { get; set; }

        public string FieldAlias { get; set; }

        public string DefferedLoadGroup { get; set; }

        object ICloneable.Clone()
        {
            PropertyDefinition prop = new PropertyDefinition(Entity, Name)
            {
                Disabled = Disabled,
                Obsolete = Obsolete,
                ObsoleteDescripton = ObsoleteDescripton,
                EnablePropertyChanged = EnablePropertyChanged,
                DbTypeName = DbTypeName,
                DbTypeSize = DbTypeSize,
                DbTypeNullable = DbTypeNullable,
                Group = Group,
                FieldAlias = FieldAlias,
                DefferedLoadGroup = DefferedLoadGroup
            };
            prop._attributes = this._attributes;
            prop._description = this._description;
            prop._fieldAccessLevel = this._fieldAccessLevel;
            prop._fieldName = _fieldName;
            //prop._fromBase = _fromBase;
            //prop._isSuppressed = _isSuppressed;
            prop._propertyAccessLevel = _propertyAccessLevel;
            prop._propertyAlias = _propertyAlias;
            prop._table = _table;
            prop._type = _type;
            return prop;
        }

        public PropertyDefinition Clone()
        {
            return (PropertyDefinition)(this as ICloneable).Clone();
        }

        public bool HasAttribute(Field2DbRelations attribute)
        {
            //bool hasIt = false;
            //foreach (string s in _attributes)
            //{
            //    if (((Field2DbRelations)Enum.Parse(typeof(Field2DbRelations), s, true) & attribute) == attribute)
            //    {
            //        hasIt = true;
            //        break;
            //    }
            //}
            //return hasIt;
            return (_attributes & attribute) == attribute;
        }

        internal PropertyDefinition Clone(EntityDefinition entityDescription)
        {
            PropertyDefinition p = Clone();
            p.Entity = entityDescription;
            return p;
        }
    }

    public enum ObsoleteType
    {
        None,
        Warning,
        Error
    }
}
