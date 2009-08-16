using System;
using System.Collections.Generic;
using System.Text;

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

        public RelationDefinitionBase(SourceFragmentDefinition table, EntityDefinition underlyingEntity, SelfRelationTarget left, SelfRelationTarget right)
            : this(table, underlyingEntity, left, right, false)
        {
        }

        public RelationDefinitionBase(SourceFragmentDefinition table, EntityDefinition underlyingEntity, SelfRelationTarget left, SelfRelationTarget right, bool disabled)
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
    }
    
	public class SelfRelationDescription : RelationDefinitionBase
	{
		private readonly EntityDefinition _entity;

		public SelfRelationDescription(EntityDefinition entity, SelfRelationTarget direct, SelfRelationTarget reverse, SourceFragmentDefinition table, EntityDefinition underlyingEntity, bool disabled)
            : base(table, underlyingEntity, direct, reverse, disabled)
		{
			_entity = entity;
		}

        public SelfRelationDescription(EntityDefinition entity, SelfRelationTarget direct, SelfRelationTarget reverse, SourceFragmentDefinition table, EntityDefinition underlyingEntity)
            : this(entity, direct, reverse, table, underlyingEntity, false)
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

        public override bool Similar(RelationDefinitionBase obj)
        {
            return _Similar(obj as SelfRelationDescription);
        }

        public bool Similar(SelfRelationDescription obj)
        {
            return _Similar(obj);
        }

        protected bool _Similar(SelfRelationDescription obj)
        {
            return base.Similar((RelationDefinitionBase)obj) && _entity.Name == obj._entity.Name;
        }

		public override bool IsEntityTakePart(EntityDefinition entity)
		{
			return Entity == entity;
		}
	}
}
