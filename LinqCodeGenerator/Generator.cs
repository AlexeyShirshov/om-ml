using System.Linq;
using WXML.CodeDom;
using WXML.Model;
using WXML.CodeDom.CodeDomExtensions;
using LinqToCodedom;
using LinqToCodedom.Extensions;
using System.CodeDom;
using System.Reflection;
using LinqToCodedom.Generator;
using System.Data;
using WXML.Model.Descriptors;
using System;
using Microsoft.VisualBasic;
using Microsoft.CSharp;

namespace LinqCodeGenerator
{
    public class LinqCodeDomGenerator
    {
        private readonly WXMLModel _ormObjectsDefinition;
        private readonly WXMLCodeDomGeneratorSettings _settings;
        private Func<string, string> _validId;

        public LinqCodeDomGenerator(WXMLModel ormObjectsDefinition, WXMLCodeDomGeneratorSettings settings)
        {
            _ormObjectsDefinition = ormObjectsDefinition;
            _settings = settings;
        }

        protected WXMLCodeDomGeneratorSettings Settings
        {
            get { return _settings; }
        }

        public WXMLModel Model
        {
            get
            {
                return _ormObjectsDefinition;
            }
        }

        #region Generator

        private CodeDomGenerator _GenerateCode(LinqToCodedom.CodeDomGenerator.Language language)
        {
            if (Model.LinqSettings == null)
                throw new WXMLException("LinqContext is not specified in model");

            if (language == CodeDomGenerator.Language.CSharp)
                _validId = new CSharpCodeProvider().CreateValidIdentifier;
            else if (language == CodeDomGenerator.Language.VB)
                _validId = new VBCodeProvider().CreateValidIdentifier;
            else
                throw new NotImplementedException(language.ToString());

            var c = new CodeDomGenerator {RequireVariableDeclaration = true, AllowLateBound = false};

            var ns = c.AddNamespace(Model.Namespace)
                .Imports("System")
                .Imports("System.Collections.Generic")
                .Imports("System.ComponentModel")
                .Imports("System.Data")
                .Imports("System.Data.Linq")
                .Imports("System.Data.Linq.Mapping")
                .Imports("System.Linq")
                .Imports("System.Linq.Expressions")
                .Imports("System.Reflection");

            var ctx = ns.AddClass(Model.LinqSettings.ContextName).Inherits(typeof(System.Data.Linq.DataContext));
            ctx.IsPartial = true;

            ctx.AddField(typeof(System.Data.Linq.Mapping.MappingSource), MemberAttributes.Private | MemberAttributes.Static, "mappingSource",
                () => new System.Data.Linq.Mapping.AttributeMappingSource());

            AddPartialMethods(ctx);

            AddCtors(ctx);

            AddProps(ctx);

            AddEntities(ns, c, language);

            return c;
        }

        private void AddEntities(CodeNamespace ns, CodeDomGenerator c, LinqToCodedom.CodeDomGenerator.Language language)
        {
            var namespaces = (from e in Model.GetActiveEntities()
                              where !string.IsNullOrEmpty(e.EntitySpecificNamespace)
                              select e.EntitySpecificNamespace)
                              .Distinct()
                              .Select(n => new { name = n, ns = c.AddNamespace(n) })
                              .ToArray();

            foreach(EntityDefinition e in Model.GetActiveEntities())
            {
                CodeNamespace ens = ns;
                var nns = namespaces.SingleOrDefault(item => item.name == e.EntitySpecificNamespace);
                if (nns != null)
                    ens = nns.ns;

                FillEntity(ens, e, language);
            }

            int i = 1;
            foreach(RelationDefinitionBase rel in Model.ActiveRelations)
            {
                EntityDefinition e = new EntityDefinition("relationEntity" + i, 
                    GetName(rel.SourceFragment.Name), ns.Name);
                
                e.AddSourceFragment(new SourceFragmentRefDefinition(rel.SourceFragment));
                
                if (rel is SelfRelationDescription)
                {
                    SelfRelationDescription r = rel as SelfRelationDescription;
                    CreatePropFromRel(e, r.SourceFragment, r.Entity, r.EntityProperties, r.Direct);
                    CreatePropFromRel(e, r.SourceFragment, r.Entity, r.EntityProperties, r.Reverse);
                }
                else if (rel is RelationDefinition)
                {
                    RelationDefinition r = rel as RelationDefinition;
                    CreatePropFromRel(e, r.SourceFragment, r.Left.Entity, r.Left.EntityProperties, r.Left);
                    CreatePropFromRel(e, r.SourceFragment, r.Right.Entity, r.Right.EntityProperties, r.Right);
                }
                else
                    throw new NotSupportedException(rel.GetType().ToString());

                FillEntity(ns, e, language/*, (cls)=>
                {
                    if (rel is SelfRelationDescription)
                    {
                        SelfRelationDescription r = rel as SelfRelationDescription;
                        throw new NotImplementedException();
                    }
                    else if (rel is RelationDefinition)
                    {
                        RelationDefinition r = rel as RelationDefinition;

                        string ename = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(r.Left.Entity, true);

                        CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntityRef<>), ename);

                        cls.AddField(ft,
                             WXMLCodeDomGenerator.GetMemberAttribute(AccessLevel.Private),
                             new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(r.Left.Entity.Name)
                        );

                        ename = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(r.Right.Entity, true);

                        ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntityRef<>), ename);

                        cls.AddField(ft,
                             WXMLCodeDomGenerator.GetMemberAttribute(AccessLevel.Private),
                             new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(r.Right.Entity.Name)
                        );
                    }
                },
                (cls)=>
                {
                    if (rel is SelfRelationDescription)
                    {
                        SelfRelationDescription r = rel as SelfRelationDescription;
                        throw new NotImplementedException();
                    }
                    else if (rel is RelationDefinition)
                    {
                        RelationDefinition r = rel as RelationDefinition;

                        string ename = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(r.Left.Entity, true);

                        string efieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(r.Left.Entity.Name);

                        string name = string.IsNullOrEmpty(r.Left.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(GetName(r.SourceFragment.Name))
                            : r.Left.AccessorName;

                        CreateAssociation(ename, cls, CodeDom.TypeRef(ename), r.Left.Entity.Name, 
                            efieldName, name, e, r.Left);

                        ename = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(r.Right.Entity, true);

                        efieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(r.Right.Entity.Name);

                        name = string.IsNullOrEmpty(r.Right.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(GetName(r.SourceFragment.Name))
                            : r.Right.AccessorName;

                        CreateAssociation(ename, cls, CodeDom.TypeRef(ename), r.Right.Entity.Name, 
                            efieldName, name, e, r.Left);
                    }
                }, 
                (ctor)=>
                {
                    if (rel is SelfRelationDescription)
                    {
                        SelfRelationDescription r = rel as SelfRelationDescription;
                        throw new NotImplementedException();
                    }
                    else if (rel is RelationDefinition)
                    {
                        RelationDefinition r = rel as RelationDefinition;

                        string ename = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(r.Left.Entity, true);

                        string efieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(r.Left.Entity.Name);

                        ctor.Statements.Add(Emit.assignField(efieldName, ()=>CodeDom.@default(CodeDom.TypeRef(typeof(System.Data.Linq.EntityRef<>), ename))));

                        ename = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(r.Right.Entity, true);

                        efieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(r.Right.Entity.Name);

                        ctor.Statements.Add(Emit.assignField(efieldName, () => CodeDom.@default(CodeDom.TypeRef(typeof(System.Data.Linq.EntityRef<>), ename))));
                    }
                }*/);

                i++;
            }
        }

