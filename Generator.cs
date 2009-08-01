using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace LinqCodeGenerator
{
    public class LinqCodeDomGenerator
    {
        private WXMLModel _ormObjectsDefinition;
        private WXMLCodeDomGeneratorSettings _settings;

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

        private CodeDomGenerator _GenerateCode(LinqToCodedom.CodeDomGenerator.Language language)
        {
            var c = new CodeDomGenerator();

            var ns = c.AddNamespace("")
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

            AddEntities(ctx);

            return c;
        }

        private void AddEntities(CodeTypeDeclaration ctx)
        {
            foreach(EntityDescription e in Model.ActiveEntities)
            {
                FillEntity(ctx, e);
            }
        }

        private void FillEntity(CodeTypeDeclaration ctx, EntityDescription e)
        {
            CodeTypeDeclaration cls = ctx.AddClass(/*new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(e, true)*/ e.Name)
                .Inherits(typeof(System.ComponentModel.INotifyPropertyChanging))
                .Inherits(typeof(System.ComponentModel.INotifyPropertyChanged));

            SourceFragmentDescription tbl = e.SourceFragments[0];

            var c = Define.Attribute(typeof(System.Data.Linq.Mapping.TableAttribute));
            cls.AddAttribute(c);
            Define.InitAttributeArgs(() => new { Name = string.IsNullOrEmpty(tbl.Selector) ? tbl.Name : tbl.Selector + "." + tbl.Name }, c);

            cls.IsPartial = true;

            cls.AddField(typeof(System.ComponentModel.PropertyChangingEventArgs), MemberAttributes.Private | MemberAttributes.Static,
                "emptyChangingEventArgs", () => new System.ComponentModel.PropertyChangingEventArgs(string.Empty));

            foreach (PropertyDescription p in e.ActiveProperties)
            {
                cls.AddField(p.PropertyType.ToCodeType(_settings),
                    WXMLCodeDomGenerator.GetMemberAttribute(p.FieldAccessLevel), 
                    new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.PropertyName));
            }

            //add relations

            AddEntityPartialMethods(cls, e);

            cls.AddCtor(Emit.stmt(() => CodeDom.Call(null, "OnCreated")))
                .Base();

            foreach (PropertyDescription p in e.ActiveProperties)
            {
                var prop = cls.AddProperty(p.PropertyType.ToCodeType(_settings),
                    WXMLCodeDomGenerator.GetMemberAttribute(p.PropertyAccessLevel), p.PropertyName,
                    CodeDom.CombineStmts(
                        Emit.@return(()=>CodeDom.@this.Field(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.PropertyName)))
                    ),
                    //set
                        Emit.@if(()=>CodeDom.Call<bool>(CodeDom.@this.Field(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.PropertyName)), "Equals")(CodeDom.VarRef("value")),
                            Emit.stmt(()=>CodeDom.@this.Call("On"+p.PropertyName+"Changing")(CodeDom.VarRef("value"))),
                            Emit.stmt(()=>CodeDom.@this.Call("SendPropertyChanging")),
                            Emit.assignField(new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.PropertyName), ()=>CodeDom.VarRef("value")),
                            Emit.stmt(()=>CodeDom.@this.Call("SendPropertyChanged")(p.PropertyName)),
                            Emit.stmt(()=>CodeDom.@this.Call("On"+p.PropertyName+"Changed"))
                    )
                );

                var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.ColumnAttribute));
                string nullable = " NULL";
                if (p.DbTypeNullable.HasValue && p.DbTypeNullable.Value)
                    nullable = " NOT NULL";
                
                if (p.HasAttribute(Field2DbRelations.PK))
                    Define.InitAttributeArgs(() => new { Storage = p.FieldName, DbType = p.DbTypeName + nullable, IsPrimaryKey = true }, attr);
                else
                    Define.InitAttributeArgs(() => new { Storage = p.FieldName, DbType = p.DbTypeName + nullable }, attr);

                prop.AddAttribute(attr);
            }

            //relations

            cls.AddEvent(typeof(System.ComponentModel.PropertyChangingEventHandler), MemberAttributes.Public, "PropertyChanging")
                .Implements(typeof(System.ComponentModel.INotifyPropertyChanging));

            cls.AddEvent(typeof(System.ComponentModel.PropertyChangedEventHandler), MemberAttributes.Public, "PropertyChanged")
                .Implements(typeof(System.ComponentModel.INotifyPropertyChanged));

            cls.AddMethod(MemberAttributes.Family, () => "SendPropertyChanging",
                Emit.@if(() => !CodeDom.Call<bool>("ReferenceEquals")(CodeDom.@this.Property("PropertyChangingEvent"), null),
                    Emit.stmt(() => CodeDom.@this.Raise("PropertyChanging")(CodeDom.@this, CodeDom.Field(CodeDom.@this, "emptyChangingEventArgs")))
                )
            );

            cls.AddMethod(MemberAttributes.Family, (string propertyName) => "SendPropertyChanged",
                Emit.@if(() => !CodeDom.Call<bool>("ReferenceEquals")(CodeDom.@this.Property("PropertyChangedEvent"), null),
                    Emit.stmt((string propertyName) => CodeDom.@this.Raise("PropertyChanged")(CodeDom.@this, new System.ComponentModel.PropertyChangedEventArgs(propertyName)))
                )
            );
        }

        private void AddEntityPartialMethods(CodeTypeDeclaration cls, EntityDescription e)
        {
            cls.AddMethod(MemberAttributes.Private, () => "OnLoaded");
            cls.AddMethod(MemberAttributes.Private, (System.Data.Linq.ChangeAction action) => "OnValidate");
            cls.AddMethod(MemberAttributes.Private, () => "OnCreated");
            foreach (PropertyDescription p in e.ActiveProperties)
            {
                cls.AddMethod(MemberAttributes.Private, (DynType value) => "On" + p.PropertyName + "Changing" + value.SetType(p.PropertyType.ToCodeType(Settings)));
                cls.AddMethod(MemberAttributes.Private, () => "On" + p.PropertyName + "Changed");
            }
        }

        private void AddProps(CodeTypeDeclaration ctx)
        {
            foreach (EntityDescription e in Model.ActiveEntities)
            {
                CodeTypeReference t = new CodeTypeReference(typeof(System.Data.Linq.Table<>));
                t.TypeArguments.Add(new CodeTypeReference(e.Name));
                ctx.AddGetProperty(t, MemberAttributes.Public,
                    WXMLCodeDomGeneratorNameHelper.GetMultipleForm(e.Name),
                    Emit.@return(() => CodeDom.@this.Call("GetTable", new CodeTypeReference(e.Name)))
                );
            }
        }

        private void AddPartialMethods(CodeTypeDeclaration ctx)
        {
            ctx.AddMethod(MemberAttributes.Private, () => "OnCreated");

            foreach (EntityDescription e in Model.ActiveEntities)
            {
                ctx.AddMethod(MemberAttributes.Private, (DynType instance) => "Insert" + e.Name + instance.SetType(e.Name));
                ctx.AddMethod(MemberAttributes.Private, (DynType instance) => "Update" + e.Name + instance.SetType(e.Name));
                ctx.AddMethod(MemberAttributes.Private, (DynType instance) => "Delete" + e.Name + instance.SetType(e.Name));
            }
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

        #region Public routines

        public CodeCompileFileUnit GetFullSingleUnit(LinqToCodedom.CodeDomGenerator.Language language)
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
            CodeDomGenerator c = _GenerateCode(language);

            c.AddReference("System.Core.dll");
            c.AddReference("System.Data.dll");
            c.AddReference("System.Data.Linq.dll");

            return c.Compile(null, language);
        }

        #endregion
    }
}
