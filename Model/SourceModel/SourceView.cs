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
                    select c.SourceFragment).Distinct(new EqualityComparer<SourceFragmentDefinition, string>(
                        (item)=>item.Identifier
                   ));
        }

        public IEnumerable<SourceFieldDefinition> GetColumns(SourceFragmentDefinition sf)
        {
            return from c in _columns
                   where c.SourceFragment == sf
                   select c;
        }

        public IEnumerable<SourceFieldDefinition> GetColumns()
        {
            return _columns;
        }

        public SourceFragmentDefinition GetOrCreateTable(string selector, string name)
        {
            SourceFragmentDefinition sf = GetTables().SingleOrDefault(item =>
                item.Selector == selector && item.Name == name);

            if (sf == null)
                sf = new SourceFragmentDefinition(selector + "." + name, name, selector);

            return sf;
        }

        //public IEnumerable<SourceConstraint> GetConstraints(SourceFragmentDefinition sf)
        //{
        //    return sf.Constraints;
        //}

        public IEnumerable<SourceReferences> GetFKRelations(SourceConstraint fkConstraint)
        {
            return from sr in _references
                   where sr.FKConstraint == fkConstraint
                   select sr;
        }
    }
}
