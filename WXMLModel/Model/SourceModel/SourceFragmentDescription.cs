using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace WXML.Model.Descriptors
{
    [Serializable]
    public class SourceFragmentDefinition : IExtensible
	{
        private readonly Dictionary<Extension, XElement> _extensions = new Dictionary<Extension, XElement>();
        
        public string Identifier { get; private set; }
		public string Name { get; set; }
		public string Selector { get; set; }
        private readonly List<SourceConstraint> _constraints = new List<SourceConstraint>();

        //public SourceFragmentDefinition()
        //{
        //}

	    public SourceFragmentDefinition(string id, string name) : this(id, name, null)
		{
		}

		public SourceFragmentDefinition(string id, string name, string selector)
		{
			if (string.IsNullOrEmpty(id))
				throw new ArgumentNullException(nameof(id));
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			Identifier = id;
			Name = name;
			Selector = selector;
		}

        public List<SourceConstraint> Constraints
        {
            get { return _constraints; }
        }

        //public override string ToString()
        //{
        //    return Selector + "." + Name;
        //}

        //public SourceFragmentDefinition Clone()
        //{
        //    return new SourceFragmentDefinition(Identifier, Name, Selector);
        //}

        //object ICloneable.Clone()
        //{
        //    return Clone();
        //}

        public Dictionary<Extension, XElement> Extensions
        {
            get
            {
                return _extensions;
            }
        }

        public XElement GetExtension(string name)
        {
            XElement x;
            if (!_extensions.TryGetValue(new Extension(name), out x))
                x = null;

            return x;
        }
	}
}
