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

        #region Generator

        private CodeDomGenerator _GenerateCode(LinqToCodedom.CodeDomGenerator.Language language)
        {
            var c = new CodeDomGenerator();

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
                              where !string.IsNullOrEmpty(e.Namespace)
                              select e.Namespace)
                              .Distinct()
                              .Select(n => new { name = n, ns = c.AddNamespace(n) })
                              .ToArray();

            foreach(EntityDefinition e in Model.GetActiveEntities())
            {
                CodeNamespace ens = ns;
                var item = namespaces.SingleOrDefault(s => s.name == e.Namespace);
                if (item != null)
                    ens = item.ns;

                FillEntity(ens, e, language);
            }
        }

        private void FillEntity(CodeNamespace ns, EntityDefinition e, LinqToCodedom.CodeDomGenerator.Language language)
        {
            CodeTypeDeclaration cls = ns.AddClass(/*new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(e, true)*/ e.Name)
                .Inherits(typeof(System.ComponentModel.INotifyPropertyChanging))
                .Inherits(typeof(System.ComponentModel.INotifyPropertyChanged));

            SourceFragmentDefinition tbl = e.GetSourceFragments().First();

            var c = Define.Attribute(typeof(System.Data.Linq.Mapping.TableAttribute));
            cls.AddAttribute(c);
            Define.InitAttributeArgs(() => new { Name = string.IsNullOrEmpty(tbl.Selector) ? tbl.Name : tbl.Selector + "." + tbl.Name }, c);

            cls.IsPartial = true;

            cls.AddField(typeof(System.ComponentModel.PropertyChangingEventArgs), MemberAttributes.Private | MemberAttributes.Static,
                "emptyChangingEventArgs", () => new System.ComponentModel.PropertyChangingEventArgs(string.Empty));

            foreach (PropertyDefinition p in e.GetActiveProperties())
            {
                cls.AddField(p.PropertyType.ToCodeType(_settings),
                    WXMLCodeDomGenerator.GetMemberAttribute(p.FieldAccessLevel), 
                    new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.Name));
            }

            //add relations

            AddEntityPartialMethods(cls, e);

            cls.AddCtor(Emit.stmt(() => CodeDom.Call(null, "OnCreated")))
                .Base();

            foreach (PropertyDefinition p in e.GetActiveProperties())
            {
                var fieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(p.Name);

                var prop = cls.AddProperty(p.PropertyType.ToCodeType(_settings),
                    WXMLCodeDomGenerator.GetMemberAttribute(p.PropertyAccessLevel), p.Name,
                    CodeDom.CombineStmts(
                        Emit.@return(()=>CodeDom.@this.Field(fieldName))
                    ),
                    //set
                        Emit.@if(() => CodeDom.Call<bool>(CodeDom.@this.Field(fieldName), "Equals")(CodeDom.VarRef("value")),
                            Emit.stmt(()=>CodeDom.@this.Call("On"+p.Name+"Changing")(CodeDom.VarRef("value"))),
                            Emit.stmt(()=>CodeDom.@this.Call("SendPropertyChanging")),
                            Emit.assignField(fieldName, () => CodeDom.VarRef("value")),
                            Emit.stmt(()=>CodeDom.@this.Call("SendPropertyChanged")(p.Name)),
                            Emit.stmt(()=>CodeDom.@this.Call("On"+p.Name+"Changed"))
                    )
                );

                var attr = AddPropertyAttribute(p, fieldName);

                prop.AddAttribute(attr);
            }

            //relations

            cls.AddEvent(typeof(System.ComponentModel.PropertyChangingEventHandler), MemberAttributes.Public, "PropertyChanging")
                .Implements(typeof(System.ComponentModel.INotifyPropertyChanging));

            cls.AddEvent(typeof(System.ComponentModel.PropertyChangedEventHandler), MemberAttributes.Public, "PropertyChanged")
                .Implements(typeof(System.ComponentModel.INotifyPropertyChanged));

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
        }

        private static CodeAttributeDeclaration AddPropertyAttribute(PropertyDefinition p, string fieldName)
        {
            var attr = Define.Attribute(typeof(System.Data.Linq.Mapping.ColumnAttribute));
            string nullable = " NULL";
            if (p.DbTypeNullable.HasValue && p.DbTypeNullable.Value)
                nullable = " NOT NULL";

            if (p.HasAttribute(Field2DbRelations.PrimaryKey))
                nullable += " IDENTITY";

            string size = string.Empty;
            if (p.DbTypeSize.HasValue)
                size = "(" + p.DbTypeSize.Value.ToString() + ")";

            bool insertDefault = false;
            if (p.HasAttribute(Field2DbRelations.InsertDefault))
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
                Define.InitAttributeArgs(() => new { Storage = fieldName, Name = p.FieldName, DbType = p.DbTypeName + size + nullable, IsPrimaryKey = true, AutoSync = async, IsDbGenerated = insertDefault}, attr);
            }
            else
            {
                Define.InitAttributeArgs(() => new { Storage = fieldName, Name = p.FieldName, DbType = p.DbTypeName + size + nullable, AutoSync = async, IsDbGenerated = insertDefault }, attr);
            }

            return attr;
        }

        private void AddEntityPartialMethods(CodeTypeDeclaration cls, EntityDefinition e)
        {
            cls.AddMethod(MemberAttributes.Private, () => "OnLoaded");
            cls.AddMethod(MemberAttributes.Private, (System.Data.Linq.ChangeAction action) => "OnValidate");
            cls.AddMethod(MemberAttributes.Private, () => "OnCreated");
            foreach (PropertyDefinition p in e.GetActiveProperties())
            {
                cls.AddMethod(MemberAttributes.Private, (DynType value) => "On" + p.Name + "Changing" + value.SetType(p.PropertyType.ToCodeType(Settings)));
                cls.AddMethod(MemberAttributes.Private, () => "On" + p.Name + "Changed");
            }
        }

        private void AddProps(CodeTypeDeclaration ctx)
        {
            var n = new WXMLCodeDomGeneratorNameHelper(Settings);
            
            foreach (EntityDefinition e in Model.GetActiveEntities())
            {
                CodeTypeReference t = new CodeTypeReference(typeof(System.Data.Linq.Table<>));
                CodeTypeReference et = new CodeTypeReference(n.GetEntityClassName(e, true));
                t.TypeArguments.Add(et);
                ctx.AddGetProperty(t, MemberAttributes.Public,
                    WXMLCodeDomGeneratorNameHelper.GetMultipleForm(e.Name),
                    Emit.@return(() => CodeDom.@this.Call("GetTable", et))
                );
            }
        }

        private void AddPartialMethods(CodeTypeDeclaration ctx)
        {
            ctx.AddMethod(MemberAttributes.Private, () => "OnCreated");
            var n = new WXMLCodeDomGeneratorNameHelper(Settings);

            foreach (EntityDefinition e in Model.GetActiveEntities())
            {
                CodeTypeReference et = new CodeTypeReference(n.GetEntityClassName(e, true));

                ctx.AddMethod(MemberAttributes.Private, (DynType instance) => "Insert" + e.Name + instance.SetType(et));
                ctx.AddMethod(MemberAttributes.Private, (DynType instance) => "Update" + e.Name + instance.SetType(et));
                ctx.AddMethod(MemberAttributes.Private, (DynType instance) => "Delete" + e.Name + instance.SetType(et));
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
