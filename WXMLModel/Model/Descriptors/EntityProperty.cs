﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WXML.Model.Descriptors
{
    public class EntityPropertyDefinition : PropertyDefinition
    {
        public class SourceField : SourceFieldDefinition
        {
            private readonly string _alias;
            private readonly string _propertyAlias;

            public SourceField(string propertyAlias, SourceFragmentDefinition sf, string column, int? sourceTypeSize,
                bool isNullable, string sourceType, string defaultValue, string alias)
                : base (sf, column, sourceTypeSize, isNullable, sourceType, false, defaultValue)
            {
                _alias = alias;
                _propertyAlias = propertyAlias;
            }

            public string PropertyAlias
            {
                get { return _propertyAlias; }
            }

            public string Alias
            {
                get { return _alias; }
            }
        }

        private readonly List<SourceField> _fields = new List<SourceField>();
        private SourceFragmentDefinition _sf;

        protected EntityPropertyDefinition() {}

        public EntityPropertyDefinition(ScalarPropertyDefinition pd)
        {
            if (!pd.PropertyType.IsEntityType)
                throw new ArgumentException(string.Format("EntityProperty type must be a entity type. Passed {0}", pd.PropertyType.Identifier));

            if (pd.PropertyType.Entity.GetPkProperties().Count() != 1)
                throw new ArgumentException(string.Format("Entity {0} must have single primary key", pd.PropertyType.Identifier));

            pd.CopyTo(this);
            _sf = pd.SourceFragment;
            _fields.Add(new SourceField(
                pd.PropertyType.Entity.GetPkProperties().First().PropertyAlias,
                _sf, pd.SourceFieldExpression, pd.SourceTypeSize, pd.IsNullable, pd.SourceType, 
                pd.SourceField.DefaultValue,pd.SourceFieldAlias
            ));
        }

        public EntityPropertyDefinition(string propertyName, string propertyAlias, 
            Field2DbRelations attributes, string description, 
            AccessLevel fieldAccessLevel, AccessLevel propertyAccessLevel, 
            TypeDefinition type, SourceFragmentDefinition sf, EntityDefinition entity)
            : base(propertyName, propertyAlias, type, attributes, description, fieldAccessLevel, propertyAccessLevel, entity)
        {
            if (!type.IsEntityType)
                throw new ArgumentException(string.Format("EntityProperty type must be a entity type. Passed {0}", type.Identifier));

            _sf = sf;
        }

        public IEnumerable<SourceField> SourceFields
        {
            get { return _fields; }
        }

        public void AddSourceField(string propertyAlias, string fieldName)
        {
            AddSourceField(propertyAlias, fieldName, null, null, null, true, null);
        }

        public void AddSourceField(string propertyAlias, string fieldName, string fieldAlias,
            string sourceTypeName, int? sourceTypeSize, bool IsNullable, string sourceFieldDefault)
        {
            if (string.IsNullOrEmpty(propertyAlias))
                throw new ArgumentNullException("propertyAlias");

            if (!PropertyType.Entity.GetProperties().Any(item => item.Identifier == propertyAlias))
                throw new ArgumentException(string.Format("Entity {0} has no property {1}", PropertyType.Entity.Identifier, propertyAlias));

            if (_fields.Any(item => item.PropertyAlias == propertyAlias))
                throw new ArgumentException(string.Format("PropertyAlias {0} already in collection", propertyAlias));

            _fields.Add(
                new SourceField(propertyAlias, SourceFragment, fieldName, sourceTypeSize, IsNullable, sourceTypeName, sourceFieldDefault, fieldAlias)
            );
        }

        public override SourceFragmentDefinition SourceFragment
        {
            get { return _sf; }
            set
            {
                _sf = value;
                foreach (SourceField field in SourceFields)
                {
                    field.SourceFragment = value;
                }
            }
        }

        protected override PropertyDefinition _Clone()
        {
            EntityPropertyDefinition prop = new EntityPropertyDefinition();
            CopyTo(prop);
            return prop;
        }

        public override void CopyTo(PropertyDefinition to)
        {
            base.CopyTo(to);
            EntityPropertyDefinition property = (to as EntityPropertyDefinition);
            if (property != null) 
                foreach (SourceField sf in _fields)
                {
                    property._fields.Add(sf);
                }
        }

        public TypeDefinition NeedReplace()
        {
            if (FromBase/* && PropertyType.IsEntityType*/)
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

    }
}
