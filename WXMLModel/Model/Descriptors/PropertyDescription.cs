using System;
using System.Collections.Generic;
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

    public class PropertyDescription : ICloneable
    {
        private string _name;
        private string _propertyAlias;
        private string[] _attributes;
        private string _description;
        private TypeDescription _type;
        private string _fieldName;
        private SourceFragmentDescription _table;
        private bool _fromBase;
        private AccessLevel _fieldAccessLevel;
        private AccessLevel _propertyAccessLevel;
        //private bool _isSuppressed;

        public PropertyDescription(EntityDescription entity, string name)
            : this(entity, name, name, null, null, null, null, null, false, AccessLevel.Private, AccessLevel.Public, false)
        {
        }

        public PropertyDescription(string name)
            : this(null, name, name, null, null, null, null, null, false, AccessLevel.Private, AccessLevel.Public, false)
        {
        }

        public PropertyDescription(EntityDescription entity, string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, SourceFragmentDescription table, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
            : this(entity,name, alias, attributes, description, type, fieldname, table, false, fieldAccessLevel, propertyAccessLevel, false)
        {
        }

        public PropertyDescription(string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, SourceFragmentDescription table, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel) 
            : this(null,name, alias, attributes, description, type, fieldname, table, false, fieldAccessLevel, propertyAccessLevel, false)
        {
        }

        internal PropertyDescription(EntityDescription entity, string name, string alias, string[] attributes, string description, TypeDescription type, string fieldname, SourceFragmentDescription table, bool fromBase, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel/*, bool isSuppressed*/, bool isRefreshed)
        {
            _name = name;
            _propertyAlias = string.IsNullOrEmpty(alias)?name:alias;
            _attributes = attributes;
            _description = description;
            _type = type;
            _fieldName = fieldname;
            _table = table;
            _fromBase = fromBase;
            _fieldAccessLevel = fieldAccessLevel;
            _propertyAccessLevel = propertyAccessLevel;
            //_isSuppressed = isSuppressed;
            IsRefreshed = isRefreshed;
            Entity = entity;
        }
        
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
                
        public string[] Attributes
        {
            get { return _attributes; }
            set { _attributes = value; }
        }
        
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public TypeDescription PropertyType
        {
            get { return _type; }
            set { _type = value; }
        }
                
        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }
                
        public SourceFragmentDescription SourceFragment
        {
            get { return _table; }
            set { _table = value; }
        }

        public bool FromBase
        {
            get { return _fromBase; }
            set { _fromBase = value; }
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
            get { return  _propertyAccessLevel; }
            set { _propertyAccessLevel = value; }
        }

        public bool IsSuppressed
        {
            get
            {
                return Entity.SuppressedProperties.Exists(item=>item == PropertyAlias);
            }
            //set { _isSuppressed = value; }
        }

        public bool IsRefreshed { get; set; }

        public EntityDescription Entity { get; set; }

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
            PropertyDescription prop = new PropertyDescription(Entity, Name)
            {
                Disabled=Disabled,
                Obsolete=Obsolete,
                ObsoleteDescripton = ObsoleteDescripton,
                EnablePropertyChanged = EnablePropertyChanged,
                DbTypeName = DbTypeName,
                DbTypeSize = DbTypeSize,
                DbTypeNullable=DbTypeNullable,
                Group = Group,
                FieldAlias = FieldAlias,
                DefferedLoadGroup = DefferedLoadGroup
            };
            prop._attributes = this._attributes;
            prop._description = this._description;
            prop._fieldAccessLevel = this._fieldAccessLevel;
            prop._fieldName = _fieldName;
            prop._fromBase = _fromBase;
            //prop._isSuppressed = _isSuppressed;
            prop._propertyAccessLevel = _propertyAccessLevel;
            prop._propertyAlias = _propertyAlias;
            prop._table = _table;
            prop._type = _type;
            return prop;
        }

        public PropertyDescription Clone()
        {
            return (PropertyDescription)(this as ICloneable).Clone();
        }

    	public bool HasAttribute(Field2DbRelations attribute)
    	{
    		bool hasIt = false;
    		foreach (string s in _attributes)
    		{
				if (((Field2DbRelations)Enum.Parse(typeof(Field2DbRelations), s, true) & attribute) == attribute)
				{
					hasIt = true;
					break;
				}
    		}
    		return hasIt;
    	}

        internal PropertyDescription Clone(EntityDescription entityDescription)
        {
            PropertyDescription p = Clone();
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
