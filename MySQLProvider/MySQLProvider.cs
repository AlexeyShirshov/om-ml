using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using WXML.Model.Descriptors;

namespace WXML.Model.Database.Providers
{
    public class MySQLProvider : DatabaseProvider
    {
        public MySQLProvider() :
            this(null, null)
        {

        }

        public MySQLProvider(string server, string db) :
            base(server, db, true, null, null)
        {
        }

        public MySQLProvider(string server, string db, string user, string psw) :
            base(server, db, false, user, psw)
        {
        }

        public override Descriptors.SourceView GetSourceView(string schemas, string namelike, bool escapeTableNames, bool escapeColumnNames)
        {
            SourceView database = new SourceView();
            //List<Pair<string>> defferedCols = new List<Pair<string>>();

            using (DbConnection conn = GetDBConn())
            {
                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select t.table_schema,t.table_name,c.column_name,c.is_nullable,c.data_type,c.column_default,c.character_maximum_length from INFORMATION_SCHEMA.TABLES t
						join INFORMATION_SCHEMA.COLUMNS c on t.table_name = c.table_name and t.table_schema = c.table_schema
						where t.TABLE_TYPE = 'BASE TABLE'
						YYYYY
						XXXXX
						order by t.table_schema,t.table_name,c.ordinal_position";

                    schemas = _db;

                    PrepareCmd(cmd, schemas, namelike, "YYYYY", "XXXXX", true, "t");

                    RaiseOnDatabaseConnecting(conn.ConnectionString);

                    conn.Open();

                    RaiseOnStartLoadDatabase();
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Create(database, reader, escapeTableNames, escapeColumnNames, true);
                        }
                    }
                    RaiseOnEndLoadDatabase();
                }
            }

            return database;
        }

        #region Generate

        public override void GenerateCreateScript(IEnumerable<Descriptors.PropertyDefinition> props, StringBuilder script, bool unicodeStrings)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCreateScript(Descriptors.RelationDefinitionBase rel, StringBuilder script, bool unicodeStrings)
        {
            throw new NotImplementedException();
        }

        public override void GenerateDropConstraintScript(Descriptors.SourceFragmentDefinition table, string constraintName, StringBuilder script)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCreatePKScript(IEnumerable<Descriptors.SourceFieldDefinition> pks, string constraintName, StringBuilder script, bool pk, bool clustered)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCreateFKsScript(Descriptors.SourceFragmentDefinition table, IEnumerable<Descriptors.FKDefinition> fks, StringBuilder script)
        {
            throw new NotImplementedException();
        }

        public override void GenerateAddColumnsScript(IEnumerable<Descriptors.PropDefinition> props, StringBuilder script, bool unicodeStrings)
        {
            throw new NotImplementedException();
        }

        public override void GenerateAddColumnsScript(IEnumerable<Descriptors.SourceFieldDefinition> props, StringBuilder script, bool unicodeStrings)
        {
            throw new NotImplementedException();
        }

        public override void GenerateCreateIndexScript(Descriptors.SourceFragmentDefinition table, Descriptors.IndexDefinition indexes, StringBuilder script)
        {
            throw new NotImplementedException();
        }

        public override void GenerateDropIndexScript(Descriptors.SourceFragmentDefinition table, string indexName, StringBuilder script)
        {
            throw new NotImplementedException();
        }

        public override void GenerateDropTableScript(Descriptors.SourceFragmentDefinition table, StringBuilder script)
        {
            throw new NotImplementedException();
        }

        public override System.Data.Common.DbConnection GetDBConn()
        {
            MySqlConnectionStringBuilder cb = new MySqlConnectionStringBuilder();
            cb.Server = _server;
            if (!string.IsNullOrEmpty(_db))
                cb.Database = _db;

            if (_integratedSecurity)
            {
                cb.IntegratedSecurity = true;
            }
            else
            {
                cb.UserID = _user;
                cb.Password = _psw;
            }
            return new MySqlConnection(cb.ToString());
        }

        protected override string AppendIdentity()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
