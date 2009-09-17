using System;
using System.Collections.Generic;

namespace WXML.Model.Descriptors
{
    [Serializable]
	public class SourceFragmentDefinition //: ICloneable
	{
		public string Identifier { get; set; }
		public string Name { get; set; }
		public string Selector { get; set; }
        private readonly List<SourceConstraint> _constraints = new List<SourceConstraint>();

        public SourceFragmentDefinition()
        {
        }

	    public SourceFragmentDefinition(string id, string name) : this(id, name, null)
		{
		}

		public SourceFragmentDefinition(string id, string name, string selector)
		{
			if (string.IsNullOrEmpty(id))
				throw new ArgumentNullException("id");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			Identifier = id;
			Name = name;
			Selector = selector;
		}

        public List<SourceConstraint> Constraints
        {
            get { return _constraints; }
        }

        public override string ToString()
        {
            return Selector + "." + Name;
        }

        //public SourceFragmentDefinition Clone()
        //{
        //    return new SourceFragmentDefinition(Identifier, Name, Selector);
        //}

        //object ICloneable.Clone()
        //{
        //    return Clone();
        //}
	}
}
