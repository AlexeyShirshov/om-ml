using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WXML.Model.Descriptors
{
    public interface ISourceProvider
    {
        SourceView GetSourceView(string schemas, string namelike, bool escapeTableNames, bool escapeColumnNames);
        void GenerateCreateScript(IEnumerable<PropertyDefinition> props, StringBuilder script, bool unicodeStrings);
    }
}
