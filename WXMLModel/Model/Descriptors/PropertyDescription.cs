using System;
using System.Linq;

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

    public abstract class PropertyDefinition : ICloneable
    {
        private TypeDefinition _type;
        private string _name;
        private string _propertyAlias;
        private Field2DbRelations _attributes;
        private string _description;
        private AccessLevel _fieldAccessLevel;
        private AccessLevel _propertyAccessLevel;
        
        protected PropertyDefinition() {}

        public PropertyDefinition(string propertyName, string propertyAlias, TypeDefinition type, Field2DbRelations attributes, 
            string description, AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel, 
            EntityDefinition entity)
        {
            _name = propertyName;
            _type = type;
            _propertyAlias = string.IsNullOrEmpty(propertyAlias) ? propertyName : propertyAlias;
            _attributes = attributes;
            _description = description;
            _fieldAccessLevel = fieldAccessLevel;
            _propertyAccessLevel = propertyAccessLevel;
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

        public TypeDefinition PropertyType
        {
            get { return _type; }
            set { _type = value; }
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

        public EntityDefinition Entity { get; set; }

        public bool Disabled { get; set; }

        public ObsoleteType Obsolete { get; set; }

        public string ObsoleteDescripton { get; set; }

        public bool EnablePropertyChanged { get; set; }

        public PropertyGroup Group { get; set; }

        public string DefferedLoadGroup { get; set; }

        public bool HasAttribute(Field2DbRelations attribute)
        {
            return (_attributes & attribute) == attribute;
        }

        public bool FromBase
        {
            get
            {
                return !Entity.SelfProperties.Any(item => !item.Disabled && item.Identifier == Identifier);
            }
            //set { _fromBase = value; }
        }

        public abstract SourceFragmentDefinition SourceFragment { get; set; }

        public virtual void CopyTo(PropertyDefinition to)
        {
            to.Entity = Entity;
            to.Action = Action;
            to.DefferedLoadGroup = DefferedLoadGroup;
            to.Disabled = Disabled;
            to.EnablePropertyChanged = EnablePropertyChanged;
            to.Group = Group;
            to.Obsolete = Obsolete;
            to.ObsoleteDescripton = ObsoleteDescripton;
            to._attributes = _attributes;
            to._description = _description;
            to._fieldAccessLevel = _fieldAccessLevel;
            to._propertyAccessLevel = _propertyAccessLevel;
            to._propertyAlias = _propertyAlias;
            to._type = _type;
            to._name = _name;
        }

        protected abstract PropertyDefinition _Clone();

        object ICloneable.Clone()
        {
            return _Clone();
        }

        internal PropertyDefinition Clone(EntityDefinition entityDescription)
        {
            PropertyDefinition p = _Clone();
            p.Entity = entityDescription;
            return p;
        }
    }

    public class ScalarPropertyDefinition : PropertyDefinition
    {
        private SourceFieldDefinition _sf;

        protected ScalarPropertyDefinition() {}

        public ScalarPropertyDefinition(EntityDefinition entity, string propertyName)
            : this(entity, propertyName, propertyName, Field2DbRelations.None, null, null, null, AccessLevel.Private, AccessLevel.Public)
        {
        }

        //public ScalarPropertyDefinition(string propertyName)
        //    : this(null, propertyName, propertyName, Field2DbRelations.None, null, null, null, AccessLevel.Private, AccessLevel.Public)
        //{
        //}

        //public ScalarPropertyDefinition(string propertyName, string propertyAlias, Field2DbRelations attributes, string description,
        //    TypeDefinition type, SourceFieldDefinition sf,
        //    AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
        //    : this(null, propertyName, propertyAlias, attributes, description, type, sf, fieldAccessLevel, propertyAccessLevel)
        //{
        //}

        public ScalarPropertyDefinition(EntityDefinition entity, string propertyName, string propertyAlias, Field2DbRelations attributes,
            string description, TypeDefinition type, SourceFieldDefinition sf,
            AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel)
            : base(propertyName, propertyAlias, type, attributes, description, fieldAccessLevel, propertyAccessLevel, entity)
        {
            _sf = sf;
        }

        public SourceFieldDefinition SourceField
        {
            get
            {
                return _sf;
            }
            set
            {
                _sf = value;
            }
        }

        public string SourceFieldExpression
        {
            get
            {
                if (_sf != null) return _sf.SourceFieldExpression;
                return null;
            }
        }

        public override SourceFragmentDefinition SourceFragment
        {
            get
            {
                if (_sf != null) return _sf.SourceFragment;
                return null;
            }
            set { _sf.SourceFragment = value; }
        }

        public string SourceType
        {
            get
            {
                if (_sf != null) return _sf.SourceType;
                return null;
            }
        }

        public int? SourceTypeSize 
        {
            get
            {
                if (_sf != null) return _sf.SourceTypeSize;
                return null;
            }
        }

        public bool IsNullable
        {
            get
            {
                if (_sf != null) return _sf.IsNullable;
                return true;
            }
        }

        public string SourceFieldAlias { get; set; }

        public override void CopyTo(PropertyDefinition to)
        {
            base.CopyTo(to);
            ScalarPropertyDefinition definition = (to as ScalarPropertyDefinition);

            if (definition != null)
            {
                definition._sf = _sf;
                definition.SourceFieldAlias = SourceFieldAlias;
            }
        }

        protected override PropertyDefinition _Clone()
        {
            ScalarPropertyDefinition prop = new ScalarPropertyDefinition();
            CopyTo(prop);
            return prop;
        }

        public ScalarPropertyDefinition Clone()
        {
            return _Clone() as ScalarPropertyDefinition;
        }
    }

    public enum ObsoleteType
    {
        None,
        Warning,
        Error
    }
}

