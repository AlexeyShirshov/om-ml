using System.Collections.Generic;
using System;

namespace WXML.Model.Descriptors
{
    [Serializable]
    public class SourceConstraint
    {
        private string _constraintType;
        private string _constraintName;

        public const string PrimaryKeyConstraintTypeName = "PRIMARY KEY";
        public const string ForeignKeyConstraintTypeName = "FOREIGN KEY";
        public const string UniqueConstraintTypeName = "UNIQUE";

        public const string CascadeAction = "CASCADE";
        public const string NoAction = "NO ACTION";

        public SourceConstraint()
        {
        }

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
            set { _constraintName = value; }
        }

        public List<SourceFieldDefinition> SourceFields { get; set; }
    }
}
