using System;
using System.Collections.Generic;
using WXML.Model.Descriptors;
using System.Text.RegularExpressions;
using System.Linq;
using WXML.Model;
using WXML.CodeDom;

namespace WXML.SourceConnector
{
    //public class Pair<T>
    //{
    //    internal T _first;
    //    internal T _second;

    //    public Pair()
    //    {
    //    }

    //    public Pair(T first, T second)
    //    {
    //        _first = first;
    //        _second = second;
    //    }

    //    public T First
    //    {
    //        get { return _first; }
    //        set { _first = value; }
    //    }

    //    public T Second
    //    {
    //        get { return _second; }
    //        set { _second = value; }
    //    }
    //}

    //public class Pair<T, T2>
    //{
    //    internal T _first;
    //    internal T2 _second;

    //    public Pair()
    //    {
    //    }

    //    public Pair(T first, T2 second)
    //    {
    //        _first = first;
    //        _second = second;
    //    }

    //    public T First
    //    {
    //        get { return _first; }
    //        set { _first = value; }
    //    }

    //    public T2 Second
    //    {
    //        get { return _second; }
    //        set { _second = value; }
    //    }
    //}

    public enum relation1to1
    {
        Default,
        Unify,
        Hierarchy
    }

    public class SourceToModelConnector
    {
        private readonly SourceView _db;
        private readonly WXMLModel _model;
        private List<SourceFragmentDefinition> _tables2skip;
        private Dictionary<string, EntityDefinition> _entities2skip;

        public delegate void OnEntityCreatedDelegate(SourceToModelConnector sender, EntityDefinition entity);
        public event OnEntityCreatedDelegate OnEntityCreated;
        
        public delegate void OnPropertyCreatedDelegate(SourceToModelConnector sender, PropertyDefinition entity, bool created);
        public event OnPropertyCreatedDelegate OnPropertyCreated;

        public delegate void OnPropertyRemovedDelegate(SourceToModelConnector sender, PropertyDefinition entity);
        public event OnPropertyRemovedDelegate OnPropertyRemoved;

        public SourceToModelConnector(SourceView sourceView, WXMLModel model)
        {
            _db = sourceView;
            _model = model;
            //_transform = transform;
        }

        public SourceView SourceView
        {
            get { return _db; }
        }

        public WXMLModel Model
        {
            get { return _model; }
        }

        public void ApplySourceViewToModel()
        {
            ApplySourceViewToModel(false, relation1to1.Hierarchy, true, true, false);
        }

        public void ApplySourceViewToModel(bool dropProperties, relation1to1 rb, 
            bool transforRawNamesToReadableForm, bool capitalizeNames, bool caseSensitive)
        {
            _tables2skip = new List<SourceFragmentDefinition>();
            _entities2skip = new Dictionary<string, EntityDefinition>();
            
            foreach (SourceFragmentDefinition sf in SourceView.GetSourceFragments())
            {
                if (_tables2skip.Contains(sf))
                    continue;

                if (sf.Constraints.Count(item=>item.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName) == 2 &&
                    SourceView.GetSourceFields(sf).All(clm => clm.IsFK))
                    continue;

                GetEntity(sf, rb, transforRawNamesToReadableForm, capitalizeNames, caseSensitive);
            }

            //Dictionary<string, EntityDefinition> dic = Process1to1Relations(columns, defferedCols, odef, escape, notFound, rb);

            ProcessMany2Many();

            if (dropProperties)
            {
                foreach (EntityDefinition ed in Model.GetActiveEntities())
                {
                    foreach (PropertyDefinition pd in ed.GetActiveProperties()
                        .Where(item=>
                            !string.IsNullOrEmpty(item.Description) &&
                            item.Description.StartsWith("Auto generated from ", StringComparison.Ordinal)
                        ).ToArray()
                    )
                    {
                        var sourceFields = SourceView.GetSourceFields(pd.SourceFragment);

                        if (pd is ScalarPropertyDefinition)
                        {
                            ScalarPropertyDefinition p = pd as ScalarPropertyDefinition;
                            if (!sourceFields
                                .Any(item => (
                                        caseSensitive?Trim(
                                            (capitalizeNames ? Capitalize(item.SourceFieldExpression) : item.SourceFieldExpression), 
                                            transforRawNamesToReadableForm
                                        ):item.SourceFieldExpression.ToLower()
                                    ) == (
                                        caseSensitive?p.SourceFieldExpression:p.SourceFieldExpression.ToLower()
                                    )))
                            {
                                ed.RemoveProperty(pd);
                                RaiseOnPropertyRemoved(pd);
                            }
                        }
                        else if (pd is EntityPropertyDefinition)
                        {
                            EntityPropertyDefinition p = pd as EntityPropertyDefinition;
                            foreach (EntityPropertyDefinition.SourceField field in p.SourceFields.ToArray())
                            {
                                if (!sourceFields.Any(item => 
                                    (caseSensitive?Trim(capitalizeNames ? Capitalize(item.SourceFieldExpression) 
                                        : item.SourceFieldExpression, transforRawNamesToReadableForm):item.SourceFieldExpression.ToLower()) == (caseSensitive?field.SourceFieldExpression:field.SourceFieldExpression.ToLower())))
                                {
                                    p.RemoveSourceField(field);
                                }
                            }
                            if (p.SourceFields.Count() == 0)
                            {
                                ed.RemoveProperty(pd);
                                RaiseOnPropertyRemoved(pd);
                            }
                        }
                        else
                            throw new NotSupportedException(pd.GetType().ToString());
                    }
                }
            }

            ProcessOne2Many();
        }

