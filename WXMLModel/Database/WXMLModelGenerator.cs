using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using WXML.Model.Database;
using WXML.Model.Descriptors;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using WXML.Model;
using WXML.CodeDom;

namespace WXML.DatabaseConnector
{
    public class Pair<T>
    {
        internal T _first;
        internal T _second;

        public Pair()
        {
        }

        public Pair(T first, T second)
        {
            this._first = first;
            this._second = second;
        }

        public T First
        {
            get { return _first; }
            set { _first = value; }
        }

        public T Second
        {
            get { return _second; }
            set { _second = value; }
        }
    }

    public class Pair<T, T2>
    {
        internal T _first;
        internal T2 _second;

        public Pair()
        {
        }

        public Pair(T first, T2 second)
        {
            this._first = first;
            this._second = second;
        }

        public T First
        {
            get { return _first; }
            set { _first = value; }
        }

        public T2 Second
        {
            get { return _second; }
            set { _second = value; }
        }
    }

    public enum relation1to1
    {
        Default,
        Unify,
        Hierarchy
    }

    public class WXMLModelGenerator
    {
        private readonly SourceView _db;
        private readonly WXMLModel _model;
        private readonly bool _transform;
        private HashSet<string> _ents;

        public WXMLModelGenerator(SourceView db, WXMLModel model, bool transform)
        {
            _db = db;
            _model = model;
            _transform = transform;
        }

        public void MergeModelWithDatabase(bool dr, relation1to1 rb, bool escape)
        {
            _ents = new HashSet<string>();
            List<Pair<SourceFieldDefinition, PropertyDefinition>> notFound = new List<Pair<SourceFieldDefinition, PropertyDefinition>>();
            List<SourceFragmentDefinition> tables2skip = new List<SourceFragmentDefinition>();
            foreach (SourceFragmentDefinition sf in _db.GetTables())
            {
                if (tables2skip.Contains(sf))
                    continue;

                if (_db.GetConstraints(sf).Count(item=>item.ConstraintType == SourceFieldConstraint.ForeignKeyConstraintTypeName) == 2 &&
                    _db.GetColumns(sf).All(clm => clm.IsPK && clm.IsFK))
                    continue;

                GetEntity(sf, rb, tables2skip, escape);

                bool ent, col;
                EntityDefinition e = GetEntity(sf, out ent, escape);
                Pair<DatabaseColumn, PropertyDefinition> p = null;
                PropertyDefinition pd = AppendColumn(columns, c, e, out col, escape, (clm)=>p = new Pair<DatabaseColumn,PropertyDefinition>(clm, null), defferedCols, rb);
                if (p != null)
                {
                    p.Second = pd;
                    notFound.Add(p);
                }
                if (ent)
                {
                    Console.WriteLine("Create class {0} ({1})", e.Name, e.Identifier);
                    _ents.Add(e.Identifier);
                }
                else if (col)
                {
                    if (!_ents.ContainsKey(e.Identifier))
                    {
                        Console.WriteLine("Alter class {0} ({1})", e.Name, e.Identifier);
                        _ents.Add(e.Identifier, null);
                    }
                    Console.WriteLine("\tAdd property: " + pd.Name);
                }
            }

            Dictionary<string, EntityDefinition> dic = Process1to1Relations(columns, defferedCols, odef, escape, notFound, rb);

            ProcessM2M(columns, odef, escape, dic);

            if (dropColumns)
            {
                foreach (EntityDefinition ed in odef.GetEntities())
                {
                    List<PropertyDefinition> col2remove = new List<PropertyDefinition>();
                    foreach (PropertyDefinition pd in ed.GetProperties())
                    {
                        string[] ss = ed.GetSourceFragments().First().Name.Split('.');
                        DatabaseColumn c = new DatabaseColumn(ss[0].Trim(new char[] { '[', ']' }), ss[1].Trim(new char[] { '[', ']' }),
                            pd.FieldName.Trim(new char[] { '[', ']' }), false, null, false, 1);
                        if (!columns.ContainsKey(c))
                        {
                            col2remove.Add(pd);
                        }
                    }
                    foreach (PropertyDefinition pd in col2remove)
                    {
                        ed.RemoveProperty(pd);
                        Console.WriteLine("Remove: {0}.{1}", ed.Name, pd.Name);
                    }
                }
            }

            foreach (EntityDefinition e in odef.GetEntities())
            {
                //if (e.HasSinglePk)
                {
                    foreach (EntityDefinition oe in
                        from k in odef.GetActiveEntities()
                        where k != e &&
                            e.EntityRelations.Count(er => !er.Disabled && er.Entity.Identifier == k.Identifier) == 0
                        select k)
                    {
                        IEnumerable<PropertyDefinition> entityProps = oe.GetActiveProperties()
                            .Where(l => l.PropertyType.IsEntityType && l.PropertyType.Entity.Identifier == e.Identifier);
                        int idx = 1;
                        foreach (PropertyDefinition pd in entityProps)
                        {
                            int cnt = odef.ActiveRelations.OfType<RelationDefinition>().Count(r =>
                                (r.Left.Entity.Identifier == oe.Identifier && r.Right.Entity.Identifier == e.Identifier) ||
                                (r.Left.Entity.Identifier == e.Identifier && r.Right.Entity.Identifier == oe.Identifier));

                            string accName = null; string prop = null;
                            if (entityProps.Count() > 1 || cnt > 0)
                            {
                                accName = WXMLCodeDomGeneratorNameHelper.GetMultipleForm(oe.Name + idx.ToString());
                                prop = pd.Name;

                                //foreach (var erd in from k in col
                                //                    where string.IsNullOrEmpty(k.PropertyAlias)
                                //                    select k)
                                //{
                                //    PropertyDescription erdProperty = erd.Property;
                                //    erd.PropertyAlias = erdProperty.PropertyAlias;
                                //    erd.AccessorName = erdProperty.PropertyName;
                                //}
                            }

                            e.EntityRelations.Add(new EntityRelationDefinition()
                            {
                                Entity = oe,
                                SourceEntity = e,
                                AccessorName = accName,
                                PropertyAlias = prop,
                            });
                            idx++;
                        }
                    }
                }
            }

            
        }

