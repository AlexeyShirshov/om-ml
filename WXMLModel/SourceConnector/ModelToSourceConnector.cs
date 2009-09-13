using System;
using System.Linq;
using System.Text;
using WXML.Model;
using WXML.Model.Descriptors;
using System.Collections.Generic;

namespace WXML.SourceConnector
{
    public class ModelToSourceConnector
    {
        private readonly SourceView _db;
        private readonly WXMLModel _model;

        public ModelToSourceConnector(SourceView sourceView, WXMLModel model)
        {
            _db = sourceView;
            _model = model;
        }

        public SourceView SourceView
        {
            get { return _db; }
        }

        public WXMLModel Model
        {
            get { return _model; }
        }

        public string GenerateSourceScript(ISourceProvider provider, bool unicodeStrings)
        {
            var props = Model.GetActiveEntities().SelectMany(item =>
                item.OwnProperties.Where(p => !p.Disabled && p.SourceFragment != null)
            );

            StringBuilder script = new StringBuilder();

            CreateTables(script, props, provider, unicodeStrings);

            AlterTables(script, props, provider, unicodeStrings);

            CreatePrimaryKeys(script, provider);

            CreateUniqueConstraints(script, provider);

            CreateForeignKeys(script, provider);

            return script.ToString();
        }

        private void AlterTables(StringBuilder script, IEnumerable<PropertyDefinition> props, ISourceProvider provider, bool unicodeStrings)
        {
            bool hdr = false;

            foreach (SourceFragmentDefinition s in props.Select(item => item.SourceFragment).Distinct())
            {
                SourceFragmentDefinition sf = s;

                var targetSF = SourceView.GetSourceFragments().SingleOrDefault(item =>
                     item.Name == sf.Name && item.Selector == sf.Selector);

                if (targetSF != null)
                {
                    List<PropDefinition> tableProps = new List<PropDefinition>();
                    foreach (PropertyDefinition prop in props.Where(item => item.SourceFragment == sf))
                    {
                        if(prop is ScalarPropertyDefinition)
                            tableProps.Add(new PropDefinition{Attr = prop.Attributes, Field = ((ScalarPropertyDefinition)prop).SourceField, PropType = prop.PropertyType});
                        else
                            tableProps.AddRange(((EntityPropertyDefinition)prop).SourceFields
                                .Select(item=>new PropDefinition{Attr = Field2DbRelations.None, Field = item, PropType = prop.PropertyType.Entity.GetProperties().Single(p=>p.PropertyAlias==item.PropertyAlias).PropertyType}));
                    }

                    var props2Add = tableProps.Where(item=>!SourceView.GetSourceFields(targetSF).Any(fld=>fld.SourceFieldExpression == item.Field.SourceFieldExpression));
                    if (props2Add.Count() > 0)
                    {
                        if (!hdr)
                        {
                            script.AppendLine("--Altering tables");
                            hdr = true;
                        }
                        provider.GenerateAddColumnsScript(props2Add, script, unicodeStrings);
                    }
                }
            }
        }

