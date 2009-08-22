using System.Collections.Generic;

namespace WXML.Model.Descriptors
{
    public class SourceFieldDefinition
    {
        protected internal SourceFragmentDefinition _tbl;
        protected internal string _column;

        protected internal bool _isNullable;
        protected internal string _type;
        protected internal bool _identity;
        protected internal int? _sz;
        protected internal string _defaultValue;
        internal readonly List<SourceFieldConstraint> _constraints = new List<SourceFieldConstraint>();

        protected internal SourceFieldDefinition()
        {
        }

        public SourceFieldDefinition(SourceFragmentDefinition sf, string column,
            bool isNullable, string type, bool identity, string defaultValue)
        {
            _tbl = sf;
            _column = column;

            _isNullable = isNullable;
            _type = type;
            _identity = identity;
            _defaultValue = defaultValue;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SourceFieldDefinition);
        }

        public bool Equals(SourceFieldDefinition obj)
        {
            if (obj == null)
                return false;

            return ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return _tbl.Selector + _tbl.Name + _column;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public SourceFragmentDefinition SourceFragment
        {
            get { return _tbl; }
        }

        public string ColumnName
        {
            get { return _column; }
        }

        public bool IsAutoIncrement
        {
            get { return _identity; }
        }

        public bool IsNullable
        {
            get { return _isNullable; }
        }

        public int? DbSize
        {
            get { return _sz; }
        }

        public string DbType
        {
            get { return _type; }
        }

        public string DefaultValue
        {
            get { return _defaultValue; }
        }

        public IEnumerable<SourceFieldConstraint> Constraints
        {
            get { return _constraints; }
        }

        public bool IsPK
        {
            get
            {
                return _constraints.Find((c) => c.ConstraintType == SourceFieldConstraint.PrimaryKeyConstraintTypeName) != null;
            }
        }

        public bool IsFK
        {
            get
            {
                return _constraints.Find((c) => c.ConstraintType == SourceFieldConstraint.ForeignKeyConstraintTypeName) != null;
            }
        }

        public string FKName
        {
            get
            {
                return _constraints.Find((c) => c.ConstraintType == SourceFieldConstraint.ForeignKeyConstraintTypeName).ConstraintName;
            }
        }

        public Field2DbRelations GetAttributes()
        {
            Field2DbRelations attrs = Field2DbRelations.None;
            if (IsPK)
            {
                if (!IsAutoIncrement)
                    attrs = Field2DbRelations.PK;
                else
                    attrs = Field2DbRelations.PrimaryKey;
            }
            else
            {
                if (!IsNullable && !string.IsNullOrEmpty(DefaultValue))
                    attrs = Field2DbRelations.InsertDefault | Field2DbRelations.SyncInsert;
                else if (!string.IsNullOrEmpty(DefaultValue))
                    attrs = Field2DbRelations.SyncInsert;
            }

            return attrs;
        }
    }
}
