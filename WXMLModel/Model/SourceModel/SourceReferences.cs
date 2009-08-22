namespace WXML.Model.Descriptors
{
    public struct SourceReferences
    {
        public readonly SourceConstraint PKConstraint;
        public readonly SourceConstraint FKConstraint;

        public readonly SourceFieldDefinition PKField;
        public readonly SourceFieldDefinition FKField;

        public readonly string DeleteAction;

        public SourceReferences(string action,
            SourceConstraint pkConstarint,
            SourceConstraint fkConstarint,
            SourceFieldDefinition pkField,
            SourceFieldDefinition fkField)
        {
            DeleteAction = action;
            PKConstraint = pkConstarint;
            FKConstraint = fkConstarint;
            PKField = pkField;
            FKField = fkField;
        }
    }
}
