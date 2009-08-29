﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using WXML.Model.Descriptors;
using System.Text;

namespace WXML.Model.Database.Providers
{
    public class MSSQLProvider : DatabaseProvider
    {
        public MSSQLProvider(string server, string db) :
            base(server, db, true, null, null)
        {
        }

        public MSSQLProvider(string server, string db, string user, string psw) :
            base(server, db, false, user, psw)
        {
        }

        public SourceView GetSourceView()
        {
            return GetSourceView(null, null, true, true);
        }

        public SourceView GetSourceView(string schemas, string namelike)
        {
            return GetSourceView(schemas, namelike, true, true);
        }

        public override SourceView GetSourceView(string schemas, string namelike, bool escapeTableNames, bool escapeColumnNames)
        {
            SourceView database = new SourceView();
            //List<Pair<string>> defferedCols = new List<Pair<string>>();

            using (DbConnection conn = GetDBConn())
            {
                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select t.table_schema,t.table_name,c.column_name,c.is_nullable,c.data_type,cc.constraint_type,cc.constraint_name, " + AppendIdentity() + @",c.column_default,c.character_maximum_length from INFORMATION_SCHEMA.TABLES t
						join INFORMATION_SCHEMA.COLUMNS c on t.table_name = c.table_name and t.table_schema = c.table_schema
                        left join (
	                        select cc.table_name,cc.table_schema,cc.column_name,tc.constraint_type,cc.constraint_name from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc 
	                        join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc on tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name --and tc.constraint_type is not null
                        ) cc on t.table_name = cc.table_name and t.table_schema = cc.table_schema and c.column_name = cc.column_name
						where t.TABLE_TYPE = 'BASE TABLE'
						--and (
						--((select count(*) from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
						--join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc on 
						--tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
						--where t.table_name = tc.table_name and t.table_schema = tc.table_schema
						--and tc.constraint_type = 'PRIMARY KEY'
						--) > 0) or 
						--((select count(*) from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
						--join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc on 
						--tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
						--where t.table_name = tc.table_name and t.table_schema = tc.table_schema
						--and tc.constraint_type = 'UNIQUE'
						--) > 0))
						--and (select count(*) from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu 
						--	where ccu.table_name = t.table_name and ccu.table_schema = t.table_schema and ccu.constraint_name = cc.constraint_name) < 2
						--and (tc.constraint_type <> 'CHECK' or tc.constraint_type is null)
						YYYYY
						XXXXX
						order by t.table_schema,t.table_name,c.ordinal_position";

                    PrepareCmd(cmd, schemas, namelike, "t");

                    RaiseOnDatabaseConnecting(conn.ConnectionString);

                    conn.Open();

                    RaiseOnStartLoadDatabase();
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Create(database, reader, escapeTableNames, escapeColumnNames);
                        }
                    }
                    RaiseOnEndLoadDatabase();
                }

