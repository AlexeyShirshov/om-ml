using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model.Descriptors
{
	public class SourceFragmentRefDescription : SourceFragmentDescription
	{
        public class Condition
        {
            public string LeftColumn { get; protected set; }
            public string RightColumn { get; protected set; }

            public Condition(string leftColumn, string rightColumn)
            {
                this.LeftColumn = leftColumn;
                this.RightColumn = rightColumn;
            }
        }

        public enum JoinTypeEnum
        {
            inner,
            outer
        }

        public SourceFragmentDescription AnchorTable { get; set; }
        public JoinTypeEnum JoinType { get; set; }
        
        public List<Condition> _c = new List<Condition>();
        public List<Condition> Conditions
        {
            get
            {
                return _c;
            }
        }

		public SourceFragmentRefDescription(string id, string name) : base(id, name, null)
		{
		}

        public SourceFragmentRefDescription(string id, string name, string selector)
            : base(id, name, selector)
		{
		}

        public SourceFragmentRefDescription(SourceFragmentDescription sf)
            : base(sf.Identifier, sf.Name, sf.Selector)
        {
        }
	}
}
