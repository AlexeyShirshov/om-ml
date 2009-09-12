using System.Collections.Generic;
using System.Linq;

namespace WXML.Model.Descriptors
{
    public abstract class RelationDefinitionBase
    {
        private readonly SourceFragmentDefinition _table;
        private readonly EntityDefinition _underlyingEntity;
        private readonly bool _disabled;
        private readonly SelfRelationTarget _left;
        private readonly SelfRelationTarget _right;
        private readonly List<RelationConstantDescriptor> _constants;
        private readonly Dictionary<string, object> _items = new Dictionary<string, object>();

        public override bool Equals(object obj)
        {
            return base.Equals(obj as RelationDefinitionBase);
        }

        public bool Equals(RelationDefinitionBase obj)
        {
            if (obj == null)
                return false;

            return _table.Identifier == obj._table.Identifier && _left == obj._left && _right == obj._right;
        }

        public override int GetHashCode()
        {
            return _table.GetHashCode() ^ _left.GetHashCode() ^ _right.GetHashCode();
        }

        public SourceFragmentDefinition SourceFragment
        {
            get { return _table; }
        }

        public EntityDefinition UnderlyingEntity
        {
            get { return _underlyingEntity; }
        }

        public bool Disabled
        {
            get { return _disabled; }
        }

        protected RelationDefinitionBase(SourceFragmentDefinition table, EntityDefinition underlyingEntity, SelfRelationTarget left, SelfRelationTarget right)
            : this(table, underlyingEntity, left, right, false)
        {
        }

        protected RelationDefinitionBase(SourceFragmentDefinition table, EntityDefinition underlyingEntity, SelfRelationTarget left, SelfRelationTarget right, bool disabled)
        {
            _table = table;
            _underlyingEntity = underlyingEntity;
            _disabled = disabled;
            _left = left;
            _right = right;
            _constants = new List<RelationConstantDescriptor>();
        }

        public IList<RelationConstantDescriptor> Constants
        {
            get
            {
                return _constants;
            }
        }

        public SelfRelationTarget Left
        {
            get { return _left; }
        }

        public SelfRelationTarget Right
        {
            get { return _right; }
        }

        public virtual bool Similar(RelationDefinitionBase obj)
        {
            if (obj == null)
                return false;

            return (_left == obj._left && _right == obj._right) ||
                (_left == obj._right && _right == obj._left);
        }

    	public abstract bool IsEntityTakePart(EntityDefinition entity);

    	public virtual bool HasAccessors
    	{
			get
			{
				return !string.IsNullOrEmpty(Left.AccessorName) || !string.IsNullOrEmpty(Right.AccessorName);
			}
    	}

        public MergeAction Action { get; set; }

        public Dictionary<string, object> Items
        {
            get
            {
                return _items;
            }
        }

        public RelationConstraint Constraint { get; set; }
    }
    
	public class SelfRelationDefinition : RelationDefinitionBase
	{
		private readonly EntityDefinition _entity;
	    private string[] _props;

	    public SelfRelationDefinition(EntityDefinition entity, string[] props, 
            SelfRelationTarget direct, SelfRelationTarget reverse, 
            SourceFragmentDefinition table, EntityDefinition underlyingEntity, bool disabled)
            : base(table, underlyingEntity, direct, reverse, disabled)
		{
			_entity = entity;
	        _props = props;
		}

        public SelfRelationDefinition(EntityDefinition entity, string[] props, 
            SelfRelationTarget direct, SelfRelationTarget reverse, 
            SourceFragmentDefinition table, EntityDefinition underlyingEntity)
            : this(entity, props, direct, reverse, table, underlyingEntity, false)
        {
        }

		public EntityDefinition Entity
		{
			get { return _entity; }
		}        		

		public SelfRelationTarget Direct
		{
			get { return Left; }
		}

		public SelfRelationTarget Reverse
		{
			get { return Right; }
		}

        public string[] EntityProperties
        {
            get { return _props; }
            set { _props = value; }
        }

        public IEnumerable<ScalarPropertyDefinition> Properties
        {
            get
            {
                return Entity.GetProperties().Where(item => EntityProperties.Contains(item.PropertyAlias)).Cast<ScalarPropertyDefinition>();
            }
        } 

        public override bool Similar(RelationDefinitionBase obj)
        {
            return _Similar(obj as SelfRelationDefinition);
        }

        public bool Similar(SelfRelationDefinition obj)
        {
            return _Similar(obj);
        }

        protected bool _Similar(SelfRelationDefinition obj)
        {
            return base.Similar(obj) && _entity.Name == obj._entity.Name;
        }

		public override bool IsEntityTakePart(EntityDefinition entity)
		{
			return Entity == entity;
		}
	}
}