        private void CreateForeignKeys(StringBuilder script, ISourceProvider provider)
        {
            var fks = Model.GetActiveEntities().SelectMany(item => item.GetProperties()
                .OfType<EntityPropertyDefinition>()
            ).Where(item=>item.SourceFragment != null);

            List<string> names = new List<string>();

            bool hdr = false;

            foreach (SourceFragmentDefinition s in fks.Select(item => item.SourceFragment).Distinct())
            {
                SourceFragmentDefinition sf = s;

                var targetSF = SourceView.GetSourceFragments().SingleOrDefault(item =>item.Name == sf.Name && item.Selector == sf.Selector);

                List<FKDefinition> fksList = new List<FKDefinition>();

                foreach (EntityPropertyDefinition prop in fks.Where(item => item.SourceFragment == sf))
                {
                    EntityDefinition re = prop.PropertyType.Entity;

                    var fpk = re.GetPkProperties().Where(item => prop.SourceFields.Any(fld=>fld.PropertyAlias == item.PropertyAlias));
                    if (fpk.Count() == 0)
                        fpk = re.GetProperties().OfType<ScalarPropertyDefinition>()
                            .Where(item => !item.Disabled && item.SourceField.Constraints.Any(cns => cns.ConstraintType == SourceConstraint.UniqueConstraintTypeName));

                    FKDefinition f = new FKDefinition
                    {
                        cols = prop.SourceFields.Select(item=>item.SourceFieldExpression).ToArray(),
                        refCols = prop.SourceFields.Select(item=>fpk.Single(pk=>pk.PropertyAlias == item.PropertyAlias).SourceFieldExpression).ToArray(),
                        refTbl = fpk.Single(pk=>pk.PropertyAlias == prop.SourceFields.First().PropertyAlias).SourceFragment
                    };

                    if (targetSF != null)
                    {
                        if (targetSF.Constraints.Any(item => 
                            item.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName &&
                            prop.SourceFields.All(p=>item.SourceFields.Any(fkf => 
                                fkf.SourceFieldExpression == p.SourceFieldExpression)
                            )
                        ))
                            continue;
                    }

                    if (string.IsNullOrEmpty(f.constraintName))
                    {
                        f.constraintName = "FK_" + prop.Entity.Name + "_" + re.Name;

                        if (names.Contains(f.constraintName))
                        {
                            f.constraintName += "_" + prop.Name;
                        }
                    }
                    
                    names.Add(f.constraintName);
                    fksList.Add(f);
                }

                if (fksList.Count > 0)
                {
                    if (!hdr)
                    {
                        script.AppendLine("--Creating foreign keys");
                        hdr = true;
                    }
                    provider.GenerateCreateFKsScript(sf, fksList, script);
                }
            }

            foreach (RelationDefinitionBase rel in Model.GetActiveRelations())
            {
                var relSF = SourceView.GetSourceFragments().SingleOrDefault(item => item.Name == rel.SourceFragment.Name && item.Selector == rel.SourceFragment.Selector);
                List<FKDefinition> fksList = new List<FKDefinition>();

                if (rel is SelfRelationDefinition)
                {
                    SelfRelationDefinition r = rel as SelfRelationDefinition;
                   
                    FKDefinition f;
                    if (CreateFKDefinition(r.Properties, r.Left, relSF, script, provider, out f))
                        fksList.Add(f);

                    if (string.IsNullOrEmpty(f.constraintName))
                        f.constraintName = "FK_" + rel.SourceFragment.Name.Trim(']','[') + "_" + r.Entity.Name;

                    if (names.Contains(f.constraintName))
                    {
                        f.constraintName += "_" + names.Count(item => item.StartsWith(f.constraintName));
                    }
                    names.Add(f.constraintName);

                    if (CreateFKDefinition(r.Properties, r.Right, relSF, script, provider, out f))
                        fksList.Add(f);

                    if (string.IsNullOrEmpty(f.constraintName))
                        f.constraintName = "FK_" + rel.SourceFragment.Name.Trim(']', '[') + "_" + r.Entity.Name;

                    if (names.Contains(f.constraintName))
                    {
                        f.constraintName += "_" + names.Count(item => item.StartsWith(f.constraintName));
                    }
                    names.Add(f.constraintName);
                }
                else if (rel is RelationDefinition)
                {
                    RelationDefinition r = rel as RelationDefinition;

                    FKDefinition f;
                    if (CreateFKDefinition(r.Left.Properties, r.Left, relSF, script, provider, out f))
                        fksList.Add(f);

                    if (string.IsNullOrEmpty(f.constraintName))
                        f.constraintName = "FK_" + rel.SourceFragment.Name.Trim(']', '[') + "_" + r.Left.Entity.Name;

                    if (names.Contains(f.constraintName))
                    {
                        f.constraintName += "_" + names.Count(item => item.StartsWith(f.constraintName));
                    }
                    names.Add(f.constraintName);

                    if (CreateFKDefinition(r.Right.Properties, r.Right, relSF, script, provider, out f))
                        fksList.Add(f);

                    if (string.IsNullOrEmpty(f.constraintName))
                        f.constraintName = "FK_" + rel.SourceFragment.Name.Trim(']', '[') + "_" + r.Right.Entity.Name;

                    if (names.Contains(f.constraintName))
                    {
                        f.constraintName += "_" + names.Count(item => item.StartsWith(f.constraintName));
                    }
                    names.Add(f.constraintName);
                }
                else
                    throw new NotSupportedException(rel.GetType().ToString());

                if (fksList.Count > 0)
                {
                    if (!hdr)
                    {
                        script.AppendLine("--Creating foreign keys");
                        hdr = true;
                    }
                    provider.GenerateCreateFKsScript(rel.SourceFragment, fksList, script);
                }
            }
        }

