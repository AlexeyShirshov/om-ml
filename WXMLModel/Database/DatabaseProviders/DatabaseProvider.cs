using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using WXML.Model.Descriptors;
using System.Linq;
using System.Data;

namespace WXML.Model.Database.Providers
{
    public abstract class DatabaseProvider : ISourceProvider
    {
        protected string _server;
        protected string _db;
        protected bool _integratedSecurity;
        protected string _user;
        protected string _psw;
        protected string _conn;

        public delegate void DatabaseConnectingDelegate(DatabaseProvider sender, string conn);
        public event DatabaseConnectingDelegate OnDatabaseConnecting;

        public delegate void StartLoadDatabaseDelegate(DatabaseProvider sender, string cmd);
        public event StartLoadDatabaseDelegate OnStartLoadDatabase;

        public event Action OnEndLoadDatabase;

        protected DatabaseProvider(string server, string db, bool integratedSecurity, string user, string psw)
        {
            _server = server;
            _db = db;
            _integratedSecurity = integratedSecurity;
            _user = user;
            _psw = psw;
        }
        protected DatabaseProvider(string conn)
        {
            _conn = conn;
        }
        public abstract SourceView GetSourceView(string schemas, string namelike, bool escapeTableNames, bool escapeColumnNames);
        public abstract void GenerateCreateScript(IEnumerable<PropertyDefinition> props, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateCreateScript(RelationDefinitionBase rel, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateDropConstraintScript(SourceFragmentDefinition table, string constraintName, StringBuilder script);
        public abstract void GenerateCreatePKScript(IEnumerable<SourceFieldDefinition> pks, string constraintName, StringBuilder script, bool pk, bool clustered);
        public abstract void GenerateCreateFKsScript(SourceFragmentDefinition table, IEnumerable<FKDefinition> fks, StringBuilder script);
        public abstract void GenerateAddColumnsScript(IEnumerable<PropDefinition> props, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateAddColumnsScript(IEnumerable<SourceFieldDefinition> props, StringBuilder script, bool unicodeStrings);
        public abstract void GenerateCreateIndexScript(SourceFragmentDefinition table, IndexDefinition indexes, StringBuilder script);
        public abstract void GenerateDropIndexScript(SourceFragmentDefinition table, string indexName, StringBuilder script);
        public abstract void GenerateDropTableScript(SourceFragmentDefinition table, StringBuilder script);

        public virtual bool CaseSensitive
        {
            get
            {
                using (var conn = GetDBConn())
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT 1 WHERE 'SQL' = 'sql'";
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    object r = cmd.ExecuteScalar();
                    return r == DBNull.Value;
                }
            }
        }
        public abstract DbConnection GetDBConn();

        protected abstract string AppendIdentity();
        
        protected void RaiseOnDatabaseConnecting(string conn)
        {
            if (OnDatabaseConnecting != null)
                OnDatabaseConnecting(this, conn);
        }

        protected void RaiseOnStartLoadDatabase(string cmd)
        {
            if (OnStartLoadDatabase != null)
                OnStartLoadDatabase(this, cmd);
        }

        protected void RaiseOnEndLoadDatabase()
        {
            if (OnEndLoadDatabase != null)
                OnEndLoadDatabase();
        }

        #region ISourceProvider Members


        public void GenerateCreatePKScript(IEnumerable<PropDefinition> pks, string constraintName, StringBuilder script, bool pk, bool clustered)
        {
            GenerateCreatePKScript(pks.Select((item)=>item.Field), constraintName, script, pk, clustered);
        }

        #endregion

        public static void PrepareCmd(DbCommand cmd, string schemas, string namelike,
            string schemaReplace, string tableReplace, bool addParams, params string[] aliases)
        {
            StringBuilder yyyyy = new StringBuilder();
            if (!string.IsNullOrEmpty(schemas))
            {
                string r = string.Empty;
                if (schemas.StartsWith("(") && schemas.EndsWith(")"))
                {
                    schemas = schemas.Trim('(', ')');
                    r = "not ";
                }
                StringBuilder ss = new StringBuilder();
                foreach (string s in schemas.Split(','))
                {
                    ss.AppendFormat("'{0}',", s.Trim());
                }
                ss.Length -= 1;
                foreach (string alias in aliases)
                {
                    yyyyy.AppendLine("and " + alias + string.Format(".table_schema {1}in ({0})", ss.ToString(), r));
                }
            }
            cmd.CommandText = cmd.CommandText.Replace(schemaReplace, yyyyy.ToString());

            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(namelike))
            {
                int startNum = 1;
                string r = string.Empty;
                string cond = "or";
                if (namelike.StartsWith("(") && namelike.EndsWith(")"))
                {
                    namelike = namelike.Trim('(', ')');
                    r = "not ";
                    cond = "and";
                }

                foreach (string alias in aliases)
                {
                    sb.Append("and (");
                    foreach (string nl in namelike.Split(','))
                    {
                        if (addParams)
                        {
                            DbParameter tn = cmd.CreateParameter();
                            tn.ParameterName = "tn" + startNum;
                            tn.Value = nl.Trim();
                            tn.Direction = ParameterDirection.Input;
                            cmd.Parameters.Add(tn);
                        }
                        //{2}.table_schema+
                        sb.AppendFormat("{2}.table_name {1}like @tn{0} {3}", startNum, r, alias, cond).AppendLine();
                        startNum++;
                    }
                    sb.Length -= cond.Length + 3;
                    sb.Append(")");
                }
            }

            cmd.CommandText = cmd.CommandText.Replace(tableReplace, sb.ToString());

            //return startNum;
        }

        public static SourceFieldDefinition Create(SourceView db, DbDataReader reader, bool escapeTableNames, bool escapeColumnNames, bool skipSchema)
        {
            SourceFieldDefinition c = new SourceFieldDefinition();

            string table = reader.GetString(reader.GetOrdinal("table_name"));
            string schema = null;
            if (!skipSchema && !reader.IsDBNull(reader.GetOrdinal("table_schema")))
            {
                schema = reader.GetString(reader.GetOrdinal("table_schema"));

                if (escapeTableNames)
                {
                    if (!(table.StartsWith("[") || table.EndsWith("]")))
                        table = "[" + table + "]";

                    if (!(schema.StartsWith("[") || schema.EndsWith("]")))
                        schema = "[" + schema + "]";
                }
            }

            c.SourceFragment = db.GetOrCreateSourceFragment(schema, table);

            c._column = reader.GetString(reader.GetOrdinal("column_name"));
            if (escapeColumnNames && !c._column.StartsWith("[") && !c._column.EndsWith("]"))
                c._column = "[" + c._column + "]";

            if (!db.GetSourceFields(c.SourceFragment).Any(item => item.SourceFieldExpression == c._column))
            {
                string yn = reader.GetString(reader.GetOrdinal("is_nullable"));
                if (yn == "YES")
                {
                    c.IsNullable = true;
                }
                else
                    c.IsNullable = false;

                c.SourceType = reader.GetString(reader.GetOrdinal("data_type"));

                try
                {
                    c.IsAutoIncrement = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("identity")));
                }
                catch
                { }

                int dfo = reader.GetOrdinal("column_default");
                if (!reader.IsDBNull(dfo))
                    c._defaultValue = reader.GetString(dfo);

                if (!new[] { "ntext", "text", "image" }.Any(item => item == c.SourceType.ToLower()))
                {
                    int sc = reader.GetOrdinal("character_maximum_length");
                    if (!reader.IsDBNull(sc))
                        c.SourceTypeSize = reader.GetInt32(sc);
                }

                db.SourceFields.Add(c);
            }
            else
                c = db.GetSourceFields(c.SourceFragment).Single(item => item.SourceFieldExpression == c._column);

            try
            {
                int ct = reader.GetOrdinal("constraint_type");
                int cn = reader.GetOrdinal("constraint_name");

                if (!reader.IsDBNull(ct))
                {
                    SourceConstraint cns = c.SourceFragment.Constraints
                        .SingleOrDefault(item => item.ConstraintName == reader.GetString(cn));

                    if (cns == null)
                    {
                        cns = new SourceConstraint(reader.GetString(ct), reader.GetString(cn));
                        c.SourceFragment.Constraints.Add(cns);
                    }

                    cns.SourceFields.Add(c);
                }
            }
            catch { }

            return c;
        }

    }
}
