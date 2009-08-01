using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model.Descriptors
{
    public class RelationDescription : RelationDescriptionBase
    {
        //private readonly LinkTarget _left;
        //private readonly LinkTarget _right;
        //private readonly TableDescription _table;
        //private readonly EntityDescription _underlyingEntity;
        //private bool _disabled;

        public RelationDescription(LinkTarget left, LinkTarget right, SourceFragmentDescription table, EntityDescription underlyingEntity)
            : this(left, right, table, underlyingEntity, false)
        {
        }

        public RelationDescription(LinkTarget left, LinkTarget right, SourceFragmentDescription table, EntityDescription underlyingEntity, bool disabled)
            : base(table, underlyingEntity, left, right, disabled)
        {
            //_left = left;
            //_right = right;
        }

        //public TableDescription Table
        //{
        //    get { return _table; }
        //}

        //public EntityDescription UnderlyingEntity
        //{
        //    get { return _underlyingEntity; }
        //} 

        public new LinkTarget Left
        {
            get { return (LinkTarget)(base.Left); }
        }     

        public new LinkTarget Right
        {
            get { return (LinkTarget)(base.Right); }
        }

        //public bool Disabled
        //{
        //    get { return _disabled; }
        //    set { _disabled = value; }
        //}

        //public static bool IsSimilar(RelationDescription first, RelationDescription second)
        //{
        //    bool yep = false;
        //    yep |= LinkTarget.IsSimilar(first.Left, second.Left) && LinkTarget.IsSimilar(first.Right, second.Right);
        //    yep |= LinkTarget.IsSimilar(first.Left, second.Right) && LinkTarget.IsSimilar(first.Right, second.Left);
        //    return yep;
        //}

		public override bool IsEntityTakePart(EntityDescription entity)
		{
			return Left.Entity == entity || Right.Entity == entity;
		}
	}
}
