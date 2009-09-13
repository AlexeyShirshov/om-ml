using System.Collections.Generic;

namespace WXML.Model.Descriptors
{
    public class SourceConstraint
    {
        private string _constraintType;
        private readonly string _constraintName;

        public const string PrimaryKeyConstraintTypeName = "PRIMARY KEY";
        public const string ForeignKeyConstraintTypeName = "FOREIGN KEY";
        public const string UniqueConstraintTypeName = "UNIQUE";

        public const string CascadeAction = "CASCADE";
        public const string NoAction = "NO ACTION";

        public SourceConstraint(string constraintType, string constraintName)
        {
            _constraintType = constraintType;
            _constraintName = constraintName;
            SourceFields = new List<SourceFieldDefinition>();
        }

        public string ConstraintType
        {
            get { return _constraintType; }
            set { _constraintType = value; }
        }

        public string ConstraintName
        {
            get { return _constraintName; }
        }

        public List<SourceFieldDefinition> SourceFields { get; set; }
    }
}
