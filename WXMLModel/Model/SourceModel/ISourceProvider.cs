using System.Collections.Generic;
using System.Text;

namespace WXML.Model.Descriptors
{
    public interface ISourceProvider
    {
        SourceView GetSourceView(string schemas, string namelike, bool escapeTableNames, bool escapeColumnNames);
        void GenerateCreateScript(IEnumerable<PropertyDefinition> props, StringBuilder script, bool unicodeStrings);
        void GenerateCreateScript(RelationDefinitionBase rel, StringBuilder script, bool unicodeStrings);
        void GenerateDropConstraintScript(SourceFragmentDefinition table, string constraintName, StringBuilder script);
        void GenerateCreatePKScript(IEnumerable<PropDefinition> pks, string constraintName, StringBuilder script, bool pk, bool clustered);
        void GenerateCreateFKsScript(SourceFragmentDefinition table, IEnumerable<FKDefinition> fks, StringBuilder script);
        void GenerateAddColumnsScript(IEnumerable<PropDefinition> props, StringBuilder script, bool unicodeStrings);
    }

    public class FKDefinition
    {
        public string constraintName;
        public string[] cols;
        public SourceFragmentDefinition refTbl;
        public string[] refCols;
    }

    public class PropDefinition
    {
        public TypeDefinition PropType;
        public SourceFieldDefinition Field;
        public Field2DbRelations Attr;
    }
}