        private EntityDefinition GetEntity(SourceFragmentDefinition sf, relation1to1 rb, 
            List<SourceFragmentDefinition> tables2skip, bool escape)
        {
            try
            {
                EntityDefinition masterEntity = null;
                SourceFragmentDefinition masterTable = null;
                List<SourceFragmentRefDefinition.Condition> conds = null;
                if (rb != relation1to1.Default && _db.GetColumns(sf)
                    .Where(item=>item.IsPK)
                    .SelectMany(item=>item.Constraints)
                    .Count(item=>item.ConstraintType == SourceFieldConstraint.ForeignKeyConstraintTypeName) == 1)
                {
                    switch (rb)
                    {
                        case relation1to1.Unify:
                        case relation1to1.Hierarchy:
                            masterTable = GetMasterTable(sf, out conds);
                            masterEntity = GetEntity(masterTable, rb, tables2skip, escape);
                            break;
                        default:
                            throw new NotSupportedException(rb.ToString());
                    }
                }

                bool entCreated;
                EntityDefinition e = GetEntity(sf, out entCreated, escape);
                
                foreach (SourceFieldDefinition field in _db.GetColumns(sf)
                    .Where(item=>!item.IsFK))
                {
                    bool fieldCreated;
                    AppendColumn(e, field, escape, out fieldCreated);
                }

                foreach (SourceFieldDefinition field in _db.GetColumns(sf)
                    .Where(item=>item.IsFK))
                {
                    bool fieldCreated;
                    AppendFKColumn(e, field, escape, out fieldCreated);
                }

                if (masterEntity != null)
                {
                    SourceFragmentRefDefinition sfr = null;
                    switch (rb)
                    {
                        case relation1to1.Unify:
                            sfr = masterEntity.GetSourceFragments()
                                .Single(item=>item.Identifier == masterTable.Identifier);
                            sfr.AnchorTable = sf;
                            sfr.JoinType = SourceFragmentRefDefinition.JoinTypeEnum.outer;
                            sfr.Conditions.AddRange(conds);
                            masterEntity.AddSourceFragment(new SourceFragmentRefDefinition(sf));

                            foreach (PropertyDefinition property in e.GetProperties()
                                .Where(item=>!item.HasAttribute(Field2DbRelations.PK)))
                            {
                                masterEntity.AddProperty(property);
                            }

                            break;
                        case relation1to1.Hierarchy:
                            sfr = e.GetSourceFragments().Single();
                            sfr.AnchorTable = masterTable;
                            sfr.JoinType = SourceFragmentRefDefinition.JoinTypeEnum.inner;
                            foreach (SourceFragmentRefDefinition.Condition cond in conds)
                            {
                                sfr.Conditions.Add(new SourceFragmentRefDefinition.Condition(cond.RightColumn, cond.LeftColumn));
                            }

                            e.BaseEntity = masterEntity;
                            e.InheritsBaseTables = true;

                            break;
                    }
                }
            }
            finally
            {
                tables2skip.Add(sf);
            }
        }

        private SourceFragmentDefinition GetMasterTable(SourceFragmentDefinition sf, 
            out List<SourceFragmentRefDefinition.Condition> conditions)
        {
            SourceFieldConstraint fk = _db.GetColumns(sf)
                .Where(item => item.IsPK)
                .SelectMany(item => item.Constraints)
                .Single();

            SourceFragmentDefinition m = null;
            conditions = new List<SourceFragmentRefDefinition.Condition>();
            foreach (SourceReferences rel in _db.GetFKRelations(fk))
            {
                if (m == null)
                    m = rel.PKField.SourceFragment;

                conditions.Add(new SourceFragmentRefDefinition.Condition(
                    rel.PKField.ColumnName, rel.FKField.ColumnName
                ));
            }
            
            return m;
        }

