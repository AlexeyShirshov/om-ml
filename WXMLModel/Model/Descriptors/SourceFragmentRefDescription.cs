using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model.Descriptors
{
	public class SourceFragmentRefDefinition : SourceFragmentDefinition
	{
        public class Condition
        {
            public string LeftColumn { get; protected set; }
            public string RightColumn { get; protected set; }
            public string RightConstant { get; set; }

            public Condition(string leftColumn, string rightColumn)
            {
                LeftColumn = leftColumn;
                RightColumn = rightColumn;
            }

            public Condition(string leftColumn)
            {
                LeftColumn = leftColumn;
            }
        }

        public enum JoinTypeEnum
        {
            inner,
            outer
        }

        public SourceFragmentDefinition AnchorTable { get; set; }
        public JoinTypeEnum JoinType { get; set; }
        public SourceFragmentDefinition Replaces { get; set; }

        public List<Condition> _c = new List<Condition>();
        public List<Condition> Conditions
        {
            get
            {
                return _c;
            }
        }

		public SourceFragmentRefDefinition(string id, string name) : base(id, name, null)
		{
		}

        public SourceFragmentRefDefinition(string id, string name, string selector)
            : base(id, name, selector)
		{
		}

        public SourceFragmentRefDefinition(SourceFragmentDefinition sf)
            : base(sf.Identifier, sf.Name, sf.Selector)
        {
        }
	}
}
