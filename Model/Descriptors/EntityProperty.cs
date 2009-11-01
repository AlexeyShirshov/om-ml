using System;
using System.Collections.Generic;
using System.Linq;

namespace WXML.Model.Descriptors
{
    public class EntityPropertyDefinition : PropertyDefinition
    {
        public class SourceField : SourceFieldDefinition
        {
            private readonly string _alias;
            private string _propertyAlias;

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
                set { _propertyAlias = value; }
            }

            public string SourceFieldAlias
            {
                get { return _alias; }
            }
        }

        private readonly List<SourceField> _fields = new List<SourceField>();
        private SourceFragmentDefinition _sf;

        protected EntityPropertyDefinition() {}

        public EntityPropertyDefinition(ScalarPropertyDefinition pd)
        {
            if (pd.PropertyType.Entity.GetPkProperties().Count() != 1)
                throw new ArgumentException(string.Format("Entity {0} must have single primary key", pd.PropertyType.Identifier));

            InitFromScalar(this, pd);
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

        public bool RemoveSourceField(SourceField field)
        {
            return _fields.Remove(field);
        }

        public bool RemoveSourceField(string propertyAlias)
        {
            return RemoveSourceField(_fields.Find(item => item.PropertyAlias == propertyAlias));
        }

        public bool RemoveSourceFieldByExpression(string sourceExpression)
        {
            return RemoveSourceField(_fields.Find(item => item.SourceFieldExpression == sourceExpression));
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

        internal void AddSourceFieldUnckeck(string propertyAlias, string fieldName, string fieldAlias,
            string sourceTypeName, int? sourceTypeSize, bool IsNullable, string sourceFieldDefault)
        {
            if (string.IsNullOrEmpty(propertyAlias))
                throw new ArgumentNullException("propertyAlias");

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

        public ScalarPropertyDefinition ToPropertyDefinition()
        {
            if (SourceFields.Count() > 1)
                throw new InvalidOperationException(string.Format("Cannot convert property {0} to PropertyDefinition", Identifier));

            var sf = SourceFields.First();

            var p = new ScalarPropertyDefinition(Entity, Name)
            {
                SourceField = new SourceFieldDefinition(SourceFragment,
                    sf.SourceFieldExpression, sf.SourceTypeSize, sf.IsNullable,
                    sf.SourceType, false, sf.DefaultValue)
            };
            CopyTo(p);
            return p;
        }

        internal static EntityPropertyDefinition FromScalar(ScalarPropertyDefinition pd)
        {
            return InitFromScalar(new EntityPropertyDefinition(), pd);
        }

        internal static EntityPropertyDefinition InitFromScalar(EntityPropertyDefinition ed, ScalarPropertyDefinition pd)
        {
            if (!pd.PropertyType.IsEntityType)
                throw new ArgumentException(string.Format("EntityProperty type must be a entity type. Passed {0}", pd.PropertyType.Identifier));

            pd.CopyTo(ed);
            ed._sf = pd.SourceFragment;
            var pks = pd.PropertyType.Entity.GetPkProperties();
            string propAlias = null;
            if (pks.Count() != 0)
                propAlias = pks.First().PropertyAlias;

            SourceField field = new SourceField(
                propAlias,
                ed._sf, pd.SourceFieldExpression, pd.SourceTypeSize, pd.IsNullable, pd.SourceType,
                pd.SourceField.DefaultValue, pd.SourceFieldAlias
            );

            ed._fields.Add(field);

            if (string.IsNullOrEmpty(propAlias))
            {
                EntityDefinition.PropertyAddedDelegate d = null;
                d = (e, p) =>
                {
                    if (p.HasAttribute(Field2DbRelations.PK))
                    {
                        field.PropertyAlias = p.PropertyAlias;
                        e.PropertyAdded -= d;
                    }
                };
                pd.PropertyType.Entity.PropertyAdded += d;
            }
            return ed;
        }

        public override bool HasMapping
        {
            get
            {
                return _fields.Count(f=>!string.IsNullOrEmpty(f.SourceFieldExpression)) > 0;
            }
        }
    }
}