                FillReferencedColumns(schemas, namelike, escapeTableNames, escapeColumnNames, database, conn);
            }

            return database;
        }

        private static void PrepareCmd(DbCommand cmd, string schemas, string namelike, params string[] aliases)
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
                    ss.AppendFormat("'{0}',", s);
                }
                ss.Length -= 1;
                foreach (string alias in aliases)
                {
                    yyyyy.AppendLine("and " + alias + string.Format(".table_schema {1}in ({0})", ss.ToString(), r));
                }
            }
            cmd.CommandText = cmd.CommandText.Replace("YYYYY", yyyyy.ToString());

            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(namelike))
            {
                int i = 1;
                foreach (string alias in aliases)
                {
                    foreach (string nl in namelike.Split(','))
                    {
                        DbParameter tn = cmd.CreateParameter();
                        tn.ParameterName = "tn" + i;
                        string r = string.Empty;
                        if (nl.StartsWith("!"))
                        {
                            r = "not ";
                            tn.Value = nl.Substring(1);
                        }
                        else
                            tn.Value = nl;
                        tn.Direction = ParameterDirection.Input;
                        cmd.Parameters.Add(tn);

                        //{2}.table_schema+
                        sb.AppendFormat("and ({2}.table_name {1}like @tn{0})", i, r, alias).AppendLine();
                        i++;
                    }
                }
            }

            cmd.CommandText = cmd.CommandText.Replace("XXXXX", sb.ToString());
        }

        protected override DbConnection GetDBConn()
        {
            System.Data.SqlClient.SqlConnectionStringBuilder cb = new System.Data.SqlClient.SqlConnectionStringBuilder();
            string srv = _server;
            string path = _server;
            string[] ss = _server.Split(';');
            if (ss.Length == 2)
            {
                srv = ss[0];
                path = ss[1];
            }

            if (File.Exists(path))
            {
                if (path == srv)
                    srv = @".\sqlexpress";
                cb.AttachDBFilename = _server;
                cb.UserInstance = true;
            }

            cb.DataSource = srv;
            if (!string.IsNullOrEmpty(_db))
                cb.InitialCatalog = _db;

            if (_integratedSecurity)
            {
                cb.IntegratedSecurity = true;
            }
            else
            {
                cb.UserID = _user;
                cb.Password = _psw;
            }
            return new System.Data.SqlClient.SqlConnection(cb.ConnectionString);
        }

        protected override string AppendIdentity()
        {
            return "columnproperty(object_id(c.table_schema + '.' + c.table_name),c.column_name,'isIdentity') [identity]";
        }

        public static SourceFieldDefinition Create(SourceView db, DbDataReader reader, bool escapeTableNames, bool escapeColumnNames)
        {
            SourceFieldDefinition c = new SourceFieldDefinition();

            string table = reader.GetString(reader.GetOrdinal("table_name"));
            string schema = reader.GetString(reader.GetOrdinal("table_schema"));

            if (escapeTableNames)
            {
                if (!(table.StartsWith("[") || table.EndsWith("]")))
                    table = "[" + table + "]";

                if (!(schema.StartsWith("[") || schema.EndsWith("]")))
                    schema = "[" + schema + "]";
            }

            c.SourceFragment = db.GetOrCreateTable(schema, table);

            c._column = reader.GetString(reader.GetOrdinal("column_name"));
            if (escapeColumnNames && !c._column.StartsWith("[") && !c._column.EndsWith("]"))
                c._column = "[" + c._column + "]";

            if (!db.GetColumns(c.SourceFragment).Any(item => item.SourceFieldExpression == c._column))
            {
                string yn = reader.GetString(reader.GetOrdinal("is_nullable"));
                if (yn == "YES")
                {
                    c.IsNullable = true;
                }
                c.SourceType = reader.GetString(reader.GetOrdinal("data_type"));

                c.IsAutoIncrement = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("identity")));

                int dfo = reader.GetOrdinal("column_default");
                if (!reader.IsDBNull(dfo))
                    c._defaultValue = reader.GetString(dfo);

                int sc = reader.GetOrdinal("character_maximum_length");
                if (!reader.IsDBNull(sc))
                    c.SourceTypeSize = reader.GetInt32(sc);

                db._columns.Add(c);
            }
            else
                c = db.GetColumns(c.SourceFragment).Single(item => item.SourceFieldExpression == c._column);

            int ct = reader.GetOrdinal("constraint_type");
            int cn = reader.GetOrdinal("constraint_name");

            if (!reader.IsDBNull(ct))
            {
                SourceConstraint cns = c.SourceFragment.Constraints
                    .SingleOrDefault(item => item.ConstraintName == reader.GetString(cn));

                if (cns == null)
                {
                    cns = new SourceConstraint(reader.GetString(ct), reader.GetString(cn));
                    c.SourceFragment._constraints.Add(cns);
                }

                cns.SourceFields.Add(c);
            }

            return c;
        }

        public void FillReferencedColumns(string schemas, string namelike,
            bool escapeTableNames, bool escapeColumnNames, SourceView sv, DbConnection conn)
        {
            using (DbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText =
                    @"select cc.TABLE_SCHEMA, cc.TABLE_NAME, cc.COLUMN_NAME, 
                    tc.TABLE_SCHEMA AS fkSchema, tc.TABLE_NAME AS fkTable, cc2.COLUMN_NAME AS fkColumn, 
                    rc.DELETE_RULE, cc.CONSTRAINT_NAME, cc2.CONSTRAINT_NAME AS fkConstraint
					from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc
					join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc on rc.unique_constraint_name = cc.constraint_name
					join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc on tc.constraint_name = rc.constraint_name
					join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc2 on cc2.constraint_name = tc.constraint_name and cc2.table_schema = tc.table_schema and cc2.table_name = tc.table_name
					where tc.constraint_type = 'FOREIGN KEY'
                    YYYYY
                    XXXXX
                ";

                PrepareCmd(cmd, schemas, namelike, "cc", "tc");

                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string pkSchema = reader.GetString(reader.GetOrdinal("TABLE_SCHEMA"));
                        string pkName = reader.GetString(reader.GetOrdinal("TABLE_NAME"));

                        if (escapeTableNames)
                        {
                            if (!(pkName.StartsWith("[") || pkName.EndsWith("]")))
                                pkName = "[" + pkName + "]";

                            if (!(pkSchema.StartsWith("[") || pkSchema.EndsWith("]")))
                                pkSchema = "[" + pkSchema + "]";
                        }

                        SourceFragmentDefinition pkTable = sv.GetTables()
                            .SingleOrDefault(item => item.Selector == pkSchema && item.Name == pkName);

                        if (pkTable == null)
                            throw new InvalidOperationException(string.Format("Table {0}.{1} not found",
                                pkSchema, pkName));

                        string fkSchema = reader.GetString(reader.GetOrdinal("fkSchema"));
                        string fkName = reader.GetString(reader.GetOrdinal("fkTable"));
                        if (escapeTableNames)
                        {
                            if (!(fkName.StartsWith("[") || fkName.EndsWith("]")))
                                fkName = "[" + fkName + "]";

                            if (!(fkSchema.StartsWith("[") || fkSchema.EndsWith("]")))
                                fkSchema = "[" + fkSchema + "]";
                        }

                        SourceFragmentDefinition fkTable = sv.GetTables()
                            .SingleOrDefault(item => item.Selector == fkSchema && item.Name == fkName);

                        if (fkTable == null)
                            throw new InvalidOperationException(string.Format("Table {0}.{1} not found",
                                fkSchema, fkName));

                        string pkCol = reader.GetString(reader.GetOrdinal("COLUMN_NAME"));
                        if (escapeColumnNames && !pkCol.StartsWith("[") && !pkCol.EndsWith("]"))
                            pkCol = "[" + pkCol + "]";

                        string fkCol = reader.GetString(reader.GetOrdinal("fkColumn"));
                        if (escapeColumnNames && !fkCol.StartsWith("[") && !fkCol.EndsWith("]"))
                            fkCol = "[" + fkCol + "]";

                        //if (pkTable.Constraints.Count(item => item.ConstraintName == reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"))) > 1)
                        //    throw new InvalidOperationException(string.Format("Constraint {0} occur {1} times", reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME")),
                        //        pkTable.Constraints.Count(item => item.ConstraintName == reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME")))));

                        //SourceConstraint pkConstarint = pkTable.Constraints.SingleOrDefault(item => item.ConstraintName == reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME")));
                        //if (pkConstarint == null)
                        //    throw new InvalidOperationException(string.Format("Constraint {0} not found", reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"))));

                        //SourceConstraint fkConstarint = fkTable.Constraints.SingleOrDefault(item => item.ConstraintName == reader.GetString(reader.GetOrdinal("fkConstraint")));
                        //if (fkConstarint == null)
                        //    throw new InvalidOperationException(string.Format("Constraint {0} not found", reader.GetString(reader.GetOrdinal("fkConstraint"))));

                        sv._references.Add(new SourceReferences(
                            reader.GetString(reader.GetOrdinal("DELETE_RULE")),
                            pkTable.Constraints.Single(item => item.ConstraintName == reader.GetString(reader.GetOrdinal("CONSTRAINT_NAME"))),
                            fkTable.Constraints.Single(item => item.ConstraintName == reader.GetString(reader.GetOrdinal("fkConstraint"))),
                            sv.GetColumns(pkTable).Single(item => item.SourceFieldExpression == pkCol),
                            sv.GetColumns(fkTable).Single(item => item.SourceFieldExpression == fkCol)
                        ));
                    }
                }
            }
        }

        public override void GenerateCreateScript(IEnumerable<PropertyDefinition> props, StringBuilder script,
            bool unicodeStrings)
        {
            SourceFragmentDefinition sf = props.First().SourceFragment;
            script.AppendFormat("CREATE TABLE {0}.{1}(", sf.Selector, sf.Name);

            foreach (PropertyDefinition prop in props)
            {
                ScalarPropertyDefinition sp = prop as ScalarPropertyDefinition;
                if (sp != null)
                {
                    script.Append(sp.SourceFieldExpression).Append(" ").Append(GetType(sp, unicodeStrings));

                    if (sp.SourceField.IsAutoIncrement)
                        script.Append(" IDENTITY");

                    script.Append(sp.IsNullable ? " NULL" : "NOT NULL");

                    if (!string.IsNullOrEmpty(sp.SourceField.DefaultValue))
                        script.AppendFormat(" DEFAULT({0})", sp.SourceField.DefaultValue);

                    script.Append(", ");
                }
            }

            script.Length -= 2;
            script.AppendLine(");");
            script.AppendLine();
        }

        public static string GetType(ScalarPropertyDefinition prop, bool unicodeStrings)
        {
            string result = prop.SourceType;
            if (string.IsNullOrEmpty(result))
            {
                switch (prop.PropertyType.ClrType.FullName)
                {
                    case "System.Boolean":
                        result = "bit";
                        break;
                    case "System.Byte":
                        result = "tinyint";
                        break;
                    case "System.Int16":
                    case "System.SByte":
                        result = "smallint";
                        break;
                    case "System.Int32":
                    case "System.UInt16":
                        result = "int";
                        break;
                    case "System.Int64":
                    case "System.UInt32":
                        result = "bigint";
                        break;
                    case "System.UInt64":
                        result = "decimal";
                        break;
                    case "System.Decimal":
                        result = "money";
                        break;
                    case "System.Single":
                        result = "real";
                        break;
                    case "System.Double":
                        result = "float";
                        break;
                    case "System.String":
                        result = string.Format(unicodeStrings ? "nvarchar({0})" : "varchar({0})",
                            prop.SourceTypeSize.HasValue ? prop.SourceTypeSize.Value : 50);
                        break;
                    case "System.Char":
                        result = unicodeStrings ? "nchar(1)" : "char(1)";
                        break;
                    case "System.Xml.XmlDocument":
                    case "System.Xml.XmlDocumentFragment":
                    case "System.Xml.Linq.XDocument":
                    case "System.Xml.Linq.XElement":
                        result = "xml";
                        break;
                    case "System.DateTime":
                        result = "datetime";
                        break;
                    case "System.GUID":
                        result = "uniqueidentifier";
                        break;
                    case "System.Char[]":
                        result = string.Format(unicodeStrings ? "nvarchar({0})" : "varchar({0})",
                            prop.SourceTypeSize.HasValue ? prop.SourceTypeSize.Value : 50);
                        break;
                    case "System.Byte[]":
                        if ((prop.Attributes & Field2DbRelations.RV) == Field2DbRelations.RV)
                            result = "rowversion";
                        else
                            result = string.Format("varbinary({0})", prop.SourceTypeSize.HasValue ? prop.SourceTypeSize.Value : 50);

                        break;
                    default:
                        throw new NotSupportedException(prop.PropertyType.ClrType.FullName);
                }
            }
            return result;
        }
    }
}