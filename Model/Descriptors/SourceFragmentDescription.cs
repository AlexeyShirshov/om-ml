using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model.Descriptors
{
	public class SourceFragmentDefinition
	{
		public string Identifier { get; private set; }
		public string Name { get; set; }
		public string Selector { get; set; }

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
	}
}
