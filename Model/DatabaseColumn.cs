using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Linq;

namespace Worm.CodeGen.Core
{
    public class DatabaseConstraint
    {
        private string _constraintType;
        private string _constraintName;

        public DatabaseConstraint(string constraintType, string constraintName)
        {
            _constraintType = constraintType;
            _constraintName = constraintName;
        }

        public string ConstraintType
        {
            get { return _constraintType; }
        }

        public string ConstraintName
        {
            get { return _constraintName; }
        }

    }

    public class DatabaseColumn
    {
        private string _schema;
        private string _table;
        private string _column;

        private bool _isNullable;
        private string _type;
        private bool _identity;
        private int _pkCnt;
        private int? _sz;

        private List<DatabaseConstraint> _constraints = new List<DatabaseConstraint>();

        protected DatabaseColumn()
        {
        }

        public DatabaseColumn(string schema, string table, string column,
            bool isNullable, string type, bool identity, int pkCnt)
        {
            _schema = schema;
            _table = table;
            _column = column;

            _isNullable = isNullable;
            _type = type;
            _identity = identity;

            _pkCnt = pkCnt;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DatabaseColumn);
        }

        public bool Equals(DatabaseColumn obj)
        {
            if (obj == null)
                return false;

            return ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return _schema + _table + _column;
        }

        public override int GetHashCode()
        {
            //System.Diagnostics.Debug.WriteLine(ToString());
            //System.Diagnostics.Debug.WriteLine(ToString().GetHashCode());
            return ToString().GetHashCode();
        }

        public string Schema
        {
            get { return _schema; }
        }

        public string Table
        {
            get { return _table; }
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

        public string FullTableName
        {
            get { return _schema + "." + _table; }
        }

        public int PKCount
        {
            get { return _pkCnt; }
        }

        public List<DatabaseConstraint> Constraints
        {
            get { return _constraints; }
        }

        public bool IsPK
        {
            get
            {
                return _constraints.Find((c) => c.ConstraintType == "PRIMARY KEY") != null;
            }
        }

        public bool IsFK
        {
            get
            {
                return _constraints.Find((c) => c.ConstraintType == "FOREIGN KEY") != null;
            }
        }

        public string FKName
        {
            get
            {
                return _constraints.Find((c) => c.ConstraintType == "FOREIGN KEY").ConstraintName;
            }
        }

        public IEnumerable<DatabaseColumn> GetTableColumns(IEnumerable<DatabaseColumn> columns)
        {
            return from k in columns
                   where k.Table == Table && k.Schema == Schema
                   select k;
        }

        public static DatabaseColumn Create(DbDataReader reader)
        {
            DatabaseColumn c = new DatabaseColumn();

            c._schema = reader.GetString(reader.GetOrdinal("table_schema"));
            c._table = reader.GetString(reader.GetOrdinal("table_name"));
            c._column = reader.GetString(reader.GetOrdinal("column_name"));

            string yn = reader.GetString(reader.GetOrdinal("is_nullable"));
            if (yn == "YES")
            {
                c._isNullable = true;
            }
            c._type = reader.GetString(reader.GetOrdinal("data_type"));

            int ct = reader.GetOrdinal("constraint_type");
            int cn = reader.GetOrdinal("constraint_name");

            if (!reader.IsDBNull(ct))
            {
                c.Constraints.Add(new DatabaseConstraint(reader.GetString(ct), reader.GetString(cn)));
            }

            c._identity = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("identity")));

            c._pkCnt = reader.GetInt32(reader.GetOrdinal("pk_cnt"));

            int sc = reader.GetOrdinal("character_maximum_length");
            if (!reader.IsDBNull(sc))
                c._sz = reader.GetInt32(sc);

            return c;
        }
    }
}
