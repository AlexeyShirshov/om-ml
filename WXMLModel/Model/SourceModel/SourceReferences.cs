using System;
namespace WXML.Model.Descriptors
{
    [Serializable]
    public struct SourceReferences
    {
        public SourceConstraint PKConstraint;
        public SourceConstraint FKConstraint;

        public SourceFieldDefinition PKField;
        public SourceFieldDefinition FKField;

        public readonly string DeleteAction;

        public SourceReferences(
            SourceConstraint pkConstarint,
            SourceConstraint fkConstarint,
            SourceFieldDefinition pkField,
            SourceFieldDefinition fkField)
            : this(null, pkConstarint, fkConstarint, pkField, fkField)
        {
            
        }

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