        private static bool CreateFKDefinition(IEnumerable<ScalarPropertyDefinition> fpk,
            SelfRelationTarget rt, SourceFragmentDefinition relSF, StringBuilder script, ISourceProvider provider,
            out FKDefinition f)
        {
            f = new FKDefinition()
            {
                cols = rt.FieldName,
                refCols = fpk.Select(item => item.SourceFieldExpression).ToArray(),
                refTbl = fpk.First().SourceFragment
            };

            if (relSF != null)
            {
                SourceConstraint fk = relSF.Constraints.SingleOrDefault(item => 
                    item.ConstraintType == SourceConstraint.ForeignKeyConstraintTypeName &&
                    rt.FieldName.All(p=>item.SourceFields.Any(pkf => pkf.SourceFieldExpression == p))
                );

                if (fk != null)
                {
                    //if (!rt.FieldName.All(item => fk.SourceFields.Any(pkf => pkf.SourceFieldExpression == item)))
                    //    provider.GenerateDropConstraintScript(relSF, fk.ConstraintName, script);
                    //else
                        return false;

                    //f.constraintName = fk.ConstraintName;
                }
            }

            return true;
        }

        private void CreateUniqueConstraints(StringBuilder script, ISourceProvider provider)
        {
            var uks = Model.GetActiveEntities().SelectMany(e=>e.GetActiveProperties())
                .Where(item => item.SourceFragment != null && 
                    item.SourceFragment.Constraints.Any(cns => 
                        cns.ConstraintType == SourceConstraint.UniqueConstraintTypeName));

            if (uks.Count() == 0) return;
            bool hdr = false;

            foreach (SourceFragmentDefinition s in uks.Select(item=>item.SourceFragment).Distinct())
            {
                SourceFragmentDefinition sf = s;

                var targetSF = SourceView.GetSourceFragments().SingleOrDefault(item =>
                    item.Name == sf.Name && item.Selector == sf.Selector);

                const bool isPK = false;
                List<string> names = new List<string>();
                foreach (SourceConstraint constraint in s.Constraints.Where(item =>
                    item.ConstraintType == SourceConstraint.UniqueConstraintTypeName))
                {
                    if (targetSF == null || !targetSF.Constraints.Any(item => constraint.SourceFields.All(fld =>
                        item.SourceFields.Any(sfld=>sfld.SourceFieldExpression == fld.SourceFieldExpression))))
                    {
                        var tablePKs = uks.Where(item => item.SourceFragment == sf);
                        List<PropDefinition> tableProps = new List<PropDefinition>();
                        foreach (PropertyDefinition prop in tablePKs)
                        {
                            if (prop is ScalarPropertyDefinition)
                            {
                                ScalarPropertyDefinition p = prop as ScalarPropertyDefinition;
                                if (constraint.SourceFields.Any(item=>item.SourceFieldExpression == p.SourceFieldExpression))
                                    tableProps.Add(new PropDefinition { Attr = prop.Attributes, Field = p.SourceField, PropType = prop.PropertyType });
                            }
                            else
                            {
                                EntityPropertyDefinition p = prop as EntityPropertyDefinition;
                                foreach (EntityPropertyDefinition.SourceField field in p.SourceFields)
                                {
                                    if (constraint.SourceFields.Any(item=>item.SourceFieldExpression == field.SourceFieldExpression))
                                        tableProps.Add(new PropDefinition { Attr = Field2DbRelations.None, Field = field, 
                                            PropType = prop.PropertyType.Entity.GetProperties().Single(pr => pr.PropertyAlias == field.PropertyAlias).PropertyType }
                                        );
                                }
                            }
                        }
                        string constraintName = "UK_" + sf.Name.Trim(']', '[');
                        if (names.Contains(constraintName))
                        {
                            constraintName += "_" + names.Count(item => item.StartsWith(constraintName));
                        }

                        if (!hdr)
                        {
                            script.AppendLine("--Creating unique constraints");
                            hdr = true;
                        }

                        provider.GenerateCreatePKScript(tableProps, constraintName, script, isPK,
                            tablePKs.First().Entity.GetPkProperties().Count() == 0);

                        names.Add(constraintName);
                    }
                }
            }
        }

