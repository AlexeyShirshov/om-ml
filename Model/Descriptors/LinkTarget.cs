namespace WXML.Model.Descriptors
{
    public class LinkTarget : SelfRelationTarget
    {
        private EntityDescription _entity;
        //private string _fieldName;
        //private bool _cascadeDelete;

    	public LinkTarget(EntityDescription entity, string fieldName, bool cascadeDelete)
    		: base(fieldName, cascadeDelete)
    	{
			_entity = entity;
    	}

    	public LinkTarget(EntityDescription entity, string fieldName, bool cascadeDelete, string accessorName)
            : base(fieldName, cascadeDelete, accessorName)
        {
            _entity = entity;
            //_fieldName = fieldName;
            //_cascadeDelete = cascadeDelete;
        }

        public EntityDescription Entity
        {
            get { return _entity; }
            set { _entity = value; }
        }

        //public string FieldName
        //{
        //    get { return _fieldName; }
        //    set { _fieldName = value; }
        //}   

        //public bool CascadeDelete
        //{
        //    get { return _cascadeDelete; }
        //    set { _cascadeDelete = value; }
        //}

        //public static bool IsSimilar(LinkTarget first, LinkTarget second)
        //{
        //    bool yep = true;
        //    yep &= first.Entity.Name == second.Entity.Name;
        //    yep &= first.FieldName == second.FieldName;
        //    return yep;
        //}

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
            return FieldName + "$" + Entity.Name;
        }
    }
}
