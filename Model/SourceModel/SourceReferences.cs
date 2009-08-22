namespace WXML.Model.Descriptors
{
    public struct SourceReferences
    {
        public readonly SourceFieldConstraint PKConstraint;
        public readonly SourceFieldConstraint FKConstraint;

        public readonly SourceFieldDefinition PKField;
        public readonly SourceFieldDefinition FKField;

        public readonly string Action;

        public SourceReferences(string action,
            SourceFieldConstraint pkConstarint,
            SourceFieldConstraint fkConstarint,
            SourceFieldDefinition pkField,
            SourceFieldDefinition fkField)
        {
            Action = action;
            PKConstraint = pkConstarint;
            FKConstraint = fkConstarint;
            PKField = pkField;
            FKField = fkField;
        }
    }
}