        private void CreatePrimaryKeys(StringBuilder script, ISourceProvider provider)
        {
            var pks = Model.GetActiveEntities().SelectMany(item =>item.GetPkProperties());
            var rels = Model.GetActiveRelations().Where(item => item.Constraint != RelationConstraint.None);
            if (pks.Count() == 0 && rels.Count() == 0) return;

            bool hdr = false;

            foreach (SourceFragmentDefinition s in pks.Select(item => item.SourceFragment).Distinct())
            {
                SourceFragmentDefinition sf = s;

                var targetSF = SourceView.GetSourceFragments().SingleOrDefault(item =>
                     item.Name == sf.Name && item.Selector == sf.Selector);

                var tablePKs = pks.Where(item => item.SourceFragment == sf);
                //string constraintName = null;
                const bool isPK = true;

                if (targetSF != null)
                {
                    SourceConstraint pk = targetSF.Constraints.SingleOrDefault(item => item.ConstraintType == SourceConstraint.PrimaryKeyConstraintTypeName);
                    //if (pk == null)
                    //{
                    //    isPK = false;
                    //    pk = targetSF.Constraints.SingleOrDefault(item => item.ConstraintType == SourceConstraint.UniqueConstraintTypeName);
                    //}

                    if (pk != null)
                    {
                        if (tablePKs.All(item=>pk.SourceFields.Any(pkf=>pkf.SourceFieldExpression == item.SourceFieldExpression)))
                            continue;
                    }
                    //else
                    //    isPK = true;
                }

                //if (string.IsNullOrEmpty(constraintName))
                //{
                //    constraintName = "PK_" + sf.Name.Trim(']', '[');
                //}
                if (!hdr)
                {
                    script.AppendLine("--Creating primary keys");
                    hdr = true;
                }

                provider.GenerateCreatePKScript(tablePKs.Select(item=>new PropDefinition{Field = item.SourceField, Attr = item.Attributes, PropType = item.PropertyType}), 
                    "PK_" + sf.Name.Trim(']', '['), script, isPK, true);
            }

            foreach (RelationDefinitionBase rel in rels)
            {
                var targetSF = SourceView.GetSourceFragments().SingleOrDefault(item =>
                     item.Name == rel.SourceFragment.Name && item.Selector == rel.SourceFragment.Selector);

                bool isPK = rel.Constraint==RelationConstraint.PrimaryKey;

                if (targetSF != null)
                {
                    if (isPK)
                    {
                        SourceConstraint pk = targetSF.Constraints.SingleOrDefault(item =>
                            item.ConstraintType == SourceConstraint.PrimaryKeyConstraintTypeName);
                        if (pk != null)
                        {
                            if (rel.Left.FieldName.Union(rel.Right.FieldName).All(item => pk.SourceFields.Any(pkf => pkf.SourceFieldExpression == item)))
                                continue;
                        }
                    }
                    else
                    {
                        if (targetSF.Constraints.Any(item =>
                            item.ConstraintType == SourceConstraint.UniqueConstraintTypeName &&
                            rel.Left.FieldName.Union(rel.Right.FieldName).All(fld => item.SourceFields.Any(pkf => pkf.SourceFieldExpression == fld))
                            ))
                            continue;
                    }
                }

                if (!hdr)
                {
                    script.AppendLine("--Creating primary keys");
                    hdr = true;
                }

                provider.GenerateCreatePKScript(rel.Left.FieldName.Union(rel.Right.FieldName)
                    .Select(item=>new PropDefinition{Field = new SourceFieldDefinition(rel.SourceFragment,item)})
                    , isPK?"PK_":"UQ_" + rel.SourceFragment.Name.Trim(']', '['), script, isPK, true);
            }
        }

        private void CreateTables(StringBuilder script, IEnumerable<PropertyDefinition> props, ISourceProvider provider, bool unicodeStrings)
        {
            bool hdr = false;
            
            foreach (SourceFragmentDefinition s in props.Select(item => item.SourceFragment).Distinct())
            {
                SourceFragmentDefinition sf = s;

                var targetSF = SourceView.GetSourceFragments().SingleOrDefault(item => 
                     item.Name == sf.Name && item.Selector == sf.Selector);

                if (targetSF == null)
                {
                    if (!hdr)
                    {
                        script.AppendLine("--Creating tables");
                        hdr = true;
                    }
                    provider.GenerateCreateScript(props.Where(item => item.SourceFragment == sf), script, unicodeStrings);
                }
            }

            foreach (RelationDefinitionBase rel in Model.GetActiveRelations())
            {
                if (!SourceView.GetSourceFragments().Any(item =>
                    item.Name == rel.SourceFragment.Name && item.Selector == rel.SourceFragment.Selector))
                {
                    if (!hdr)
                    {
                        script.AppendLine("--Creating tables");
                        hdr = true;
                    }
                    provider.GenerateCreateScript(rel, script, unicodeStrings);
                }
            }
        }
    }
}