        private void ProcessOne2Many()
        {
            foreach (EntityDefinition e_ in Model.GetEntities())
            {
                EntityDefinition e = e_;

                foreach (EntityDefinition oe_ in
                    from k in Model.GetActiveEntities()
                    where /*k != e && */!e.One2ManyRelations.Any(item => 
                        !item.Disabled && item.Entity.Identifier == k.Identifier)
                    select k)
                {
                    EntityDefinition oe = oe_;
                    var entityProps = oe.GetActiveProperties()
                        .Where(item => 
                               item.PropertyType.IsEntityType && 
                               item.PropertyType.Entity.Identifier == e.Identifier)
                        .Cast<EntityPropertyDefinition>();
                    int idx = 1;
                    foreach (EntityPropertyDefinition pd in entityProps)
                    {
                        int cnt = Model.GetActiveRelations().OfType<RelationDefinition>().Count(r =>
                            (r.Left.Entity.Identifier == oe.Identifier && r.Right.Entity.Identifier == e.Identifier) ||
                            (r.Left.Entity.Identifier == e.Identifier && r.Right.Entity.Identifier == oe.Identifier));

                        string accName = null; string prop = null;
                        if (cnt > 0 || entityProps.Count() > 1)
                        {
                            accName = WXMLCodeDomGeneratorNameHelper.GetMultipleForm(oe.Name + idx);
                            prop = pd.Name;
                        }

                        e.AddEntityRelations(new EntityRelationDefinition()
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

        private EntityDefinition GetEntity(SourceFragmentDefinition sf, relation1to1 rb,
            bool transforRawNamesToReadableForm, bool capitalizeNames, bool caseSensitive)
        {
            var entityIdentifier = GetEntityIdentifier(sf.Selector, sf.Name);
            EntityDefinition e;
            if (!_entities2skip.TryGetValue(entityIdentifier, out e))
            {
                EntityDefinition masterEntity = null;
                SourceFragmentDefinition masterTable = null;
                List<SourceFragmentRefDefinition.Condition> conds = null;
                if (rb != relation1to1.Default && SourceView.GetSourceFields(sf)
                    .Where(item => item.IsPK)
                    .SelectMany(item => item.Constraints)
                    .Count(item => item.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName) == 1 &&
                    SourceView.GetSourceFields(sf)
                        .Where(item => item.IsPK)
                        .All(item => item.IsFK)
                )
                {
                    switch (rb)
                    {
                        case relation1to1.Unify:
                        case relation1to1.Hierarchy:
                            masterTable = GetMasterTable(sf, out conds);
                            masterEntity = GetEntity(masterTable, rb, transforRawNamesToReadableForm, capitalizeNames, caseSensitive);
                            break;
                        default:
                            throw new NotSupportedException(rb.ToString());
                    }
                }

                e = Model.GetEntity(entityIdentifier);
                if (e == null)
                {
                    bool entCreated;
                    e = GetEntity(sf, out entCreated, capitalizeNames);
                    if (entCreated)
                        RaiseOnEntityCreated(e);
                }
                _tables2skip.Add(sf);
                _entities2skip.Add(entityIdentifier, e);

                foreach (SourceFieldDefinition field in SourceView.GetSourceFields(sf)
                    .Where(item => !item.IsFK))
                {
                    bool propCreated;
                    PropertyDefinition prop = AppendColumn(e, field, out propCreated, transforRawNamesToReadableForm, capitalizeNames, caseSensitive);
                    RaiseOnPropertyCreated(prop, propCreated);
                }

                foreach (SourceConstraint fk in sf.Constraints.Where(item => item.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName))
                {
                    bool propCreated;
                    PropertyDefinition prop = AppendFK(e, sf, fk, rb, out propCreated, transforRawNamesToReadableForm, capitalizeNames, caseSensitive);
                    if (prop != null)
                        RaiseOnPropertyCreated(prop, propCreated);
                }

                if (masterEntity != null)
                {
                    SourceFragmentRefDefinition sfr;
                    switch (rb)
                    {
                        case relation1to1.Unify:
                            sfr = masterEntity.GetSourceFragments()
                                .Single(item => item.Identifier == masterTable.Identifier);
                            sfr.AnchorTable = sf;
                            sfr.JoinType = SourceFragmentRefDefinition.JoinTypeEnum.outer;
                            sfr.Conditions.AddRange(conds);
                            masterEntity.AddSourceFragment(new SourceFragmentRefDefinition(sf));

                            foreach (PropertyDefinition property in e.GetProperties()
                                .Where(item => !item.HasAttribute(Field2DbRelations.PK)/* && 
                                    item.SourceFragment != masterTable*/))
                            {
                                if (masterEntity.GetProperties().Any(item => item.PropertyAlias == property.PropertyAlias))
                                {
                                    property.PropertyAlias = e.Name + "_" + property.PropertyAlias;
                                    property.Name = e.Name + "_" + property.Name;
                                }

                                if (!masterEntity.GetProperties().Any(item => item.PropertyAlias == property.PropertyAlias))
                                    masterEntity.AddProperty(property);
                            }

                            Model.RemoveEntity(e);

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

            return e;
        }

        protected void RaiseOnPropertyRemoved(PropertyDefinition prop)
        {
             if (OnPropertyRemoved != null)
                OnPropertyRemoved(this, prop);
        }

        protected void RaiseOnPropertyCreated(PropertyDefinition prop, bool created)
        {
             if (OnPropertyCreated != null)
                OnPropertyCreated(this, prop, created);
        }

        protected void RaiseOnEntityCreated(EntityDefinition definition)
        {
            if (OnEntityCreated != null)
                OnEntityCreated(this, definition);
        }

        private SourceFragmentDefinition GetMasterTable(SourceFragmentDefinition sf, 
            out List<SourceFragmentRefDefinition.Condition> conditions)
        {
            SourceConstraint fk = SourceView.GetSourceFields(sf)
                .Where(item => item.IsPK)
                .SelectMany(item => item.Constraints)
                .Single(item => item.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName);

            SourceFragmentDefinition m = null;
            conditions = new List<SourceFragmentRefDefinition.Condition>();
            foreach (SourceReferences rel in SourceView.GetFKRelations(fk))
            {
                if (m == null)
                    m = rel.PKField.SourceFragment;

                conditions.Add(new SourceFragmentRefDefinition.Condition(
                    rel.PKField.SourceFieldExpression, rel.FKField.SourceFieldExpression
                ));
            }
            
            return m;
        }

        protected void ProcessMany2Many()
        {
            foreach (SourceFragmentDefinition sf in SourceView.GetSourceFragments()
                .Where(item=>SourceView.GetSourceFields(item).All(clm => clm.IsFK) &&
                    item.Constraints.Count(citem => citem.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName) == 2))
            {
                List<LinkTarget> targets = new List<LinkTarget>();
                foreach (SourceConstraint fk in sf.Constraints.Where(item => 
                        item.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName))
                {
                    var rels = SourceView.GetFKRelations(fk);
                    SourceFragmentDefinition m = rels.First().PKField.SourceFragment;

                    EntityDefinition e = Model.GetEntity(GetEntityIdentifier(m.Selector, m.Name));
                    LinkTarget lt = new LinkTarget(
                        e,
                        rels.Select(item => item.FKField.SourceFieldExpression).ToArray(),
                        rels.Select(item => e.GetPkProperties().Single(p=>p.SourceFieldExpression == item.PKField.SourceFieldExpression).PropertyAlias).ToArray(),
                        rels.First().DeleteAction == SourceConstraint.CascadeAction
                    );
                    targets.Add(lt);
                }

                if (targets.Count != 2)
                    continue;

                if (targets[0].Entity.Name == targets[1].Entity.Name)
                {
                    LinkTarget t = targets[0];
                    SelfRelationDefinition newRel = new SelfRelationDefinition(
                        t.Entity, t.EntityProperties, targets[0], targets[1],
                        GetSourceFragment(sf), null);

                    if (sf.Constraints.Any(item => item.ConstraintType == SourceConstraint.PrimaryKeyConstraintTypeName))
                        newRel.Constraint = RelationConstraint.PrimaryKey;
                    else if (sf.Constraints.Any(item => item.ConstraintType == SourceConstraint.UniqueConstraintTypeName))
                        newRel.Constraint = RelationConstraint.Unique;

                    if (Model.GetSimilarRelation(newRel) == null)
                    {
                        string postFix = string.Empty;
                        if (string.IsNullOrEmpty(newRel.Left.AccessorName))
                        {
                            if (newRel.Left.FieldName.Length == 1)
                            {
                                if (newRel.Left.FieldName[0].EndsWith("_id", StringComparison.InvariantCultureIgnoreCase))
                                    newRel.Left.AccessorName = newRel.Left.FieldName[0]
                                        .Substring(0,newRel.Left.FieldName[0].Length - 3);
                                else if (newRel.Left.FieldName[0].EndsWith("id", StringComparison.InvariantCultureIgnoreCase))
                                    newRel.Left.AccessorName = newRel.Left.FieldName[0]
                                        .Substring(0, newRel.Left.FieldName[0].Length - 2);
                            }
                            
                            if (string.IsNullOrEmpty(newRel.Left.AccessorName))
                            {
                                newRel.Left.AccessorName = newRel.Entity.Name;
                                postFix = "1";
                            }
                        }

                        if (string.IsNullOrEmpty(newRel.Right.AccessorName))
                        {
                            if (newRel.Left.FieldName.Length == 1)
                            {
                                if (newRel.Right.FieldName[0].EndsWith("_id", StringComparison.InvariantCultureIgnoreCase))
                                    newRel.Right.AccessorName = newRel.Right.FieldName[0]
                                        .Substring(0, newRel.Right.FieldName[0].Length - 3);
                                else if (newRel.Right.FieldName[0].EndsWith("id", StringComparison.InvariantCultureIgnoreCase))
                                    newRel.Right.AccessorName = newRel.Right.FieldName[0]
                                        .Substring(0, newRel.Right.FieldName[0].Length - 2);
                            }

                            if (string.IsNullOrEmpty(newRel.Right.AccessorName))
                                newRel.Right.AccessorName = newRel.Entity.Name + postFix;

                        }
                        Model.AddRelation(newRel);
                    }
                }
                else
                {
                    RelationDefinition newRel = new RelationDefinition(
                        targets[0], targets[1], GetSourceFragment(sf), null);

                    if (sf.Constraints.Any(item => item.ConstraintType == SourceConstraint.PrimaryKeyConstraintTypeName))
                        newRel.Constraint = RelationConstraint.PrimaryKey;
                    else if (sf.Constraints.Any(item => item.ConstraintType == SourceConstraint.UniqueConstraintTypeName))
                        newRel.Constraint = RelationConstraint.Unique;

                    if (!Model.GetRelations().OfType<RelationDefinition>().Any(m => m.Equals(newRel)))
                    {
                        if (Model.HasSimilarRelationM2M(newRel))
                        {
                            if (string.IsNullOrEmpty(newRel.Left.AccessorName) ||
                                string.IsNullOrEmpty(newRel.Right.AccessorName))
                            {
                                var lst = from r in Model.GetRelations().OfType<RelationDefinition>()
                                      where
                                          !ReferenceEquals(r.Left, newRel.Left) &&
                                          !ReferenceEquals(r.Right, newRel.Right) &&
                                          (
                                              ((r.Left.Entity == newRel.Left.Entity &&
                                                string.IsNullOrEmpty(r.Right.AccessorName))
                                               &&
                                               (r.Right.Entity == newRel.Right.Entity &&
                                                string.IsNullOrEmpty(r.Left.AccessorName))) ||
                                              ((r.Left.Entity == newRel.Right.Entity &&
                                                string.IsNullOrEmpty(r.Right.AccessorName))
                                               &&
                                               (r.Right.Entity == newRel.Left.Entity &&
                                                string.IsNullOrEmpty(r.Left.AccessorName)))
                                          )
                                      select r;

                                if (lst.Count() > 0)
                                {
                                    foreach (RelationDefinition r in lst)
                                    {
                                        if (string.IsNullOrEmpty(r.Left.AccessorName))
                                            r.Left.AccessorName = r.SourceFragment.Name.TrimEnd(']').TrimStart('[') +
                                                                  r.Right.Entity.Name;
                                        if (string.IsNullOrEmpty(r.Right.AccessorName))
                                            r.Right.AccessorName = r.SourceFragment.Name.TrimEnd(']').TrimStart('[') +
                                                                   r.Left.Entity.Name;
                                    }

                                    if (string.IsNullOrEmpty(newRel.Left.AccessorName))
                                        newRel.Left.AccessorName =
                                            newRel.SourceFragment.Name.TrimEnd(']').TrimStart('[') +
                                            newRel.Right.Entity.Name;
                                    if (string.IsNullOrEmpty(newRel.Right.AccessorName))
                                        newRel.Right.AccessorName =
                                            newRel.SourceFragment.Name.TrimEnd(']').TrimStart('[') +
                                            newRel.Left.Entity.Name;
                                }
                            }
                        }
                        Model.AddRelation(newRel);
                    }
                }
            }

            foreach (SelfRelationDefinition rdb in Model.GetActiveRelations().OfType<SelfRelationDefinition>())
            {
                NormalizeRelationAccessors(rdb, rdb.Right.AccessorName, rdb.Entity);
                NormalizeRelationAccessors(rdb, rdb.Left.AccessorName, rdb.Entity);
            }

            foreach (RelationDefinition rdb in Model.GetActiveRelations().OfType<RelationDefinition>())
            {
                NormalizeRelationAccessors(rdb, rdb.Right.AccessorName, rdb.Right.Entity);
                NormalizeRelationAccessors(rdb, rdb.Left.AccessorName, rdb.Left.Entity);
            }
        }

        private void NormalizeRelationAccessors(RelationDefinitionBase relation,
            string searchedName, EntityDefinition rdbEntity)
        {
            if (string.IsNullOrEmpty(searchedName)) return;

            var q1 =
                from r in Model.GetActiveRelations().OfType<SelfRelationDefinition>()
                where r != relation && r.Entity.Identifier == rdbEntity.Identifier &&
                    (r.Left.AccessorName == searchedName || r.Right.AccessorName == searchedName)
                select r as RelationDefinitionBase;

            var q2 =
                from r in Model.GetActiveRelations().OfType<RelationDefinition>()
                where r != relation &&
                    (r.Right.Entity.Identifier == rdbEntity.Identifier &&
                        r.Left.AccessorName == searchedName) ||
                    (r.Left.Entity.Identifier == rdbEntity.Identifier &&
                        r.Right.AccessorName == searchedName)
                select r as RelationDefinitionBase;

            int i = 0;
            foreach (RelationDefinitionBase r in q1.Union(q2))
            {
                i++;
                RelationDefinition rd = r as RelationDefinition;
                SelfRelationDefinition srd = r as SelfRelationDefinition;

                if (srd != null)
                {
                    if (srd.Left.AccessorName == searchedName)
                        srd.Left.AccessorName += i.ToString();
                    else if (srd.Right.AccessorName == searchedName)
                        srd.Right.AccessorName += i.ToString();
                }
                else if (rd != null)
                {
                    if (rd.Left.AccessorName == searchedName)
                        rd.Left.AccessorName += i.ToString();
                    else if (rd.Right.AccessorName == searchedName)
                        rd.Right.AccessorName += i.ToString();
                }
            }
        }

        protected EntityPropertyDefinition AppendFK(EntityDefinition e, SourceFragmentDefinition sf, 
            SourceConstraint fk, relation1to1 rb, out bool created, 
            bool transforRawNamesToReadableForm, bool capitalizeNames, bool caseSensitive)
        {
            created = false;
            var rels = SourceView.GetFKRelations(fk);
            SourceFragmentDefinition m = rels.First().PKField.SourceFragment;
            EntityDefinition re = e;
            if (sf != m)
                re = GetEntity(m, rb, transforRawNamesToReadableForm, capitalizeNames, caseSensitive);
            string rid = "t" + re.Name;
            TypeDefinition td = Model.GetType(rid, false);
            if (td == null)
            {
                td = new TypeDefinition(rid, re);
                Model.AddType(td);
            }

            string propAlias = td.Entity.Name;
            if (rels.Count() == 1)
            {
                propAlias = Trim(GetName(rels.First().FKField.SourceFieldExpression), true);
            }

            if (capitalizeNames)
                propAlias = Capitalize(propAlias);

            string propName = propAlias;

            EntityPropertyDefinition ep = null;
            //try
            //{
            ep = (EntityPropertyDefinition) e.OwnProperties
                .SingleOrDefault(item=>item.Identifier == propAlias);

            if (ep == null)
            {
                ep = e.OwnProperties.OfType<EntityPropertyDefinition>().SingleOrDefault(item=>fk.SourceFields.All(sf2=>
                    item.SourceFields.Any(sff=>sf2.SourceFieldExpression.Trim('[',']') == sff.SourceFieldExpression.Trim('[',']'))
                    ));
            }
            
            if (ep == null)
            {
                var one2one = fk.SourceFields.All(sss => sss.IsPK);

                if (one2one && e.BaseEntity == re)
                {
                    foreach (var sfd_ in fk.SourceFields)
                    {
                        bool x;
                        var xx = AppendColumn(e, sfd_, out x, transforRawNamesToReadableForm, capitalizeNames, caseSensitive);
                        RaiseOnPropertyCreated(xx, x);
                    }
                    return null;
                }

                int cnt = e.OwnProperties.Count(p => p.Name == propName);
                if (cnt > 0)
                {
                    propName = propName + cnt;
                    propAlias = propAlias + cnt;
                }

                SourceFragmentDefinition sfd = GetSourceFragment(sf);

                ep = new EntityPropertyDefinition(propName, propAlias,
                    Field2DbRelations.None, "Auto generated from constraint " + fk.ConstraintName,
                    AccessLevel.Private, AccessLevel.Public, td, sfd, e);

                e.AddProperty(ep);
                created = true;

                foreach (SourceReferences rel in rels)
                {
                    SourceFieldDefinition fld = SourceView.GetSourceFields(sf).Single(item=>item.SourceFieldExpression==rel.FKField.SourceFieldExpression);

                    ScalarPropertyDefinition pk = re.GetPkProperties().SingleOrDefault(item=>item.SourceFieldExpression==rel.PKField.SourceFieldExpression);
                    if (pk == null)
                    {
                        if (rel.PKConstraint.ConstraintType != SourceConstraint.UniqueConstraintTypeName)
                            throw new WXMLException(string.Format("Cannot find pk for constraint {0}", rel.PKConstraint.ConstraintName));

                        pk = re.GetProperties().OfType<ScalarPropertyDefinition>().Single(item => item.SourceFieldExpression == rel.PKField.SourceFieldExpression);
                    }

                    ep.AddSourceField(pk.PropertyAlias,
                        fld.SourceFieldExpression, null, fld.SourceType, fld.SourceTypeSize, fld.IsNullable, fld.DefaultValue
                    );
                }
            }
            else
            {
                if (ep.Description=="Auto generated from constraint " + fk.ConstraintName)
                {
                    ep.PropertyType = td;
                }
            }
            
            foreach (SourceFieldDefinition pkField in SourceView.GetFKRelations(fk)
                .Select(item=>item.FKField)
                .Where(item=>item.IsPK))
            {
                string pkPropAlias = GetName(pkField.SourceFieldExpression);

                if (!SourceView.GetSourceFields(pkField.SourceFragment).Any(item => GetName(item.SourceFieldExpression).Equals(Trim(pkPropAlias, transforRawNamesToReadableForm), StringComparison.InvariantCultureIgnoreCase)))
                    pkPropAlias = Trim(pkPropAlias, transforRawNamesToReadableForm);

                //string pkPropAlias = Trim(GetName(pkField.SourceFieldExpression), transforRawNamesToReadableForm);
                if (capitalizeNames)
                    pkPropAlias = Capitalize(pkPropAlias);

                string pkPropName = pkPropAlias;
                PropertyDefinition pe = e.OwnProperties
                    .SingleOrDefault(pd =>pd.Identifier == pkPropAlias);
                Field2DbRelations attrs = pkField.GetAttributes();
                TypeDefinition pkType = GetClrType(pkField.SourceType, pkField.IsNullable, ref attrs);
                bool pkCreated = pe == null;
                if (pkCreated)
                {
                    int cnt = e.OwnProperties.Count(p => p.Name == pkPropName);
                    if (cnt > 0)
                    {
                        pkPropName = pkPropName + cnt;
                        //pkPropAlias = pkPropAlias + cnt;
                    }

                    pe = new ScalarPropertyDefinition(e, pkPropName, pkPropAlias, attrs, 
                        "Auto generated from column " + pkField.SourceFieldExpression,
                        pkType, pkField, AccessLevel.Private, AccessLevel.Public);

                    e.AddProperty(pe);
                }
                else
                {
l1:
                    if (pe is ScalarPropertyDefinition)
                    {
                        pe.Attributes |= attrs;
                        pe.PropertyType = pkType;
                        ((ScalarPropertyDefinition)pe).SourceField = pkField;
                    }
                    else
                    {
                        int cnt = e.OwnProperties.Count(p => p.Identifier == pkPropAlias);
                        if (cnt > 0)
                        {
                            if (e.OwnProperties.Any(item => item.Identifier == GetName(pkField.SourceFieldExpression)))
                                pkPropAlias = pkPropAlias + cnt;
                            else
                                pkPropAlias = GetName(pkField.SourceFieldExpression);
                        }
                        pkPropName = pkPropAlias;

                        pe = e.OwnProperties.SingleOrDefault(item => item.PropertyAlias == pkPropAlias);
                        if (pe != null)
                        {
                            if (pe is EntityPropertyDefinition)
                                throw new WXMLParserException(string.Format("Property {0} expected to be of type ScalarPropertyDefinition", pe.Identifier));
                            goto l1;
                        }

                        pe = new ScalarPropertyDefinition(e, pkPropName, pkPropAlias, attrs,
                            "Auto generated from column " + pkField.SourceFieldExpression,
                            pkType, pkField, AccessLevel.Private, AccessLevel.Public);

                        e.AddProperty(pe);
                    }
                }
                RaiseOnPropertyCreated(pe, pkCreated);
            }
            //}
            //catch
            //{
            //    int i = 10;
            //}
            return ep;
        }

        protected PropertyDefinition AppendColumn(EntityDefinition e, SourceFieldDefinition c,
            out bool created, bool transforRawNamesToReadableForm, bool capitalizeNames, bool caseSensitive)
        {
            created = false;
            //var x = e.OwnProperties.OfType<ScalarPropertyDefinition>().Where(pd =>
            //    pd.SourceFieldExpression.Trim(']', '[') == c.SourceFieldExpression.Trim('[',']')
            //);
            
            var nsfn = c.SourceFieldExpression.Trim('[', ']');
            if (!caseSensitive)
                nsfn = nsfn.ToLower();

            var x = e.OwnProperties.Where(pd =>
                {
                    ScalarPropertyDefinition sp = pd as ScalarPropertyDefinition;
                    if (sp != null)
                    {
                        var spsf = sp.SourceFieldExpression.Trim(']', '[');
                        if (!caseSensitive)
                            spsf = spsf.ToLower();

                        return spsf == nsfn;
                    }
                    else
                    {
                        EntityPropertyDefinition ep = pd as EntityPropertyDefinition;
                        if (ep != null)
                        {
                            return ep.SourceFields.Any(item => {
                                var spsf = item.SourceFieldExpression.Trim(']', '[');
                                if (!caseSensitive)
                                    spsf = spsf.ToLower();
                                return spsf == nsfn;
                            });
                        }
                    }
                    return false;
                }
            );

            PropertyDefinition pe = null;
            ScalarPropertyDefinition pes = x.OfType<ScalarPropertyDefinition>().SingleOrDefault();

            if (pes != null)
                pe = pes;
            else
            {
                pe = x.OfType<EntityPropertyDefinition>().SingleOrDefault();
            }

            GetSourceFragment(c.SourceFragment);

            if (pe == null)
            {
                Field2DbRelations attrs = c.GetAttributes();
                string name = GetName(c.SourceFieldExpression);

                if (!SourceView.GetSourceFields(c.SourceFragment).Any(item => GetName(item.SourceFieldExpression).Equals(Trim(name, transforRawNamesToReadableForm), StringComparison.InvariantCultureIgnoreCase)))
                    name = Trim(name, transforRawNamesToReadableForm);

                if (capitalizeNames)
                    name = Capitalize(name);

                if ((attrs & Field2DbRelations.PK) == Field2DbRelations.PK && c.IsNullable)
                    throw new WXMLException(string.Format("Column {0}.{1} cannot be nullable since it's a primary key", c.SourceFragment,c.SourceFieldExpression));

                var t = GetClrType(c.SourceType, c.IsNullable, ref attrs);

                pe = new ScalarPropertyDefinition(e, name,
                     name, attrs, "Auto generated from column " + c.SourceFieldExpression, t, c, 
                     AccessLevel.Private, AccessLevel.Public
                );

                e.AddProperty(pe);
                created = true;
            }
            else
            {
                Field2DbRelations attrs = c.GetAttributes();
                if (attrs == Field2DbRelations.None)
                    attrs = pe.Attributes;

                ScalarPropertyDefinition sp = pe as ScalarPropertyDefinition;
                if (sp != null)
                {                    
                    if (!pe.PropertyType.IsUserType && (attrs & Field2DbRelations.PK) != Field2DbRelations.PK &&
                        sp.SourceType != c.SourceType)
                        pe.PropertyType = GetClrType(c.SourceType, c.IsNullable, ref attrs);

                    sp.SourceField = c;
                }
            
                if ((attrs & Field2DbRelations.PK) == Field2DbRelations.PK && e.BaseEntity != null && e.BaseEntity.GetPkProperties().Any())
                {
                    attrs = attrs & ~Field2DbRelations.PK;
                }

                if (attrs == Field2DbRelations.None)
                    attrs = pe.Attributes;

                pe.Attributes = attrs;
            }
            return pe;
        }

        private static string Trim(string columnName, bool transforRawNamesToReadableForm)
        {
            columnName = columnName.Replace(' ', '_');
            if (transforRawNamesToReadableForm)
            {
                if (columnName.EndsWith("_id"))
                    columnName = columnName.Substring(0, columnName.Length - 3);
                else if (columnName.EndsWith("_dt"))
                    columnName = columnName.Substring(0, columnName.Length - 3);
                else if (columnName.Length > 2 && columnName.EndsWith("id", StringComparison.InvariantCultureIgnoreCase))
                    columnName = columnName.Substring(0, columnName.Length - 2);

                Regex re = new Regex(@"_(\w)");
                columnName = re.Replace(columnName, new MatchEvaluator(m => m.Groups[1].Value.ToUpper()));

                re = new Regex(@"(\w)-(\w)");
                columnName = re.Replace(columnName, new MatchEvaluator(m => m.Groups[1] + m.Groups[2].Value.ToUpper()));
            }
            return columnName;
        }

        private SourceFragmentDefinition GetSourceFragment(SourceFragmentDefinition sf)
        {
            var t = Model.GetSourceFragment(sf.Identifier);
            if (t == null)
            {
                t = sf;
                Model.AddSourceFragment(t);
            }

            return t;
        }

        #region Static helpers

        private EntityDefinition GetEntity(SourceFragmentDefinition sf, out bool created, bool capitalizeNames)
        {
            created = false;
            string entityId = GetEntityIdentifier(sf.Selector, sf.Name);
            EntityDefinition e = Model.GetEntity(entityId);
            if (e == null)
            {
                string ename = GetName(sf.Name);
                if (capitalizeNames)
                    ename = Capitalize(ename);

                e = new EntityDefinition(entityId, ename, string.Empty, 
                    string.Format("Auto generated from {0}", string.IsNullOrEmpty(sf.Selector) ? sf.Name : $"{sf.Selector}.{sf.Name}"), 
                    Model);
                var t = new SourceFragmentRefDefinition(GetSourceFragment(sf));
                e.AddSourceFragment(t);
                //odef.AddEntity(e);
                created = true;
            }
            return e;
        }

        protected static string GetEntityIdentifier(string schema, string table)
        {
            return ("e_" + schema + "_" + table).Replace("[", string.Empty).Replace("]", string.Empty);
        }

        private static string GetName(string name)
        {
            return name.Replace("[", string.Empty).Replace("]", string.Empty);
        }

        private static string Capitalize(string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1);
        }

        private TypeDefinition GetClrType(string dbType, bool nullable, ref Field2DbRelations attr)
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
                    attr = attr | Field2DbRelations.RowVersion;
                    break;
                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                case "text":
                case "ntext":
                case "enum":
                case "mediumtext":
                    id = "tString";
                    type = "System.String";
                    break;
                case "int":
                case "mediumint":
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
                case "date":
                    id = "tDateTime";
                    type = "System.DateTime";
                    break;
                case "time":
                    id = "tTime";
                    type = "System.Timespan";
                    break;
                case "money":
                case "numeric":
                case "decimal":
                case "smallmoney":
                    id = "tDecimal";
                    type = "System.Decimal";
                    break;
                case "float":
                case "double":
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

            t = Model.GetType(id, false);
            if (t == null)
            {
                Type tp = GetTypeByName(type);
                if (nullable && tp.IsValueType)
                    type = String.Format("System.Nullable`1[{0}]", type);

                t = new TypeDefinition(id, type);
                Model.AddType(t);
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

        //private static string GetTableName(string schema, string table)
        //{
        //    return "[" + schema + "].[" + table + "]";
        //}

        #endregion
    }
}