        protected void ProcessM2M(Dictionary<DatabaseColumn, DatabaseColumn> columns, WXMLModel odef, bool escape,
            Dictionary<string, EntityDefinition> dic)
        {
            List<Pair<string>> tables = new List<Pair<string>>();

            using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
            {
                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select table_schema,table_name from INFORMATION_SCHEMA.TABLE_CONSTRAINTS
						where constraint_type = 'FOREIGN KEY'
						group by table_schema,table_name
						having count(*) = 2";
                    conn.Open();

                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(new Pair<string>(reader.GetString(reader.GetOrdinal("table_schema")),
                                reader.GetString(reader.GetOrdinal("table_name"))));
                        }
                    }
                }
            }

            foreach (Pair<string> p in tables)
            {
                string underlying = GetEntityName(p.First, p.Second);
                EntityDefinition ued = odef.GetEntity(underlying);
                using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
                {
                    using (DbCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = @"select cc.table_schema,cc.table_name,cc2.column_name,rc.delete_rule
						from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc
						join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc on rc.unique_constraint_name = cc.constraint_name
						join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc on tc.constraint_name = rc.constraint_name
						join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc2 on cc2.constraint_name = tc.constraint_name and cc2.table_schema = tc.table_schema and cc2.table_name = tc.table_name
						where tc.table_name = @tbl and tc.table_schema = @schema
						and tc.constraint_type = 'FOREIGN KEY'";

                        DbParameter tbl = cmd.CreateParameter();
                        tbl.ParameterName = "tbl";
                        tbl.Value = p.Second;
                        cmd.Parameters.Add(tbl);

                        DbParameter schema = cmd.CreateParameter();
                        schema.ParameterName = "schema";
                        schema.Value = p.First;
                        cmd.Parameters.Add(schema);

                        conn.Open();

                        List<LinkTarget> targets = new List<LinkTarget>();
                        using (DbDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                //string ename = reader.GetString(reader.GetOrdinal("table_schema")) + "." +
                                //    reader.GetString(reader.GetOrdinal("table_name"));
                                bool deleteCascade = false;
                                switch (reader.GetString(reader.GetOrdinal("delete_rule")))
                                {
                                    case "NO ACTION":
                                        break;
                                    case "CASCADE":
                                        deleteCascade = true;
                                        break;
                                    default:
                                        throw new NotSupportedException("Cascade " + reader.GetString(reader.GetOrdinal("delete_rule")) + " is not supported");
                                }
                                bool c;
                                LinkTarget lt = new LinkTarget(
                                    GetEntity(odef,
                                        reader.GetString(reader.GetOrdinal("table_schema")),
                                        reader.GetString(reader.GetOrdinal("table_name")), out c, escape),
                                        reader.GetString(reader.GetOrdinal("column_name")), deleteCascade);
                                if (c)
                                {
                                    EntityDefinition e = lt.Entity;
                                    odef.RemoveEntity(e);
                                    lt.Entity = dic[e.Identifier];
                                }
                                targets.Add(lt);
                            }
                        }

                        if (targets.Count != 2)
                            continue;

                        if (targets[0].Entity.Name == targets[1].Entity.Name)
                        {
                            LinkTarget t = targets[0];
                            SelfRelationDescription newRel = new SelfRelationDescription(t.Entity, targets[0], targets[1], GetSourceFragment(odef, p.First, p.Second, escape), ued);
                            if (odef.GetSimilarRelation(newRel) == null)
                            {
                                string postFix = string.Empty;
                                if (string.IsNullOrEmpty(newRel.Left.AccessorName))
                                {
                                    if (newRel.Left.FieldName.EndsWith("_id", StringComparison.InvariantCultureIgnoreCase))
                                        newRel.Left.AccessorName = newRel.Left.FieldName.Substring(0, newRel.Left.FieldName.Length - 3);
                                    else if (newRel.Left.FieldName.EndsWith("id", StringComparison.InvariantCultureIgnoreCase))
                                        newRel.Left.AccessorName = newRel.Left.FieldName.Substring(0, newRel.Left.FieldName.Length - 2);
                                    else
                                    {
                                        newRel.Left.AccessorName = newRel.Entity.Name;
                                        postFix = "1";
                                    }

                                    //if (odef.ActiveRelations.OfType<SelfRelationDescription>()
                                    //    .Count(r => r.Entity.Identifier == newRel.Entity.Identifier &&
                                    //    r.Left.AccessorName == newRel.Left.AccessorName) > 0 ||
                                    //    odef.ActiveRelations.OfType<RelationDescription>()
                                    //    .Count(r => (r.Right.Entity.Identifier == newRel.Entity.Identifier &&
                                    //    r.Left.AccessorName == newRel.Left.AccessorName) ||
                                    //    (r.Left.Entity.Identifier == newRel.Entity.Identifier &&
                                    //    r.Right.AccessorName == newRel.Left.AccessorName)) > 0)

                                    //    newRel.Left.AccessorName = newRel.SourceFragment.Identifier + newRel.Left.AccessorName;
                                }

                                if (string.IsNullOrEmpty(newRel.Right.AccessorName))
                                {
                                    if (newRel.Right.FieldName.EndsWith("_id", StringComparison.InvariantCultureIgnoreCase))
                                        newRel.Right.AccessorName = newRel.Right.FieldName.Substring(0, newRel.Right.FieldName.Length - 3);
                                    else if (newRel.Right.FieldName.EndsWith("id", StringComparison.InvariantCultureIgnoreCase))
                                        newRel.Right.AccessorName = newRel.Right.FieldName.Substring(0, newRel.Right.FieldName.Length - 2);
                                    else
                                        newRel.Right.AccessorName = newRel.Entity.Name + postFix;

                                    //if (odef.ActiveRelations.OfType<SelfRelationDescription>()
                                    //    .Count(r => r.Entity.Identifier == newRel.Entity.Identifier &&
                                    //    r.Left.AccessorName == newRel.Right.AccessorName) > 0 ||
                                    //    odef.ActiveRelations.OfType<RelationDescription>()
                                    //    .Count(r => (r.Right.Entity.Identifier == newRel.Entity.Identifier &&
                                    //    r.Left.AccessorName == newRel.Right.AccessorName) ||
                                    //    (r.Left.Entity.Identifier == newRel.Entity.Identifier &&
                                    //    r.Right.AccessorName == newRel.Right.AccessorName)) > 0)

                                    //    newRel.Right.AccessorName = newRel.SourceFragment.Identifier + newRel.Right.AccessorName;
                                }
                                odef.Relations.Add(newRel);
                            }
                        }
                        else
                        {
                            RelationDefinition newRel = new RelationDefinition(targets[0], targets[1], GetSourceFragment(odef, p.First, p.Second, escape), ued);
                            if (!odef.Relations.OfType<RelationDefinition>().Any(m => m.Equals(newRel)))
                            {
                                if (odef.HasSimilarRelationM2M(newRel))
                                {
                                    if (string.IsNullOrEmpty(newRel.Left.AccessorName) ||
                                        string.IsNullOrEmpty(newRel.Right.AccessorName))
                                    {
                                        var lst = from r in odef.Relations.OfType<RelationDefinition>()
                                                  where
                                                    !ReferenceEquals(r.Left, newRel.Left) &&
                                                    !ReferenceEquals(r.Right, newRel.Right) &&
                                                    (
                                                        ((r.Left.Entity == newRel.Left.Entity && string.IsNullOrEmpty(r.Right.AccessorName))
                                                            && (r.Right.Entity == newRel.Right.Entity && string.IsNullOrEmpty(r.Left.AccessorName))) ||
                                                        ((r.Left.Entity == newRel.Right.Entity && string.IsNullOrEmpty(r.Right.AccessorName))
                                                            && (r.Right.Entity == newRel.Left.Entity && string.IsNullOrEmpty(r.Left.AccessorName)))
                                                    )
                                                  select r;

                                        if (lst.Count() > 0)
                                        {
                                            foreach (RelationDefinition r in lst)
                                            {
                                                if (string.IsNullOrEmpty(r.Left.AccessorName))
                                                    r.Left.AccessorName = r.SourceFragment.Name.TrimEnd(']').TrimStart('[') + r.Right.Entity.Name;
                                                if (string.IsNullOrEmpty(r.Right.AccessorName))
                                                    r.Right.AccessorName = r.SourceFragment.Name.TrimEnd(']').TrimStart('[') + r.Left.Entity.Name;
                                            }

                                            if (string.IsNullOrEmpty(newRel.Left.AccessorName))
                                                newRel.Left.AccessorName = newRel.SourceFragment.Name.TrimEnd(']').TrimStart('[') + newRel.Right.Entity.Name;
                                            if (string.IsNullOrEmpty(newRel.Right.AccessorName))
                                                newRel.Right.AccessorName = newRel.SourceFragment.Name.TrimEnd(']').TrimStart('[') + newRel.Left.Entity.Name;
                                        }
                                    }
                                }
                                odef.Relations.Add(newRel);
                            }
                        }
                    }
                }
            }

            foreach (SelfRelationDescription rdb in odef.ActiveRelations.OfType<SelfRelationDescription>())
            {
                NormalizeRelationAccessors(odef, rdb, rdb.Right, rdb.Entity);
                NormalizeRelationAccessors(odef, rdb, rdb.Left, rdb.Entity);
            }

            foreach (RelationDefinition rdb in odef.ActiveRelations.OfType<RelationDefinition>())
            {
                NormalizeRelationAccessors(odef, rdb, rdb.Right, rdb.Right.Entity);
                NormalizeRelationAccessors(odef, rdb, rdb.Left, rdb.Left.Entity);
            }
        }

        private static void NormalizeRelationAccessors(WXMLModel odef, RelationDefinitionBase rdb,
            SelfRelationTarget rdbRight, EntityDefinition rdbEntity)
        {
            var q1 =
                from r in odef.ActiveRelations.OfType<SelfRelationDescription>()
                where r != rdb && r.Entity.Identifier == rdbEntity.Identifier &&
                    (r.Left.AccessorName == rdbRight.AccessorName || r.Right.AccessorName == rdbRight.AccessorName)
                select r as RelationDefinitionBase;

            var q2 =
                from r in odef.ActiveRelations.OfType<RelationDefinition>()
                where r != rdb &&
                    (r.Right.Entity.Identifier == rdbEntity.Identifier &&
                        r.Left.AccessorName == rdbRight.AccessorName) ||
                    (r.Left.Entity.Identifier == rdbEntity.Identifier &&
                        r.Right.AccessorName == rdbRight.AccessorName)
                select r as RelationDefinitionBase;

            int i = 0;
            foreach (RelationDefinitionBase r in q1.Union(q2))
            {
                i++;
                RelationDefinition rd = r as RelationDefinition;
                SelfRelationDescription srd = r as SelfRelationDescription;

                if (srd != null)
                    if (srd.Left.AccessorName == rdbRight.AccessorName)
                        srd.Left.AccessorName = srd.Left.AccessorName + i.ToString();
                    else if (srd.Right.AccessorName == rdbRight.AccessorName)
                        srd.Right.AccessorName = srd.Right.AccessorName + i.ToString();
                    else
                        if (rd.Left.AccessorName == rdbRight.AccessorName)
                        {
                            rd.Left.AccessorName = rd.Left.AccessorName + i.ToString();
                        }
                        else if (rd.Right.AccessorName == rdbRight.AccessorName)
                        {
                            rd.Right.AccessorName = rd.Right.AccessorName + i.ToString();
                        }
            }
        }

        protected PropertyDefinition AppendFKColumn(EntityDefinition e, SourceFieldDefinition c,
            bool escape, out bool created)
        {
            created = false;
            PropertyDefinition pk = null;
            PropertyDefinition pe = null;
            var props = e.SelfProperties.Where(pd =>
                pd.FieldName == c.ColumnName || pd.FieldName.TrimEnd(']').TrimStart('[') == c.ColumnName
            );

            if (props.Count() > 1)
            {
                pe = props.Single(item => item.PropertyType.IsEntityType);
                pk = props.Single(item => !item.PropertyType.IsEntityType);
            }
            else if (props.Count() == 1)
                pe = props.First();

            if (pe == null)
            {
                Field2DbRelations attrs = c.GetAttributes();
                string name = Trim(Capitalize(c.ColumnName));

                SourceFragmentDefinition sfd = GetSourceFragment(c.SourceFragment, escape);

                pe = new PropertyDefinition(name,
                     name, attrs, "Auto generated from column " + c.ColumnName, 
                     GetRelatedType(c, escape), c.ColumnName,
                     sfd, AccessLevel.Private, AccessLevel.Public)
                {
                    DbTypeName = c.DbType,
                    DbTypeNullable = c.IsNullable,
                    DbTypeSize = c.DbSize
                };

                e.AddProperty(pe);
                created = true;

                if ((attrs & Field2DbRelations.PK) == Field2DbRelations.PK)
                {
                    if (!pe.PropertyType.Entity.IsAssignableFrom(e))
                    {
                        attrs = Field2DbRelations.ReadOnly | Field2DbRelations.SyncInsert;
                        string propName = pe.PropertyType.Entity.Name;
                        int cnt = e.SelfProperties.Count(p => !p.Disabled && p.Name == propName);
                        if (cnt > 0)
                            propName = propName + cnt;

                        pe = new PropertyDefinition(propName,
                            propName, attrs, pe.Description, 
                            GetClrType(c.DbType, c.IsNullable), c.ColumnName,
                            sfd, AccessLevel.Private, AccessLevel.Public)
                        {
                            DbTypeName = c.DbType,
                            DbTypeNullable = c.IsNullable,
                            DbTypeSize = c.DbSize
                        };

                        e.AddProperty(pe);
                    }
                }
            }
            else
            {
                Field2DbRelations attrs = c.GetAttributes();

                pe.Attributes |= attrs; 
                if (pk != null)
                {
                    pk.Attributes |= attrs;
                    pk.PropertyType = GetClrType(c.DbType, c.IsNullable);
                }

                pe.DbTypeName = c.DbType;
                pe.DbTypeNullable = c.IsNullable;
                pe.DbTypeSize = c.DbSize;
            }
            return pe;
        }

        protected PropertyDefinition AppendColumn(EntityDefinition e, SourceFieldDefinition c,
            bool escape, out bool created)
        {
            created = false;
            PropertyDefinition pe = e.SelfProperties.SingleOrDefault(pd =>
                pd.FieldName == c.ColumnName || pd.FieldName.TrimEnd(']').TrimStart('[') == c.ColumnName
            );

            if (pe == null)
            {
                Field2DbRelations attrs = c.GetAttributes();
                string name = Trim(Capitalize(c.ColumnName));

                SourceFragmentDefinition sfd = GetSourceFragment(c.SourceFragment, escape);

                pe = new PropertyDefinition(name,
                     name, attrs, "Auto generated from column " + c.ColumnName, 
                     GetClrType(c.DbType, c.IsNullable), c.ColumnName,
                     sfd, AccessLevel.Private, AccessLevel.Public)
                {
                    DbTypeName = c.DbType,
                    DbTypeNullable = c.IsNullable,
                    DbTypeSize = c.DbSize
                };

                e.AddProperty(pe);
                created = true;
            }
            else
            {
                Field2DbRelations attrs = c.GetAttributes();

                pe.Attributes = attrs; 

                if (!pe.PropertyType.IsUserType && (attrs & Field2DbRelations.PK) != Field2DbRelations.PK)
                    pe.PropertyType = GetClrType(c.DbType, c.IsNullable);

                pe.DbTypeName = c.DbType;
                pe.DbTypeNullable = c.IsNullable;
                pe.DbTypeSize = c.DbSize;
            }
            return pe;
        }

        private string Trim(string columnName)
        {
            columnName = columnName.Replace(' ', '_');
            if (_transform)
            {
                if (columnName.EndsWith("_id"))
                    columnName = columnName.Substring(0, columnName.Length - 3);
                else if (columnName.EndsWith("_dt"))
                    columnName = columnName.Substring(0, columnName.Length - 3);

                Regex re = new Regex(@"_(\w)");
                columnName = re.Replace(columnName, new MatchEvaluator(m => m.Groups[1].Value.ToUpper()));

                re = new Regex(@"(\w)-(\w)");
                columnName = re.Replace(columnName, new MatchEvaluator(m => m.Groups[1] + m.Groups[2].Value.ToUpper()));
            }
            return columnName;
        }

        protected Dictionary<string, EntityDefinition> Process1to1Relations(Dictionary<DatabaseColumn, DatabaseColumn> columns,
            List<Pair<string>> defferedCols, WXMLModel odef, bool escape,
            List<Pair<DatabaseColumn, PropertyDefinition>> notFound, relation1to1 rb)
        {
            List<Pair<string>> defferedCols2 = new List<Pair<string>>();
            Dictionary<string, EntityDefinition> dic = new Dictionary<string, EntityDefinition>();
            do
            {
                foreach (Pair<string> p in defferedCols)
                {
                    string columnName = null;
                    TypeDefinition td = GetRelatedType(p.Second, columns, odef, escape, defferedCols, ref columnName);
                    if (td == null)
                    {
                        defferedCols2.Add(p);
                        continue;
                    }

                    if (td.Entity != null)
                    {
                        EntityDefinition ed = td.Entity;
                        string[] ss = p.First.Split('.');
                        PropertyDefinition pd = AppendColumns(columns, ed, ss[0], ss[1], p.Second, escape, notFound, defferedCols, rb);
                        var t = new SourceFragmentRefDefinition(GetSourceFragment(odef, ss[0], ss[1], escape));
                        dic[GetEntityName(t.Selector, t.Name)] = ed;
                        if (!ed.GetSourceFragments().Contains(t))
                        {
                            ed.AddSourceFragment(t);
                            PropertyDefinition pkProperty = ed.GetPkProperties().Single();
                            t.AnchorTable = ed.GetSourceFragments().First();
                            t.JoinType = SourceFragmentRefDefinition.JoinTypeEnum.outer;
                            t.Conditions.Add(new SourceFragmentRefDefinition.Condition(
                                pkProperty.FieldName, columnName));
                        }
                        DatabaseColumn c = new DatabaseColumn(t.Selector, t.Name, columnName, false, null, false, 1);
                        foreach (Pair<DatabaseColumn, PropertyDefinition> p2 in notFound.FindAll((ff) => ff.First.Equals(c)))
                        {
                            p2.Second.PropertyType = td;
                        }
                    }
                }
                defferedCols = new List<Pair<string>>(defferedCols2);
                defferedCols2.Clear();
            } while (defferedCols.Count != 0);
            return dic;
        }

        private SourceFragmentDefinition GetSourceFragment(SourceFragmentDefinition sf, bool escape)
        {
            string id = "tbl" + sf.Selector + sf.Name;
            var t = _model.GetSourceFragment(id);
            if (t == null)
            {
                string table = sf.Name;
                string schema = sf.Selector;
                if (escape)
                {
                    if (!(table.StartsWith("[") || table.EndsWith("]")))
                        table = "[" + table + "]";

                    if (!(schema.StartsWith("[") || schema.EndsWith("]")))
                        schema = "[" + schema + "]";
                }
                t = new SourceFragmentDefinition(id, table, schema);
                _model.AddSourceFragment(t);
            }
            return t;
        }

        protected PropertyDefinition AppendColumns(Dictionary<DatabaseColumn, DatabaseColumn> columns, EntityDefinition ed,
            string schema, string table, string constraint, bool escape, List<Pair<DatabaseColumn,PropertyDefinition>> notFound,
            List<Pair<string>> defferedCols, relation1to1 rb)
        {
            using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
            {
                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select c.table_schema,c.table_name,c.column_name,is_nullable,data_type,tc.constraint_type,cc.constraint_name, " + AppendIdentity() + @",(select count(*) from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc 
                        join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc on 
                        tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
                        where c.table_name = tc.table_name and c.table_schema = tc.table_schema
                        and tc.constraint_type = 'PRIMARY KEY'
                        ) pk_cnt,c.character_maximum_length from INFORMATION_SCHEMA.columns c
						left join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc on c.table_name = cc.table_name and c.table_schema = cc.table_schema and c.column_name = cc.column_name and cc.constraint_name != @cns
						left join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc on c.table_name = cc.table_name and c.table_schema = cc.table_schema and cc.constraint_name = tc.constraint_name
						where c.table_name = @tbl and c.table_schema = @schema
						and (tc.constraint_type != 'PRIMARY KEY' or tc.constraint_type is null)";
                    DbParameter tbl = cmd.CreateParameter();
                    tbl.ParameterName = "tbl";
                    tbl.Value = table;
                    cmd.Parameters.Add(tbl);

                    DbParameter s = cmd.CreateParameter();
                    s.ParameterName = "schema";
                    s.Value = schema;
                    cmd.Parameters.Add(s);

                    //DbParameter rt = cmd.CreateParameter();
                    //rt.ParameterName = "rtbl";
                    //rt.Value = ed.SourceFragments[0].Name.Trim(new char[] { '[', ']' });
                    //cmd.Parameters.Add(rt);

                    //DbParameter rs = cmd.CreateParameter();
                    //rs.ParameterName = "rsch";
                    //rs.Value = ed.SourceFragments[0].Selector.Trim(new char[] { '[', ']' });
                    //cmd.Parameters.Add(rs);

                    DbParameter cns = cmd.CreateParameter();
                    cns.ParameterName = "cns";
                    cns.Value = constraint;
                    cmd.Parameters.Add(cns);

                    conn.Open();

                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DatabaseColumn c = DatabaseColumn.Create(reader);
                            if (!columns.ContainsKey(c))
                            {
                                columns.Add(c, c);
                                bool cr;
                                Pair<DatabaseColumn, PropertyDefinition> pfd = null;
                                PropertyDefinition pd = AppendColumn(columns, c, ed, out cr, escape, (clm) => pfd = new Pair<DatabaseColumn, PropertyDefinition>(clm, null), defferedCols, rb);
                                if (pfd != null)
                                {
                                    pfd.Second = pd;
                                    notFound.Add(pfd);
                                }
                                if (String.IsNullOrEmpty(pd.Description))
                                {
                                    pd.Description = "Autogenerated from table " + schema + "." + table;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        //protected TypeDefinition GetType(DatabaseColumn c, IDictionary<DatabaseColumn, DatabaseColumn> columns,
        //    WXMLModel odef, bool escape, Action<DatabaseColumn> notFound, List<Pair<string>> defferedCols)
        //{
        //    TypeDefinition t = null;

        //    if (c.IsFK)
        //    {
        //        t = GetRelatedType(c, columns, odef, escape, notFound, defferedCols);
        //    }
        //    else
        //    {
        //        t = GetClrType(c.DbType, c.IsNullable, odef);
        //    }
        //    return t;
        //}
        
        private TypeDefinition GetRelatedType(SourceFieldDefinition field, bool escape)
        {
            
        }


        protected TypeDefinition GetRelatedType(DatabaseColumn col, IDictionary<DatabaseColumn, DatabaseColumn> columns,
            WXMLModel odef, bool escape, Action<DatabaseColumn> notFound, List<Pair<string>> defferedCols)
        {
            using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
            {
                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select tc.table_schema,tc.table_name,cc.column_name from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
						join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc on tc.constraint_name = rc.unique_constraint_name
						join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc on tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and tc.constraint_name = cc.constraint_name
						where rc.constraint_name = @cn";
                    DbParameter cn = cmd.CreateParameter();
                    cn.ParameterName = "cn";
                    cn.Value = col.FKName;
                    cmd.Parameters.Add(cn);

                    conn.Open();

                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DatabaseColumn c = new DatabaseColumn(reader.GetString(reader.GetOrdinal("table_schema")),
                                reader.GetString(reader.GetOrdinal("table_name")),
                                reader.GetString(reader.GetOrdinal("column_name")), false, null, false, 1);
                            if (columns.ContainsKey(c))
                            {
                                string id = "t" + Capitalize(c.Table);
                                TypeDefinition t = odef.GetType(id, false);
                                if (t == null)
                                {
                                    bool cr;
                                    EntityDefinition e = GetEntity(odef, c.Schema, c.Table, out cr, escape);
                                    t = new TypeDefinition(id, e);
                                    odef.AddType(t);
                                    if (cr)
                                    {
                                        Console.WriteLine("\tCreate class {0} ({1})", e.Name, e.Identifier);
                                        //_ents.Add(e.Identifier, null);
                                    }
                                }
                                return t;
                            }
                            else
                            {
                                Pair<string> p = defferedCols.Find((pp) => pp.First == c.FullTableName);
                                if (p != null)
                                {
                                    string clm = null;
                                    reader.Close();
                                    try
                                    {
                                        return GetRelatedType(p.Second, columns, odef, escape, defferedCols, ref clm);
                                    }
                                    catch (InvalidDataException)
                                    {
                                        notFound(c);
                                        return GetClrType(col.DbType, col.IsNullable, odef);
                                    }
                                }
                                else
                                {
                                    notFound(c);
                                    return GetClrType(col.DbType, col.IsNullable, odef);
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        protected TypeDefinition GetRelatedType(string constraint, IDictionary<DatabaseColumn, DatabaseColumn> columns,
            WXMLModel odef, bool escape, List<Pair<string>> defferedCols, ref string clm)
        {
            using (DbConnection conn = GetDBConn(_server, _m, _db, _i, _user, _psw))
            {
                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"select tc.table_schema,tc.table_name,cc.column_name, (
                        select ccu.column_name from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE ccu where ccu.CONSTRAINT_NAME = @cn
                        ) clm  from INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
						join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc on tc.constraint_name = rc.unique_constraint_name
						join INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE cc on tc.table_name = cc.table_name and tc.table_schema = cc.table_schema and tc.constraint_name = cc.constraint_name
						where rc.constraint_name = @cn";
                    DbParameter cn = cmd.CreateParameter();
                    cn.ParameterName = "cn";
                    cn.Value = constraint;
                    cmd.Parameters.Add(cn);

                    conn.Open();

                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DatabaseColumn c = new DatabaseColumn(reader.GetString(reader.GetOrdinal("table_schema")),
                                reader.GetString(reader.GetOrdinal("table_name")),
                                reader.GetString(reader.GetOrdinal("column_name")), false, null, false, 1);
                            clm = reader.GetString(reader.GetOrdinal("clm"));
                            if (columns.ContainsKey(c))
                            {
                                string id = "t" + Capitalize(c.Table);
                                TypeDefinition t = odef.GetType(id, false);
                                if (t == null)
                                {
                                    bool cr;
                                    t = new TypeDefinition(id, GetEntity(odef, c.Schema, c.Table, out cr, escape));
                                    if (cr)
                                    {
                                        Pair<string> p = defferedCols.Find((pp) => pp.First == c.FullTableName);
                                        if (p != null)
                                        {
                                            //odef.RemoveEntity(t.Entity);
                                            reader.Close();
                                            return GetRelatedType(p.Second, columns, odef, escape, defferedCols, ref clm);
                                        }
                                        else
                                        {
                                            odef.RemoveEntity(t.Entity);
                                            throw new InvalidDataException(String.Format("Entity for column {0} was referenced but not created.", c.ToString()));
                                        }
                                    }
                                    odef.AddType(t);
                                }
                                return t;
                            }
                            else
                            {
                                Pair<string> p = defferedCols.Find((pp) => pp.First == c.FullTableName);
                                if (p != null)
                                {
                                    reader.Close();
                                    return GetRelatedType(p.Second, columns, odef, escape, defferedCols, ref clm);
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        #region Static helpers

        private EntityDefinition GetEntity(SourceFragmentDefinition sf, out bool created, bool escape)
        {
            created = false;
            string ename = GetEntityName(sf.Selector, sf.Name);
            EntityDefinition e = _model.GetEntity(ename);
            if (e == null)
            {
                e = new EntityDefinition(ename, Capitalize(sf.Name), string.Empty, null, _model);
                var t = new SourceFragmentRefDefinition(GetSourceFragment(sf, escape));
                e.AddSourceFragment(t);
                //odef.AddEntity(e);
                created = true;
            }
            return e;
        }

        protected static string GetEntityName(string schema, string table)
        {
            return "e_" + schema + "_" + table;
        }

        private static string Capitalize(string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        private static Field2DbRelations IsPrimaryKey(SourceFieldDefinition c, out  attrs)
        {
            //attrs = new string[] { };
            attrs = Field2DbRelations.None;
            if (c.IsPK)
            {
                if (!c.IsAutoIncrement)
                    attrs = Field2DbRelations.PK; //new string[] { "PK" };
                else
                    attrs = Field2DbRelations.PrimaryKey; //new string[] { "PrimaryKey" };
                return true;
            }
            return false;
        }
        private TypeDefinition GetClrType(string dbType, bool nullable)
        {
            TypeDefinition t = null;
            string id = null;
            string type = null;

            switch (dbType)
            {
                case "rowversion":
                case "timestamp":
                    id = "tBytes";
                    type = "System.Byte[]";
                    break;
                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                case "text":
                case "ntext":
                    id = "tString";
                    type = "System.String";
                    break;
                case "int":
                    id = "tInt32";
                    type = "System.Int32";
                    break;
                case "smallint":
                    id = "tInt16";
                    type = "System.Int16";
                    break;
                case "bigint":
                    id = "tInt64";
                    type = "System.Int64";
                    break;
                case "tinyint":
                    id = "tByte";
                    type = "System.Byte";
                    break;
                case "datetime":
                case "smalldatetime":
                    id = "tDateTime";
                    type = "System.DateTime";
                    break;
                case "money":
                case "numeric":
                case "decimal":
                case "smallmoney":
                    id = "tDecimal";
                    type = "System.Decimal";
                    break;
                case "float":
                    id = "tDouble";
                    type = "System.Double";
                    break;
                case "real":
                    id = "tSingle";
                    type = "System.Single";
                    break;
                case "varbinary":
                case "binary":
                    id = "tBytes";
                    type = "System.Byte[]";
                    break;
                case "bit":
                    id = "tBoolean";
                    type = "System.Boolean";
                    break;
                case "xml":
                    id = "tXML";
                    type = "System.Xml.XmlDocument";
                    break;
                case "uniqueidentifier":
                    id = "tGUID";
                    type = "System.Guid";
                    break;
                case "image":
                    id = "tBytes";
                    type = "System.Byte[]";
                    break;
                default:
                    throw new ArgumentException("Unknown database type " + dbType);
            }

            if (nullable)
                id += "nullable";

            t = _model.GetType(id, false);
            if (t == null)
            {
                Type tp = GetTypeByName(type);
                if (nullable && tp.IsValueType)
                    type = String.Format("System.Nullable`1[{0}]", type);

                t = new TypeDefinition(id, type);
                _model.AddType(t);
            }
            return t;
        }

        private static Type GetTypeByName(string type)
        {
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type tp = a.GetType(type, false, true);
                if (tp != null)
                    return tp;
            }
            throw new TypeLoadException("Cannot load type " + type);
        }

        private static string GetTableName(string schema, string table)
        {
            return "[" + schema + "].[" + table + "]";
        }

        #endregion
    }
}
