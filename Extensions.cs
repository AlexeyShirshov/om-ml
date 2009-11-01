using WXML.Model.Descriptors;
namespace WXML2Linq
{
    public static class Extensions
    {
        public static string GetLinqRelationField(this RelationDefinitionBase rel)
        {
            return (string)rel.Items[LinqContextGenerator.LinqRelationField];
        }

        public static string GetLinqRelationFieldDirect(this RelationDefinitionBase rel)
        {
            return (string)rel.Items[LinqContextGenerator.LinqRelationFieldDirect];
        }

        public static string GetLinqRelationFieldReverse(this RelationDefinitionBase rel)
        {
            return (string)rel.Items[LinqContextGenerator.LinqRelationFieldReverse];
        }
    }
}
