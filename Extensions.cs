using WXML.Model.Descriptors;
namespace LinqCodeGenerator
{
    public static class Extensions
    {
        public static string GetLinqRelationField(this RelationDefinitionBase rel)
        {
            return (string)rel.Items[LinqCodeDomGenerator.LinqRelationField];
        }

        public static string GetLinqRelationFieldDirect(this RelationDefinitionBase rel)
        {
            return (string)rel.Items[LinqCodeDomGenerator.LinqRelationFieldDirect];
        }

        public static string GetLinqRelationFieldReverse(this RelationDefinitionBase rel)
        {
            return (string)rel.Items[LinqCodeDomGenerator.LinqRelationFieldReverse];
        }
    }
}
