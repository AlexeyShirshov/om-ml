using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom;
using LinqToCodedom;
using WXML.Repository;
using WXML.Model;
using WXML.CodeDom;
using LinqToCodedom.Generator;
using LinqToCodedom.Extensions;
using System.Reflection;

namespace WXML2Linq
{
    public class LinqRepositoryGenerator
    {
        private string _conn;
        private string _modificationTrackerClassName;

        public LinqRepositoryGenerator(string connectionString, string modificationTrackerClassName)
        {
            _conn = connectionString;
            _modificationTrackerClassName = modificationTrackerClassName;
        }

        public CodeCompileUnit GenerateModificationTracker(WXMLModel model, 
            WXMLCodeDomGeneratorSettings setting, CodeDomGenerator.Language language)
        {
            var c = new CodeDomGenerator();

            var cls = c.AddNamespace(model.Namespace).AddClass(_modificationTrackerClassName);

            string _ctxName = model.Namespace + "." + model.LinqSettings.ContextName;
            string _mtName = model.Namespace + "." + _modificationTrackerClassName;

            cls.Implements(typeof(IModificationTracker))
                .AddEnum("ActionEnum")
                    .AddFields(
                        Define.StructField("Update"),
                        Define.StructField("Insert"),
                        Define.StructField("Delete")
            );

            cls.AddField("_changed", () => new List<object>());
            cls.AddField("_deleted", () => new List<object>());

            var tableType = new CodeTypeReference(typeof(System.Data.Linq.Table<>));
            tableType.TypeArguments.Add(new CodeTypeReference("T"));

            cls
                .AddMethod(MemberAttributes.Public | MemberAttributes.Final, (ParamArray<object> entities) => "Add",
                    Emit.stmt((List<object> _changed, object[] entities) => _changed.AddRange(entities)))
                .Implements(typeof(IModificationTracker))
                .AddMethod(MemberAttributes.Public | MemberAttributes.Final, (ParamArray<object> entities) => "Delete",
                    Emit.stmt((List<object> _deleted, object[] entities) => _deleted.AddRange(entities)))
                .Implements(typeof(IModificationTracker))
                .AddMethod(MemberAttributes.Public | MemberAttributes.Final, () => "AcceptModifications",
                    Emit.@using(new CodeTypeReference(_ctxName), "ctx", () => CodeDom.@new(_ctxName, _conn),
                        Emit.@foreach("entity", () => CodeDom.@this.Field<IEnumerable<object>>("_changed"),
                            Emit.stmt((object entity) => CodeDom.@this.Call("SyncEntity")(CodeDom.VarRef("ctx"), entity, false))
                        ),
                        Emit.@foreach("entity", () => CodeDom.@this.Field<IEnumerable<object>>("_deleted"),
                            Emit.stmt((object entity) => CodeDom.@this.Call("SyncEntity")(CodeDom.VarRef("ctx"), entity, true))
                        ),
                        Emit.stmt(() => CodeDom.VarRef("ctx").Call("SubmitChanges")),
                        Emit.@foreach("entity", () => CodeDom.@this.Field<IEnumerable<object>>("_changed"),
                            Emit.stmt((object entity) => CodeDom.@this.Call("AcceptChanges")(entity))
                        ),
                        Emit.stmt(() => CodeDom.@this.Field<List<object>>("_changed").Clear()),
                        Emit.@foreach("entity", () => CodeDom.@this.Field<IEnumerable<object>>("_deleted"),
                            Emit.stmt((object entity) => CodeDom.@this.Call("AcceptChanges")(entity))
                        ),
                        Emit.stmt(() => CodeDom.@this.Field<List<object>>("_deleted").Clear())
                    )
                ).Implements(typeof(IModificationTracker))
                .AddMethod(MemberAttributes.Private, () => "Dispose").Implements(typeof(IDisposable))
                .AddMethod(MemberAttributes.Private, (object entity) => "AcceptChanges",
                    Emit.declare("mi", (object entity) => entity.GetType().GetMethod("AcceptChanges")),
                    Emit.@if(() => CodeDom.VarRef("mi") != null,
                        Emit.stmt((MethodInfo mi, object entity) => mi.Invoke(entity, null))
                    )
                )
                .AddMethod(MemberAttributes.Private, (DynType ctx, object entity, bool delete) => "SyncEntity" + ctx.SetType(_ctxName),
                    Emit.@foreach("mi", () => CodeDom.@this.Call<Type>("GetType")().GetMethods(BindingFlags.NonPublic | BindingFlags.Static),
                        Emit.@if((bool delete, MethodInfo mi, object entity) =>
                            ((delete && mi.Name == "_DelEntity") || (!delete && mi.Name == "_SyncEntity")) && mi.GetParameters().Count() == 2 && mi.GetParameters().Last().ParameterType == entity.GetType(),
                            Emit.stmt((MethodInfo mi, object entity) => mi.Invoke(null, BindingFlags.Static, null, new object[] { CodeDom.VarRef("ctx"), entity }, null)),
                            Emit.exitFor()
                        )
                    )
                )
                .AddMethod(MemberAttributes.Private | MemberAttributes.Static, (DynType p, DynType action, DynType table) => "SyncEntity" + p.SetType("T") + action.SetType("ActionEnum") + table.SetType(tableType),
                    Emit.ifelse(() => CodeDom.VarRef("action") == CodeDom.Field(new CodeTypeReference("ActionEnum"), "Insert"),
                        CodeDom.CombineStmts(Emit.stmt(() => CodeDom.VarRef("table").Call("InsertOnSubmit")(CodeDom.VarRef("p")))),
                        Emit.ifelse(() => CodeDom.VarRef("action") == CodeDom.Field(new CodeTypeReference("ActionEnum"), "Delete"),
                            CodeDom.CombineStmts(
                                Emit.stmt(() => CodeDom.VarRef("table").Call("Attach")(CodeDom.VarRef("p"))),
                                Emit.stmt(() => CodeDom.VarRef("table").Call("DeleteOnSubmit")(CodeDom.VarRef("p")))
                            ),
                            Emit.stmt(() => CodeDom.VarRef("table").Call("Attach")(CodeDom.VarRef("p"), true))
                        )
                    )
                ).Generic("T", typeof(object))
            ;

            WXMLCodeDomGeneratorNameHelper n = new WXMLCodeDomGeneratorNameHelper(setting);

            foreach (var entity in model.GetActiveEntities())
            {
                if (entity.GetPkProperties().Count() == 1)
                {
                    //string entityName = entity.Name;
                    string entityProp = WXMLCodeDomGeneratorNameHelper.GetMultipleForm(entity.Name);
                    string entityType = n.GetEntityClassName(entity, true);
                    string pkName = entity.GetPkProperties().Single().Name;

                    cls.AddMethod(MemberAttributes.Static | MemberAttributes.Private,
                        (DynType ctx, DynType p) => "_DelEntity" + ctx.SetType(_ctxName) + p.SetType(entityType),
                        Emit.stmt(() => CodeDom.Call(null, "SyncEntity", new CodeTypeReference(entityType))(
                            CodeDom.VarRef("p"),
                            CodeDom.Field(new CodeTypeReference("ActionEnum"), "Delete"),
                            CodeDom.VarRef("ctx").Property(entityProp))
                        )
                    )
                    .AddMethod(MemberAttributes.Static | MemberAttributes.Private,
                        (DynType ctx, DynType p) => "_SynEntity" + ctx.SetType(_ctxName) + p.SetType(entityType),
                        Emit.stmt(() => CodeDom.Call(null, "SyncEntity", new CodeTypeReference(entityType))(
                            CodeDom.VarRef("p"),
                            CodeDom.VarRef("p").Field<int>(pkName) == 0 ? CodeDom.Field(new CodeTypeReference("ActionEnum"), "Insert") : CodeDom.Field(new CodeTypeReference("ActionEnum"), "Update"),
                            CodeDom.VarRef("ctx").Property(entityProp))
                        )
                    )
                    ;
                }
            }

            //string debug = c.GenerateCode(CodeDomGenerator.Language.CSharp);

            return c.GetCompileUnit(language);
        }


    }
}