        //private void CreateAssociation(string ename, CodeTypeDeclaration cls, CodeTypeReference ft, 
        //    string propName, string efieldName, string name, EntityDefinition e, LinkTarget target)
        //{
        //    var eprop = cls.AddProperty(ft,
        //        MemberAttributes.Public | MemberAttributes.Final, propName,
        //        CodeDom.CombineStmts(
        //            Emit.@return(() => CodeDom.Field(CodeDom.@this.Field(efieldName), "Entity"))
        //            ),
        //        Emit.declare(ft, "previousValue", () => CodeDom.Field(CodeDom.@this.Field(efieldName), "Entity")),
        //        Emit.@if(() => !Equals(CodeDom.@this.Field(efieldName), CodeDom.VarRef("value")) ||
        //                       !CodeDom.Field<bool>(CodeDom.@this.Field(efieldName), "HasLoadedOrAssignedValue"),
        //                 Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
        //                 Emit.@if((object previousValue) => !Equals(previousValue, null),
        //                          Emit.assignProperty<object>(CodeDom.GetExpression(() => CodeDom.@this.Field(efieldName)), "Entity", () => null),
        //                          Emit.stmt(() => CodeDom.VarRef("previousValue").Property(name).Call("Remove")(CodeDom.@this))
        //                     ),
        //                 Emit.assignProperty(CodeDom.GetExpression(() => CodeDom.@this.Field(efieldName)), "Entity", (object value) => value),
        //                 Emit.ifelse((object value) => !Equals(value, null),
        //                             CodeDom.CombineStmts(
        //                                 Emit.stmt(() => CodeDom.VarRef("value").Property(name).Call("Add")(CodeDom.@this))
        //                                 ).Union(target.FieldName.Select(field => Emit.assignField(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(GetName(field)), () => CodeDom.VarRef("value").Property(target.Entity.GetPkProperties().Skip(target.FieldName.IndexOf(field)).First().Name))).Cast<CodeStatement>()).ToArray(),
        //                             target.FieldName.Select(field => Emit.assignField(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(GetName(field)), () => CodeDom.@default(target.Entity.GetPkProperties().Skip(target.FieldName.IndexOf(field)).First().PropertyType.ToCodeType(_settings)))).ToArray()
        //                     ),
        //                 Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanged")(propName))
        //            )
        //    );

        //    var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.AssociationAttribute));

        //    Define.InitAttributeArgs(() => new
        //       {
        //           Storage = efieldName,
        //           Name = ename + "_" + e.Name,
        //           ThisKey = string.Join(",", target.FieldName.Select(item=>GetName(item)).ToArray()),
        //           OtherKey = string.Join(",", target.Properties.Select(item => GetName(item.SourceFieldExpression)).ToArray()),
        //           IsForeignKey = true
        //       }, attr);

        //    eprop.AddAttribute(attr);
        //}

        private void CreatePropFromRel(EntityDefinition e, SourceFragmentDefinition sf, 
            EntityDefinition relEntity, string [] props, SelfRelationTarget target)
        {
            TypeDefinition t = Model.GetTypes().SingleOrDefault(item => item.IsEntityType && item.Entity == relEntity);
            if (t == null)
                t = new TypeDefinition("t"+relEntity.Name, relEntity);

            EntityPropertyDefinition prop = new EntityPropertyDefinition(relEntity.Name, null, Field2DbRelations.PK,
                null, AccessLevel.Private, AccessLevel.Public, t, sf, e
            );
            e.AddProperty(prop);
            for (int j = 0; j < target.FieldName.Length; j++)
            {
                //string prop = GetName(target.FieldName[j]);
                ScalarPropertyDefinition pk = (ScalarPropertyDefinition)relEntity.GetActiveProperties().Single(item => item.PropertyAlias == props[j]);
                //e.AddProperty(new ScalarPropertyDefinition(e, prop, prop, Field2DbRelations.PK,
                //    null, pk.PropertyType, new SourceFieldDefinition(
                //        sf, target.FieldName[j], pk.SourceTypeSize, false,
                //        pk.SourceType, false, null
                //    ), AccessLevel.Private, AccessLevel.Public)
                //);
                
                prop.AddSourceField(props[j], target.FieldName[j], null,
                    pk.SourceType, pk.SourceTypeSize, false, null
                );
            }
        }

