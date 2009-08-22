using System;
using System.Collections.Generic;
using System.Linq;

namespace WXML.Model.Descriptors
{
    public class SourceView
    {
        internal List<SourceFieldDefinition> _columns = new List<SourceFieldDefinition>();
        internal List<SourceReferences> _references = new List<SourceReferences>();

        public IEnumerable<SourceFragmentDefinition> GetTables()
        {
            return (from c in _columns
                    select c.SourceFragment).Distinct();
        }

        public IEnumerable<SourceFieldDefinition> GetColumns(SourceFragmentDefinition sf)
        {
            return from c in _columns
                   where c.SourceFragment == sf
                   select c;
        }

        public SourceFragmentDefinition GetOrCreateTable(string selector, string name)
        {
            SourceFragmentDefinition sf = GetTables().SingleOrDefault(item =>
                item.Selector == selector && item.Name == name);

            if (sf == null)
                sf = new SourceFragmentDefinition(selector + "." + name, selector, name);

            return sf;
        }

        public IEnumerable<SourceFieldConstraint> GetConstraints(SourceFragmentDefinition sf)
        {
            return (from c in GetColumns(sf)
                    select c).SelectMany(item => item.Constraints);
        }

        public IEnumerable<SourceReferences> GetFKRelations(SourceFieldConstraint fkConstraint)
        {
            return from sr in _references
                   where sr.FKConstraint == fkConstraint
                   select sr;
        }
    }
}
