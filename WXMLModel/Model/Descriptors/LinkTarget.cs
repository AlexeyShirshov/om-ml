using System.Collections.Generic;
using System.Linq;

namespace WXML.Model.Descriptors
{
    public class LinkTarget : SelfRelationTarget
    {
        private EntityDefinition _entity;
        private string[] _props;

    	public LinkTarget(EntityDefinition entity, string[] fieldName, string[] props, bool cascadeDelete)
    		: base(fieldName, cascadeDelete)
    	{
			_entity = entity;
    	    _props = props;
    	}

    	public LinkTarget(EntityDefinition entity, string[] fieldName,string[] props, bool cascadeDelete, string accessorName)
            : base(fieldName, cascadeDelete, accessorName)
        {
            _entity = entity;
            _props = props;
        }

        public EntityDefinition Entity
        {
            get { return _entity; }
            set { _entity = value; }
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
                return Entity.GetProperties().Where(item=>EntityProperties.Contains(item.PropertyAlias)).Cast<ScalarPropertyDefinition>();
            }
        } 

        public override bool Equals(object obj)
        {
            return Equals(obj as LinkTarget);
        }

        public bool Equals(LinkTarget obj)
        {
            return Equals((SelfRelationTarget)obj) && _entity.Name == obj._entity.Name;
        }

        public override int GetHashCode()
        {
            return _entity.Name.GetHashCode() ^ base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Join("-", FieldName) + "$" + Entity.Name;
        }
    }
}