        private string GetName(string p)
        {
            var n = p.Trim('[', ']');
            n = _validId(n);
            if (char.IsDigit(n[0]))
                n = "_" + n;
            return n;
        }

        private void FillEntity(CodeNamespace ns, EntityDefinition e, CodeDomGenerator.Language language)
        {
            FillEntity(ns, e, language, null, null, null);
        }

        private void FillEntity(CodeNamespace ns, EntityDefinition e, CodeDomGenerator.Language language,
            Action<CodeTypeDeclaration> addFields, Action<CodeTypeDeclaration> addProps,
            Action<CodeConstructor> addCtor)
        {
            CodeTypeDeclaration cls = ns.AddClass(/*new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(e, true)*/ e.Name)
                .Inherits(typeof(System.ComponentModel.INotifyPropertyChanging))
                .Inherits(typeof(System.ComponentModel.INotifyPropertyChanged));

            SourceFragmentDefinition tbl = e.GetSourceFragments().First();

            var c = Define.Attribute(typeof(System.Data.Linq.Mapping.TableAttribute));
            cls.AddAttribute(c);
            Define.InitAttributeArgs(() => new { Name = string.IsNullOrEmpty(tbl.Selector) ? tbl.Name : tbl.Selector + "." + tbl.Name }, c);

            cls.IsPartial = true;

            AddFields(e, cls, addFields);

            AddEntityPartialMethods(cls, e);

            CodeConstructor ctor = cls.AddCtor();
            foreach (EntityPropertyDefinition p in e.GetActiveProperties().OfType<EntityPropertyDefinition>())
            {
                CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntityRef<>), p.PropertyType.ToCodeType(_settings));
                ctor.Statements.Add(Emit.assignField(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.Name),
                    ()=>CodeDom.@default(ft)
                ));
            }

            foreach (EntityRelationDefinition relation in e.EntityRelations.Where(item=>!item.Disabled))
            {
                string name = string.IsNullOrEmpty(relation.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(relation.Entity.Name)
                    : relation.AccessorName;

                string clsName = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relation.Entity, true);
                CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntitySet<>), clsName);
                string fldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(name);
                ctor.Statements.Add(Emit.assignField(fldName,
                    () => CodeDom.@new(ft, 
                        new CodeDelegateCreateExpression(
                            new CodeTypeReference("System.Action", new CodeTypeReference(clsName)), new CodeThisReferenceExpression(), "attach"+fldName),
                        new CodeDelegateCreateExpression(
                            new CodeTypeReference("System.Action", new CodeTypeReference(clsName)), new CodeThisReferenceExpression(), "detach" + fldName)
                    )
                ));
            }

            foreach (RelationDefinition rel in Model.ActiveRelations.OfType<RelationDefinition>().Where(item => item.Left.Entity == e || item.Right.Entity == e))
            {
                LinkTarget t = rel.Left;
                if (t.Entity != e)
                    t = rel.Right;

                string ename = GetName(rel.SourceFragment.Name);

                EntityDefinition re = new EntityDefinition("relationEntity",
                    GetName(rel.SourceFragment.Name), ns.Name);

                string name = string.IsNullOrEmpty(t.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(ename)
                    : t.AccessorName;

                string clsName = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(re, true);
                CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntitySet<>), clsName);
                string fldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(name);
                ctor.Statements.Add(Emit.assignField(fldName,
                    () => CodeDom.@new(ft,
                        new CodeDelegateCreateExpression(
                            new CodeTypeReference("System.Action", new CodeTypeReference(clsName)), new CodeThisReferenceExpression(), "attach" + fldName),
                        new CodeDelegateCreateExpression(
                            new CodeTypeReference("System.Action", new CodeTypeReference(clsName)), new CodeThisReferenceExpression(), "detach" + fldName)
                    )
                ));
            }

            foreach (SelfRelationDescription rel in Model.ActiveRelations.OfType<SelfRelationDescription>().Where(item => item.Entity == e))
            {
                throw new NotSupportedException("M2M relation to self is not supported yet");
            }

            if (addCtor != null)
                addCtor(ctor);

            ctor.Statements.Add(Emit.stmt(() => CodeDom.Call(null, "OnCreated")));

            AddProperties(e, cls, addProps);

            cls.AddEvent(typeof(System.ComponentModel.PropertyChangingEventHandler), MemberAttributes.Public, "PropertyChanging")
                .Implements(typeof(System.ComponentModel.INotifyPropertyChanging));

            cls.AddEvent(typeof(System.ComponentModel.PropertyChangedEventHandler), MemberAttributes.Public, "PropertyChanged")
                .Implements(typeof(System.ComponentModel.INotifyPropertyChanged));

            AddMethods(cls, language, e);
        }

        private void AddMethods(CodeTypeDeclaration cls, CodeDomGenerator.Language language, EntityDefinition e)
        {
            string evntName = "PropertyChanging";
            if (language == CodeDomGenerator.Language.VB)
                evntName+="Event";

            cls.AddMethod(MemberAttributes.Family, () => "SendPropertyChanging",
              Emit.@if(() => !CodeDom.Call<bool>("ReferenceEquals")(CodeDom.@this.Property(evntName), null),
                       Emit.stmt(() => CodeDom.@this.Raise("PropertyChanging")(CodeDom.@this, CodeDom.VarRef("emptyChangingEventArgs")))
                  )
            );

            evntName = "PropertyChanged";
            if (language == CodeDomGenerator.Language.VB)
                evntName += "Event"; 
            
            cls.AddMethod(MemberAttributes.Family, (string propertyName) => "SendPropertyChanged",
              Emit.@if(() => !CodeDom.Call<bool>("ReferenceEquals")(CodeDom.@this.Property(evntName), null),
                       Emit.stmt((string propertyName) => CodeDom.@this.Raise("PropertyChanged")(CodeDom.@this, new System.ComponentModel.PropertyChangedEventArgs(propertyName)))
                  )
            );

            foreach (EntityRelationDefinition r in e.EntityRelations.Where(item => !item.Disabled))
            {
                EntityRelationDefinition relation = r;
                string name = string.IsNullOrEmpty(relation.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(relation.Entity.Name)
                    : relation.AccessorName;

                string clsName = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relation.Entity, true);

                string fldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(name);

                string propName = string.IsNullOrEmpty(relation.PropertyAlias)?
                    relation.Entity.GetActiveProperties().OfType<EntityPropertyDefinition>().Single(item => item.PropertyType.Entity.Identifier == relation.SourceEntity.Identifier).Name :
                    relation.Entity.GetActiveProperties().OfType<EntityPropertyDefinition>().Single(item => item.PropertyAlias == relation.PropertyAlias).Name;

                cls.AddMethod(MemberAttributes.Private, (DynType entity) => entity.SetType(clsName) + "attach" + fldName,
                    Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
                    Emit.assignProperty(CodeDom.GetExpression(()=>CodeDom.VarRef("entity")),propName, ()=>CodeDom.@this)
                );

                cls.AddMethod(MemberAttributes.Private, (DynType entity) => entity.SetType(clsName) + "detach" + fldName,
                    Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
                    Emit.assignProperty<object>(CodeDom.GetExpression(() => CodeDom.VarRef("entity")), propName, () => null)
                );
            }

            foreach (RelationDefinition rel in Model.ActiveRelations.OfType<RelationDefinition>().Where(item => item.Left.Entity == e || item.Right.Entity == e))
            {
                LinkTarget t = rel.Left;
                if (t.Entity != e)
                    t = rel.Right;

                string ename = GetName(rel.SourceFragment.Name);

                EntityDefinition re = new EntityDefinition("relationEntity",
                    GetName(rel.SourceFragment.Name), null/*Model.Namespace*/);

                string name = string.IsNullOrEmpty(t.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(ename)
                    : t.AccessorName;

                string clsName = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(re, true);
                string fldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(name);

                string propName = t.Entity.Name;

                cls.AddMethod(MemberAttributes.Private, (DynType entity) => entity.SetType(clsName) + "attach" + fldName,
                    Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
                    Emit.assignProperty(CodeDom.GetExpression(() => CodeDom.VarRef("entity")), propName, () => CodeDom.@this)
                );

                cls.AddMethod(MemberAttributes.Private, (DynType entity) => entity.SetType(clsName) + "detach" + fldName,
                    Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
                    Emit.assignProperty<object>(CodeDom.GetExpression(() => CodeDom.VarRef("entity")), propName, () => null)
                );
            }

            foreach (SelfRelationDescription rel in Model.ActiveRelations.OfType<SelfRelationDescription>().Where(item => item.Entity == e))
            {
                throw new NotSupportedException("M2M relation to self is not supported yet");
            }
        }

        private void AddProperties(EntityDefinition e, CodeTypeDeclaration cls, Action<CodeTypeDeclaration> addProps)
        {
            foreach (PropertyDefinition p_ in e.GetActiveProperties())
            {
                if (p_ is ScalarPropertyDefinition)
                {
                    ScalarPropertyDefinition p = p_ as ScalarPropertyDefinition;
                    
                    var fieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.Name);

                    var prop = cls.AddProperty(p.PropertyType.ToCodeType(_settings),
                       WXMLCodeDomGenerator.GetMemberAttribute(p.PropertyAccessLevel) | MemberAttributes.Final, p.Name,
                       CodeDom.CombineStmts(
                           Emit.@return(() => CodeDom.@this.Field(fieldName))
                           ),
                       //set
                       Emit.@if(() => !Object.Equals(CodeDom.@this.Field(fieldName), CodeDom.VarRef("value")),
                                Emit.stmt(() => CodeDom.@this.Call("On" + p.Name + "Changing")(CodeDom.VarRef("value"))),
                                Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
                                Emit.assignField(fieldName, () => CodeDom.VarRef("value")),
                                Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanged")(p.Name)),
                                Emit.stmt(() => CodeDom.@this.Call("On" + p.Name + "Changed"))
                           )
                    );

                    var attr = AddPropertyAttribute(p, fieldName);

                    prop.AddAttribute(attr);
                }
                else if(p_ is EntityPropertyDefinition)
                {
                    EntityPropertyDefinition p = p_ as EntityPropertyDefinition;
                    
                    var efieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.Name);

                    foreach (EntityPropertyDefinition.SourceField field in p.SourceFields)
                    {
                        string propName = GetName(field.SourceFieldExpression);
                        var fieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propName);

                        var prop = cls.AddProperty(GetSourceFieldType(p, field).ToCodeType(_settings),
                           WXMLCodeDomGenerator.GetMemberAttribute(AccessLevel.Public) | MemberAttributes.Final, propName,
                           CodeDom.CombineStmts(
                               Emit.@return(() => CodeDom.@this.Field(fieldName))
                               ),
                           //set
                           Emit.@if(() => !Equals(CodeDom.@this.Field(fieldName), CodeDom.VarRef("value")),
                                Emit.@if(()=>CodeDom.Property<bool>(CodeDom.@this.Field(efieldName), "HasLoadedOrAssignedValue"),
                                    Emit.@throw(()=>new System.Data.Linq.ForeignKeyReferenceAlreadyHasValueException())
                                ),
                                Emit.stmt(() => CodeDom.@this.Call("On" + propName + "Changing")(CodeDom.VarRef("value"))),
                                Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
                                Emit.assignField(fieldName, () => CodeDom.VarRef("value")),
                                Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanged")(propName)),
                                Emit.stmt(() => CodeDom.@this.Call("On" + propName + "Changed"))
                           )
                        );

                        var attr = AddPropertyAttribute(field, fieldName, p.HasAttribute(Field2DbRelations.PK));

                        prop.AddAttribute(attr);
                    }

                    CodeTypeReference pt = p.PropertyType.ToCodeType(_settings);

                    var relationProp = p.PropertyType.Entity.EntityRelations.Where(item => !item.Disabled)
                        .SingleOrDefault(item => item.PropertyAlias == p.PropertyAlias);

                    string name = null;

                    if (relationProp == null)
                        relationProp = p.PropertyType.Entity.EntityRelations.Where(item => !item.Disabled)
                            .SingleOrDefault(item => item.Entity.Identifier == p.Entity.Identifier);

                    if (relationProp == null)
                    {
                        var rel = Model.ActiveRelations.OfType<RelationDefinition>().SingleOrDefault(item => item.Left.Entity == p.PropertyType.Entity || item.Right.Entity == p.PropertyType.Entity);

                        LinkTarget lt = rel.Left;
                        if (lt.Entity != p.PropertyType.Entity)
                            lt = rel.Right;

                        name = !string.IsNullOrEmpty(lt.AccessorName) ? lt.AccessorName
                            : WXMLCodeDomGeneratorNameHelper.GetMultipleForm(e.Name);
                    }
                    else
                        name = !string.IsNullOrEmpty(relationProp.AccessorName) ? relationProp.AccessorName
                            : WXMLCodeDomGeneratorNameHelper.GetMultipleForm(relationProp.Entity.Name);

                    var eprop = cls.AddProperty(pt,
                        WXMLCodeDomGenerator.GetMemberAttribute(p.PropertyAccessLevel) | MemberAttributes.Final, p.Name,
                        CodeDom.CombineStmts(
                            Emit.@return(() => CodeDom.Field(CodeDom.@this.Field(efieldName), "Entity"))
                            ),
                        Emit.declare(pt, "previousValue", ()=> CodeDom.Field(CodeDom.@this.Field(efieldName), "Entity")),
                        Emit.@if(() => !Equals(CodeDom.@this.Field(efieldName), CodeDom.VarRef("value")) ||
                            !CodeDom.Field<bool>(CodeDom.@this.Field(efieldName), "HasLoadedOrAssignedValue"),
                                Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanging")),
                                Emit.@if((object previousValue)=>!Equals(previousValue, null),
                                    Emit.assignProperty<object>(CodeDom.GetExpression(()=>CodeDom.@this.Field(efieldName)), "Entity", ()=>null),
                                    Emit.stmt(() => CodeDom.VarRef("previousValue").Property(name).Call("Remove")(CodeDom.@this))
                                ),
                                Emit.assignProperty(CodeDom.GetExpression(()=>CodeDom.@this.Field(efieldName)), "Entity", (object value)=>value),
                                Emit.ifelse((object value) => !Equals(value, null),
                                    CodeDom.CombineStmts(
                                        Emit.stmt(()=>CodeDom.VarRef("value").Property(name).Call("Add")(CodeDom.@this))
                                    ).Union(p.SourceFields.Select(field => Emit.assignField(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(GetName(field.SourceFieldExpression)), () => CodeDom.VarRef("value").Property(p.PropertyType.Entity.GetProperty(field.PropertyAlias).Name))).Cast<CodeStatement>()).ToArray(),
                                    p.SourceFields.Select(field => Emit.assignField(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(GetName(field.SourceFieldExpression)), () => CodeDom.@default(GetSourceFieldType(p, field).ToCodeType(_settings)))).ToArray()
                                 ),
                                 Emit.stmt(() => CodeDom.@this.Call("SendPropertyChanged")(p.Name))
                            )
                    );

                    eprop.AddAttribute(AddPropertyAttribute(p, efieldName));
                }
                else
                    throw new NotImplementedException();
            }

            if (addProps != null)
                addProps(cls);

            //relations
            foreach (EntityRelationDefinition r in e.EntityRelations.Where(item => !item.Disabled))
            {
                EntityRelationDefinition relation = r;

                string name = string.IsNullOrEmpty(relation.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(relation.Entity.Name)
                    : relation.AccessorName;

                string clsName = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relation.Entity, true);

                CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntitySet<>), clsName);

                string fldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(name);

                CodeMemberProperty prop = cls.AddProperty(ft, MemberAttributes.Public | MemberAttributes.Final, name, 
                    CodeDom.CombineStmts(
                        Emit.@return(() => CodeDom.@this.Field(fldName))
                    ),
                    Emit.stmt((object value)=>CodeDom.Call(CodeDom.@this.Field(fldName),"Assign")(value))
                );

                var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.AssociationAttribute));

                EntityPropertyDefinition p = string.IsNullOrEmpty(relation.PropertyAlias) ?
                    relation.Entity.GetActiveProperties().OfType<EntityPropertyDefinition>().Single(item => item.PropertyType.Entity.Identifier == relation.SourceEntity.Identifier) :
                    relation.Entity.GetActiveProperties().OfType<EntityPropertyDefinition>().Single(item => item.PropertyAlias == relation.PropertyAlias);

                Define.InitAttributeArgs(() => new { Storage = fldName, Name = 
                    relation.SourceEntity.Name+"_"+relation.Entity.Name,
                    ThisKey=string.Join(",", p.SourceFields.Select(item=>item.SourceFieldExpression).ToArray()),
                    OtherKey = string.Join(",", p.SourceFields.Select(item => item.PropertyAlias).Select(item=>p.PropertyType.Entity.GetProperty(item)).Cast<ScalarPropertyDefinition>().Select(item=>item.SourceFieldExpression).ToArray())
                }, attr);

                prop.AddAttribute(attr);
            }

            foreach (RelationDefinition rel in Model.ActiveRelations.OfType<RelationDefinition>().Where(item => item.Left.Entity == e || item.Right.Entity == e))
            {
                LinkTarget t = rel.Left;
                if (t.Entity != e)
                    t = rel.Right;

                string ename = GetName(rel.SourceFragment.Name);

                EntityDefinition re = new EntityDefinition("relationEntity",
                    GetName(rel.SourceFragment.Name), null/*Model.Namespace*/);

                string name = string.IsNullOrEmpty(t.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(ename)
                    : t.AccessorName;

                string clsName = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(re, true);
                string fldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(name);

                CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntitySet<>), clsName);

                CodeMemberProperty prop = cls.AddProperty(ft, MemberAttributes.Public | MemberAttributes.Final, name,
                    CodeDom.CombineStmts(
                        Emit.@return(() => CodeDom.@this.Field(fldName))
                    ),
                    Emit.stmt((object value) => CodeDom.Call(CodeDom.@this.Field(fldName), "Assign")(value))
                );

                var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.AssociationAttribute));

                Define.InitAttributeArgs(() => new
                {
                    Storage = fldName,
                    Name = e.Name + "_" + ename,
                    ThisKey = string.Join(",", t.Properties.Select(item => GetName(item.SourceFieldExpression)).ToArray()),
                    OtherKey = string.Join(",", t.FieldName.Select(item=>GetName(item)).ToArray())
                }, attr);

                prop.AddAttribute(attr);
            }

            foreach (SelfRelationDescription rel in Model.ActiveRelations.OfType<SelfRelationDescription>().Where(item => item.Entity == e))
            {
                throw new NotSupportedException("M2M relation to self is not supported yet");
            }
        }

        private void AddFields(EntityDefinition e, CodeTypeDeclaration cls, Action<CodeTypeDeclaration> addFields)
        {
            cls.AddField(typeof(System.ComponentModel.PropertyChangingEventArgs), MemberAttributes.Private | MemberAttributes.Static,
                         "emptyChangingEventArgs", () => new System.ComponentModel.PropertyChangingEventArgs(string.Empty));

            WXMLCodeDomGeneratorNameHelper nameHelper = new WXMLCodeDomGeneratorNameHelper(Settings);

            foreach (PropertyDefinition p_ in e.GetActiveProperties())
            {
                if (p_ is ScalarPropertyDefinition)
                {
                    ScalarPropertyDefinition p = p_ as ScalarPropertyDefinition;
                    cls.AddField(p.PropertyType.ToCodeType(_settings),
                                 WXMLCodeDomGenerator.GetMemberAttribute(p.FieldAccessLevel),
                                 nameHelper.GetPrivateMemberName(p.Name));
                }
                else if (p_ is EntityPropertyDefinition)
                {
                    EntityPropertyDefinition p = p_ as EntityPropertyDefinition;
                    foreach (EntityPropertyDefinition.SourceField field in p.SourceFields)
                    {
                        TypeDefinition pt = GetSourceFieldType(p, field);

                        cls.AddField(pt.ToCodeType(_settings),
                            WXMLCodeDomGenerator.GetMemberAttribute(p.FieldAccessLevel),
                            nameHelper.GetPrivateMemberName(GetName(field.SourceFieldExpression)));
                    }

                    CodeTypeReference ft = CodeDom.TypeRef(typeof (System.Data.Linq.EntityRef<>), p.PropertyType.ToCodeType(_settings));

                    cls.AddField(ft,
                         WXMLCodeDomGenerator.GetMemberAttribute(p.FieldAccessLevel),
                         nameHelper.GetPrivateMemberName(p.Name)
                    );
                }
            }

            //add relations

            foreach (EntityRelationDefinition relation in e.EntityRelations.Where(item=>!item.Disabled))
            {
                string name = string.IsNullOrEmpty(relation.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(relation.Entity.Name)
                    : relation.AccessorName;

                string clsName = nameHelper.GetEntityClassName(relation.Entity, true);

                CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntitySet<>), clsName);

                cls.AddField(ft,
                     WXMLCodeDomGenerator.GetMemberAttribute(AccessLevel.Private),
                     nameHelper.GetPrivateMemberName(name)
                );
            }

            foreach (RelationDefinition rel in Model.ActiveRelations.OfType<RelationDefinition>().Where(item => item.Left.Entity == e || item.Right.Entity == e))
            {
                LinkTarget t = rel.Left;
                if (t.Entity != e)
                    t = rel.Right;

                string ename = GetName(rel.SourceFragment.Name);

                EntityDefinition re = new EntityDefinition("relationEntity",
                    GetName(rel.SourceFragment.Name), null/*Model.Namespace*/);

                string name = string.IsNullOrEmpty(t.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(ename)
                    : t.AccessorName;

                string clsName = nameHelper.GetEntityClassName(re, true);

                CodeTypeReference ft = CodeDom.TypeRef(typeof(System.Data.Linq.EntitySet<>), clsName);

                cls.AddField(ft,
                     WXMLCodeDomGenerator.GetMemberAttribute(AccessLevel.Private),
                     nameHelper.GetPrivateMemberName(name)
                );
            }

            foreach (SelfRelationDescription rel in Model.ActiveRelations.OfType<SelfRelationDescription>().Where(item => item.Entity == e))
            {
                throw new NotSupportedException("M2M relation to self is not supported yet");
            }

            if (addFields != null)
                addFields(cls);
        }

        private static TypeDefinition GetSourceFieldType(PropertyDefinition p, EntityPropertyDefinition.SourceField field)
        {
            TypeDefinition pt = null;
            if (string.IsNullOrEmpty(field.SourceType))
            {
                var pr = ((ScalarPropertyDefinition)p.PropertyType.Entity.GetActiveProperties().SingleOrDefault(item=>item.PropertyAlias==field.PropertyAlias));
                pt = GetPropType(pr.SourceType, pr.IsNullable);
            }
            else
                pt = GetPropType(field.SourceType, field.IsNullable);
            return pt;
        }

        private static TypeDefinition GetPropType(string dbType, bool nullable)
        {
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

            Type tp = GetTypeByName(type);
            if (nullable && tp.IsValueType)
                type = String.Format("System.Nullable`1[{0}]", type);

            return new TypeDefinition(id, type);
        }

        private static Type GetTypeByName(string type)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type tp = a.GetType(type, false, true);
                if (tp != null)
                    return tp;
            }
            throw new TypeLoadException("Cannot load type " + type);
        }

        private CodeAttributeDeclaration AddPropertyAttribute(EntityPropertyDefinition p, string fieldName)
        {
            var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.AssociationAttribute));

            Define.InitAttributeArgs(() => new { Storage = fieldName, Name = 
                p.PropertyType.Entity.Name+"_"+p.Entity.Name,
                ThisKey=string.Join(",", p.SourceFields.Select(item=>GetName(item.SourceFieldExpression)).ToArray()),
                OtherKey = string.Join(",", p.SourceFields.Select(item => p.PropertyType.Entity.GetProperty(item.PropertyAlias).Name).ToArray()),
                IsForeignKey=true
            }, attr);

            return attr;
        }

        private static CodeAttributeDeclaration AddPropertyAttribute(ScalarPropertyDefinition p, string fieldName)
        {
            var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.ColumnAttribute));
            string nullable = " NULL";
            if (!p.IsNullable)
                nullable = " NOT NULL";

            if (p.HasAttribute(Field2DbRelations.PrimaryKey))
                nullable += " IDENTITY";

            string size = string.Empty;
            if (p.SourceTypeSize.HasValue)
                size = "(" + p.SourceTypeSize.Value.ToString() + ")";

            bool insertDefault = false;
            if (p.HasAttribute(Field2DbRelations.InsertDefault) || p.HasAttribute(Field2DbRelations.PrimaryKey))
                insertDefault = true;

            System.Data.Linq.Mapping.AutoSync async = System.Data.Linq.Mapping.AutoSync.Default;

            if (p.HasAttribute(Field2DbRelations.SyncInsert) && p.HasAttribute(Field2DbRelations.SyncUpdate))
                async = System.Data.Linq.Mapping.AutoSync.Always;
            else if (p.HasAttribute(Field2DbRelations.SyncInsert))
                async = System.Data.Linq.Mapping.AutoSync.OnInsert;
            else if (p.HasAttribute(Field2DbRelations.SyncUpdate))
                async = System.Data.Linq.Mapping.AutoSync.OnUpdate;

            if (p.HasAttribute(Field2DbRelations.PK))
            {
                Define.InitAttributeArgs(() => new { Storage = fieldName, Name = p.SourceFieldExpression, DbType = p.SourceType + size + nullable, IsPrimaryKey = true, AutoSync = async, IsDbGenerated = insertDefault}, attr);
            }
            else
            {
                Define.InitAttributeArgs(() => new { Storage = fieldName, Name = p.SourceFieldExpression, DbType = p.SourceType + size + nullable, AutoSync = async, IsDbGenerated = insertDefault }, attr);
            }

            return attr;
        }

        private static CodeAttributeDeclaration AddPropertyAttribute(SourceFieldDefinition p, string fieldName,
            bool isPK)
        {
            var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.ColumnAttribute));
            string nullable = " NULL";
            if (!p.IsNullable)
                nullable = " NOT NULL";

            string size = string.Empty;
            if (p.SourceTypeSize.HasValue)
                size = "(" + p.SourceTypeSize.Value.ToString() + ")";

            System.Data.Linq.Mapping.AutoSync async = System.Data.Linq.Mapping.AutoSync.Default;

            if (isPK)
                Define.InitAttributeArgs(() => new { Storage = fieldName, Name = p.SourceFieldExpression, DbType = p.SourceType + size + nullable, AutoSync = async, IsPrimaryKey = true }, attr);
            else
                Define.InitAttributeArgs(() => new { Storage = fieldName, Name = p.SourceFieldExpression, DbType = p.SourceType + size + nullable, AutoSync = async }, attr);

            return attr;
        }

        private void AddEntityPartialMethods(CodeTypeDeclaration cls, EntityDefinition e)
        {
            CodeTypeMember lastMethod = Define.PartialMethod(MemberAttributes.Private, () => "OnLoaded");
            lastMethod.StartDirective("Extensibility Method Definitions");
            cls.AddMember(lastMethod);
            cls.AddMember(Define.PartialMethod(MemberAttributes.Private, (System.Data.Linq.ChangeAction action) => "OnValidate"));
            cls.AddMember(Define.PartialMethod(MemberAttributes.Private, () => "OnCreated"));
            foreach (PropertyDefinition p_ in e.GetActiveProperties())
            {
                if (p_ is ScalarPropertyDefinition)
                {
                    ScalarPropertyDefinition p = p_ as ScalarPropertyDefinition;
                    
                    cls.AddMember(Define.PartialMethod(MemberAttributes.Private, (DynType value) => "On" + p.Name + "Changing" + value.SetType(p.PropertyType.ToCodeType(Settings))));
                    lastMethod = Define.PartialMethod(MemberAttributes.Private, () => "On" + p.Name + "Changed");
                    cls.AddMember(lastMethod);
                }
                else if (p_ is EntityPropertyDefinition)
                {
                    EntityPropertyDefinition p = p_ as EntityPropertyDefinition;
                    foreach (EntityPropertyDefinition.SourceField field in p.SourceFields)
                    {
                        TypeDefinition pt = GetSourceFieldType(p, field);
                        string fldName = GetName(field.SourceFieldExpression);
                        cls.AddMember(Define.PartialMethod(MemberAttributes.Private, (DynType value) => "On" + fldName + "Changing" + value.SetType(pt.ToCodeType(Settings))));
                        lastMethod = Define.PartialMethod(MemberAttributes.Private, () => "On" + fldName + "Changed");
                        cls.AddMember(lastMethod);
                    }
                }

            }
            lastMethod.EndDirective();
        }

        private void AddProps(CodeTypeDeclaration ctx)
        {
            var n = new WXMLCodeDomGeneratorNameHelper(Settings);
            
            foreach (EntityDefinition e in Model.GetActiveEntities())
            {
                CodeTypeReference t = new CodeTypeReference(typeof(System.Data.Linq.Table<>));
                CodeTypeReference et = new CodeTypeReference(n.GetEntityClassName(e, true));
                t.TypeArguments.Add(et);
                ctx.AddGetProperty(t, MemberAttributes.Public | MemberAttributes.Final,
                    WXMLCodeDomGeneratorNameHelper.GetMultipleForm(e.Name),
                    Emit.@return(() => CodeDom.@this.Call("GetTable", et))
                );
            }

            foreach (RelationDefinitionBase rel in Model.ActiveRelations)
            {
                EntityDefinition e = new EntityDefinition("relationEntity",
                    GetName(rel.SourceFragment.Name), null/*Model.Namespace*/);

                CodeTypeReference t = new CodeTypeReference(typeof(System.Data.Linq.Table<>));
                CodeTypeReference et = new CodeTypeReference(n.GetEntityClassName(e, true));

                t.TypeArguments.Add(et);
                ctx.AddGetProperty(t, MemberAttributes.Public | MemberAttributes.Final,
                    WXMLCodeDomGeneratorNameHelper.GetMultipleForm(e.Name),
                    Emit.@return(() => CodeDom.@this.Call("GetTable", et))
                );
            }
        }

        private void AddPartialMethods(CodeTypeDeclaration ctx)
        {
            CodeTypeMember lastMethod = Define.PartialMethod(MemberAttributes.Private, () => "OnCreated");
            lastMethod.StartDirective("Extensibility Method Definitions");
            ctx.AddMember(lastMethod);
            
            var n = new WXMLCodeDomGeneratorNameHelper(Settings);

            foreach (EntityDefinition e in Model.GetActiveEntities())
            {
                CodeTypeReference et = new CodeTypeReference(n.GetEntityClassName(e, true));
                
                ctx.AddMember(Define.PartialMethod(MemberAttributes.Private, (DynType instance) => "Insert" + e.Name + instance.SetType(et)));
                ctx.AddMember(Define.PartialMethod(MemberAttributes.Private, (DynType instance) => "Update" + e.Name + instance.SetType(et)));

                lastMethod = Define.PartialMethod(MemberAttributes.Private, (DynType instance) => "Delete" + e.Name + instance.SetType(et));
                ctx.AddMember(lastMethod);
            }

            foreach (RelationDefinitionBase rel in Model.ActiveRelations)
            {
                EntityDefinition e = new EntityDefinition("relationEntity",
                    GetName(rel.SourceFragment.Name), null/*Model.Namespace*/);

                CodeTypeReference et = new CodeTypeReference(n.GetEntityClassName(e, true));

                ctx.AddMember(Define.PartialMethod(MemberAttributes.Private, (DynType instance) => "Insert" + e.Name + instance.SetType(et)));
                ctx.AddMember(Define.PartialMethod(MemberAttributes.Private, (DynType instance) => "Update" + e.Name + instance.SetType(et)));

                lastMethod = Define.PartialMethod(MemberAttributes.Private, (DynType instance) => "Delete" + e.Name + instance.SetType(et));
                ctx.AddMember(lastMethod);
            }

            lastMethod.EndDirective();
        }

        private void AddCtors(CodeTypeDeclaration ctx)
        {
            ctx.AddCtor((string connection) => MemberAttributes.Public,
                Emit.stmt(()=>CodeDom.Call(null, "OnCreated")))
                .Base(
                    CodeDom.GetExpression((string connection) => connection),
                    CodeDom.GetExpression(() => CodeDom.VarRef("mappingSource"))
                )
            ;

            ctx.AddCtor((IDbConnection connection) => MemberAttributes.Public,
                Emit.stmt(() => CodeDom.Call(null, "OnCreated")))
                .Base(
                    CodeDom.GetExpression((IDbConnection connection) => connection),
                    CodeDom.GetExpression(() => CodeDom.VarRef("mappingSource"))
                )
            ;

            ctx.AddCtor((string connection, System.Data.Linq.Mapping.MappingSource mappingSource) => MemberAttributes.Public,
                Emit.stmt(() => CodeDom.Call(null, "OnCreated")))
                .Base(
                    CodeDom.GetExpression((string connection) => connection),
                    CodeDom.GetExpression(() => CodeDom.VarRef("mappingSource"))
                )
            ;

            ctx.AddCtor((IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) => MemberAttributes.Public,
                Emit.stmt(() => CodeDom.Call(null, "OnCreated")))
                .Base(
                    CodeDom.GetExpression((IDbConnection connection) => connection),
                    CodeDom.GetExpression(() => CodeDom.VarRef("mappingSource"))
                )
            ;
        }

        #endregion

        #region Public routines

        public CodeCompileFileUnit GetCompileUnit(LinqToCodedom.CodeDomGenerator.Language language)
        {
            CodeDomGenerator c = _GenerateCode(language);

            var un = new CodeCompileFileUnit() { Filename = Model.LinqSettings.FileName + _settings.FileNameSuffix };

            CodeDomTreeProcessor.ProcessNS(un, language, c.Namespaces.ToArray());

            return un;
        }

        public string GenerateCode(LinqToCodedom.CodeDomGenerator.Language language)
        {
            CodeDomGenerator c = _GenerateCode(language);

            return c.GenerateCode(language);
        }

        public Assembly Compile(LinqToCodedom.CodeDomGenerator.Language language)
        {
            return Compile(null, language);
        }

        public Assembly Compile(string assemblyPath, LinqToCodedom.CodeDomGenerator.Language language)
        {
            CodeDomGenerator c = _GenerateCode(language);

            c.AddReference("System.Core.dll");
            c.AddReference("System.Data.dll");
            c.AddReference("System.Data.Linq.dll");

            return c.Compile(assemblyPath, language);
        }

        #endregion
    }
}
