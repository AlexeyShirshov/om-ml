using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;
using WXML.Model.Descriptors;
using LinqToCodedom.Generator;
using System.Linq;
using LinqToCodedom.Extensions;
using Worm.Entities.Meta;
using Worm.Query;
using WXML.Model;
using Worm.Collections;
using WXML.CodeDom;

namespace WXMLToWorm.CodeDomExtensions
{
    public class CodeSchemaDefTypeDeclaration : CodeTypeDeclaration
    {
        private CodeEntityTypeDeclaration m_entityClass;
        private readonly CodeTypeReference m_typeReference;
        private WXMLCodeDomGeneratorSettings _settings;

        public CodeSchemaDefTypeDeclaration(WXMLCodeDomGeneratorSettings settings)
        {
            m_typeReference = new CodeTypeReference();
            IsClass = true;
            TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;
            PopulateBaseTypes += OnPopulateBaseTypes;
            PopulateMembers += OnPopulateMembers;
            _settings = settings;
        }

        protected void OnPopulateMembers(object sender, EventArgs e)
        {
            OnPopulateIDefferedLoadingInterfaceMemebers();
            OnPopulateM2mMembers();
            OnPopulateTableMember();
            OnPopulateMultitableMembers();

            CreateGetFieldColumnMap();
        }


        protected void OnPopulateBaseTypes(object sender, EventArgs e)
        {
            OnPupulateSchemaInterfaces();
            OnPopulateIDefferedLoadingInterface();
            OnPopulateBaseClass();
            OnPopulateM2mRealationsInterface();
            OnPopulateMultitableInterface();
        }

        private void OnPopulateMultitableInterface()
        {
            if (m_entityClass.Entity.CompleteEntity.GetSourceFragments().Count() < 2)
                return;

            if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.InheritsBaseTables && m_entityClass.Entity.GetSourceFragments().Count() == 0)
                return;

            BaseTypes.Add(new CodeTypeReference(typeof(IMultiTableObjectSchema)));
        }

        private void OnPopulateMultitableMembers()
        {
            //if (m_entityClass.Entity.CompleteEntity.SourceFragments.Count < 2)
            //    return;

            //if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.InheritsBaseTables && m_entityClass.Entity.SourceFragments.Count == 0)
            //    return;

            if (!m_entityClass.Entity.IsMultitable)
                return;

            //if(m_entityClass.Entity.BaseEntity == null || (m_entityClass.Entity.BaseEntity != null && !m_entityClass.Entity.BaseEntity.IsMultitable))
            CreateGetTableMethod();

            var field = new CodeMemberField(new CodeTypeReference(typeof(SourceFragment[])), "_tables");
            field.Attributes = MemberAttributes.Private;
            Members.Add(field);

            CodeMemberMethod method = new CodeMemberMethod();
            Members.Add(method);
            method.Name = "GetTables";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(SourceFragment[]));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.IsMultitable)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            else
                // реализует метод интерфейса
                method.ImplementationTypes.Add(typeof(IMultiTableObjectSchema));
            // параметры
            //...
            // для лока
            CodeMemberField forTablesLockField = new CodeMemberField(
                new CodeTypeReference(typeof(object)),
                "_forTablesLock"
                );
            forTablesLockField.InitExpression = new CodeObjectCreateExpression(forTablesLockField.Type);
            Members.Add(forTablesLockField);
            // тело
            method.Statements.Add(
                WormCodeDomGenerator.CodePatternDoubleCheckLock(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_forTablesLock"
                        ),
                    new CodeBinaryOperatorExpression(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_tables"
                            ),
                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(null)
                        ),
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_tables"
                            ),
                        new CodeArrayCreateExpression(
                            new CodeTypeReference(typeof(SourceFragment[])),
                            m_entityClass.Entity.CompleteEntity.GetSourceFragments().Select(
                                action =>
                                {
                                    var result = new CodeObjectCreateExpression(
                                        new CodeTypeReference(typeof(SourceFragment))
                                        );
                                    if (!string.IsNullOrEmpty(action.Selector))
                                        result.Parameters.Add(new CodePrimitiveExpression(action.Selector));
                                    result.Parameters.Add(new CodePrimitiveExpression(action.Name));
                                    return result;
                                }
                                ).ToArray()
                            )
                        )
                    )
                );
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_tables"
                        )
                    )
                );

            if (!IsPartial || m_entityClass.Entity.GetSourceFragments().Any(sf => sf.AnchorTable != null))
            {
                CodeMemberMethod jmethod = Define.Method(MemberAttributes.Public, typeof(Worm.Criteria.Joins.QueryJoin),
                    (SourceFragment left, SourceFragment right) => "GetJoins");

                CodeConditionStatement cond = null;

                foreach (SourceFragmentRefDefinition tbl in
                    m_entityClass.Entity.GetSourceFragments().Where(sf => sf.AnchorTable != null))
                {
                    int tblIdx = m_entityClass.Entity.GetSourceFragments().IndexOf(tbl);
                    int sfrIdx = m_entityClass.Entity.GetSourceFragments().IndexOf((sfr)=>sfr.Identifier == tbl.AnchorTable.Identifier);
                    if (cond == null)
                    {
                        CodeConditionStatement cond2 = Emit.@if((SourceFragment left, SourceFragment right) =>
                            (left.Equals(CodeDom.@this.Call<SourceFragment[]>("GetTables")()[tblIdx]) && right.Equals(CodeDom.@this.Call<SourceFragment[]>("GetTables")()[sfrIdx])),
                                Emit.@return((SourceFragment left, SourceFragment right) =>
                                    JCtor.join(right).on(left, tbl.Conditions[0].LeftColumn).eq(right, tbl.Conditions[0].RightColumn))
                            );
                        jmethod.Statements.Add(cond2);

                        cond = Emit.@if((SourceFragment left, SourceFragment right) =>
                            (right.Equals(CodeDom.@this.Call<SourceFragment[]>("GetTables")()[tblIdx]) && left.Equals(CodeDom.@this.Call<SourceFragment[]>("GetTables")()[sfrIdx])),
                                Emit.@return((SourceFragment left, SourceFragment right) =>
                                    JCtor.join(right).on(left, tbl.Conditions[0].RightColumn).eq(right, tbl.Conditions[0].LeftColumn))
                            );

                        cond2.FalseStatements.Add(cond);
                    }
                    else
                    {
                        CodeConditionStatement cond2 = Emit.@if((SourceFragment left, SourceFragment right) =>
                            left.Equals(CodeDom.@this.Call<SourceFragment[]>("GetTables")()[tblIdx]) && right.Equals(CodeDom.@this.Call<SourceFragment[]>("GetTables")()[sfrIdx]),
                                Emit.@return((SourceFragment left, SourceFragment right) =>
                                    JCtor.join(right).on(left, tbl.Conditions[0].LeftColumn).eq(right, tbl.Conditions[0].RightColumn))
                            );
                        
                        cond.FalseStatements.Add(cond2);

                        cond = cond2;
                    }
                }

                if (cond != null)
                    cond.FalseStatements.Add(Emit.@throw(() => new NotImplementedException("Entity has more then one table: this method must be implemented.")));
                else
                    jmethod.Statements.Add(Emit.@throw(() => new NotImplementedException("Entity has more then one table: this method must be implemented.")));
                
                jmethod.Implements(typeof(IMultiTableObjectSchema));
                Members.Add(jmethod);
            }

            if (m_entityClass.Entity.BaseEntity == null || !m_entityClass.Entity.BaseEntity.IsMultitable)
            {
                CodeMemberProperty prop = new CodeMemberProperty();
                Members.Add(prop);
                prop.Name = "Table";
                prop.Type = new CodeTypeReference(typeof(SourceFragment));
                prop.Attributes = MemberAttributes.Public;
                prop.HasSet = false;
                prop.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeArrayIndexerExpression(
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "GetTables")),
                            new CodePrimitiveExpression(0)
                        )
                    )
                );
                if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.CompleteEntity.GetSourceFragments().Count() == 1)
                    prop.Attributes |= MemberAttributes.Override;
                else
                    prop.ImplementationTypes.Add(typeof(IEntitySchema));
            }
        }

        private void CreateGetTableMethod()
        {
            CodeMemberMethod method;
            method = new CodeMemberMethod();
            Members.Add(method);
            method.Name = "GetTable";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(SourceFragment));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Family | MemberAttributes.Final;
            //if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.IsMultitable)
            //    method.Attributes |= MemberAttributes.New;

            // параметры
            method.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(
                        new WXMLCodeDomGeneratorNameHelper(_settings).GetEntitySchemaDefClassQualifiedName(m_entityClass.Entity) + ".TablesLink"), "tbl"
                    )
                );
            //	return (SourceFragment)this.GetTables().GetValue((int)tbl)

            //	SourceFragment[] tables = this.GetTables();
            //	SourceFragment table = null;
            //	int tblIndex = (int)tbl;
            //	if(tables.Length > tblIndex)
            //		table = tables[tblIndex];
            //	return table;
            //string[] strs;
            method.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(SourceFragment[])),
                    "tables",
                    new CodeMethodInvokeExpression(
                        new CodeThisReferenceExpression(),
                        "GetTables"
                        )));
            method.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(SourceFragment)), "table", new CodePrimitiveExpression(null)));
            method.Statements.Add(new CodeVariableDeclarationStatement(
                                    new CodeTypeReference(typeof(int)),
                                    "tblIndex",
                                    new CodeCastExpression(
                                        new CodeTypeReference(typeof(int)),
                                        new CodeArgumentReferenceExpression("tbl")
                                        )
                                    ));
            method.Statements.Add(new CodeConditionStatement(
                                    new CodeBinaryOperatorExpression(
                                        new CodePropertyReferenceExpression(
                                            new CodeVariableReferenceExpression("tables"),
                                            "Length"
                                            ),
                                        CodeBinaryOperatorType.GreaterThan,
                                        new CodeVariableReferenceExpression("tblIndex")
                                        ),
                                    new CodeAssignStatement(
                                        new CodeVariableReferenceExpression("table"),
                                        new CodeIndexerExpression(
                                            new CodeVariableReferenceExpression("tables"),
                                            new CodeVariableReferenceExpression("tblIndex")
                                            ))));
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeVariableReferenceExpression("table")
                    ));
        }

        private void CreateGetFieldColumnMap()
        {
            var field =
                        new CodeMemberField(
                            new CodeTypeReference(typeof(IndexedCollection<string, MapField2Column>)),
                            "_idx");
            Members.Add(field);

            var method = new CodeMemberMethod();
            Members.Add(method);
            method.Name = "GetFieldColumnMap";
            // тип возвращаемого значения
            method.ReturnType =
                new CodeTypeReference(typeof(IndexedCollection<string, MapField2Column>));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            if (m_entityClass.Entity.BaseEntity != null)
            {
                method.Attributes |= MemberAttributes.Override;
            }
            else
                // реализует метод базового класса
                method.ImplementationTypes.Add(new CodeTypeReference(typeof(IPropertyMap)));
            // параметры
            //...
            // для лока
            CodeMemberField forIdxLockField = new CodeMemberField(
                new CodeTypeReference(typeof(object)),
                "_forIdxLock"
                );
            forIdxLockField.InitExpression = new CodeObjectCreateExpression(forIdxLockField.Type);
            Members.Add(forIdxLockField);
            List<CodeStatement> condTrueStatements = new List<CodeStatement>
			                                         	{
			                                         		new CodeVariableDeclarationStatement(
			                                         			new CodeTypeReference(typeof (IndexedCollection<string, MapField2Column>)),
			                                         			"idx",
			                                         			(m_entityClass.Entity.BaseEntity == null)
			                                         				?
			                                         					(CodeExpression) new CodeObjectCreateExpression(
			                                         					                 	new CodeTypeReference(typeof (OrmObjectIndex))
			                                         					                 	)
			                                         				:
			                                         					new CodeMethodInvokeExpression(
			                                         						new CodeBaseReferenceExpression(),
			                                         						"GetFieldColumnMap"
			                                         						)
			                                         			)
			                                         	};
            condTrueStatements.AddRange(m_entityClass.Entity.SelfProperties
                .Where(action => !action.Disabled)
                .SelectMany<PropertyDefinition, CodeStatement>(action =>
                    {
                        List<CodeStatement> coll = new List<CodeStatement>();
                        coll.Add(new CodeAssignStatement(
                            new CodeIndexerExpression(
                                new CodeVariableReferenceExpression("idx"),
                                new CodePrimitiveExpression(action.PropertyAlias)
                            ),
                            GetMapField2ColumObjectCreationExpression(action)
                        ));
                        if (!string.IsNullOrEmpty(action.FieldAlias))
                            coll.Add(
                                Emit.assignProperty(new CodeIndexerExpression(
                                        new CodeVariableReferenceExpression("idx"),
                                        new CodePrimitiveExpression(action.PropertyAlias)
                                    ), "ColumnName", ()=>action.FieldAlias)
                                );
                        return coll;
                    }
                )
            );

            condTrueStatements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_idx"
                        ),
                    new CodeVariableReferenceExpression("idx")
                    )
                );
            method.Statements.Add(
                WormCodeDomGenerator.CodePatternDoubleCheckLock(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_forIdxLock"
                        ),
                    new CodeBinaryOperatorExpression(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_idx"
                            ),
                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(null)
                        ),
                    condTrueStatements.ToArray()
                    )
                );
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_idx"
                        )
                    )
                );
        }

        private CodeObjectCreateExpression GetMapField2ColumObjectCreationExpression(PropertyDefinition action)
        {
            CodeObjectCreateExpression expression = new CodeObjectCreateExpression(
                new CodeTypeReference(
                    typeof(MapField2Column)));
            expression.Parameters.Add(new CodePrimitiveExpression(action.PropertyAlias));
            expression.Parameters.Add(new CodePrimitiveExpression(action.FieldName));
            //(SourceFragment)this.GetTables().GetValue((int)(XMedia.Framework.Media.Objects.ArtistBase.ArtistBaseSchemaDef.TablesLink.tblArtists)))

            if (m_entityClass.Entity.IsMultitable)
            {
                expression.Parameters.Add(new CodeMethodInvokeExpression(
                    new CodeThisReferenceExpression(), "GetTable",
                    new CodeFieldReferenceExpression(
                        new CodeTypeReferenceExpression(new WXMLCodeDomGeneratorNameHelper(_settings).
                            GetEntitySchemaDefClassQualifiedName(m_entityClass.Entity) + ".TablesLink"),
                        WXMLCodeDomGeneratorNameHelper.GetSafeName(action.SourceFragment.Identifier)
                        )
                    )
                );
            }
            else
            {
                expression.Parameters.Add(new CodePropertyReferenceExpression(
                                            new CodeThisReferenceExpression(),
                                            "Table"));
            }

            //if (action.PropertyAlias == "ID")
            //if (action.Attributes != null && action.Attributes.Length > 0)
            //    expression.Parameters.Add(GetPropAttributesEnumValues(action.Attributes));
            //else
            //    expression.Parameters.Add(
            //        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(Worm.Entities.Meta.Field2DbRelations)),
            //                                         Worm.Entities.Meta.Field2DbRelations.None.ToString()));
            expression.Parameters.Add(GetPropAttributesEnumValues(action.Attributes));

            if (!string.IsNullOrEmpty(action.DbTypeName))
            {
                expression.Parameters.Add(new CodePrimitiveExpression(action.DbTypeName));
                if (action.DbTypeSize.HasValue)
                    expression.Parameters.Add(new CodePrimitiveExpression(action.DbTypeSize.Value));
                if (action.DbTypeNullable.HasValue)
                    expression.Parameters.Add(new CodePrimitiveExpression(action.DbTypeNullable.Value));
            }
            return expression;
        }

        //private static CodeExpression GetPropAttributesEnumValues(IEnumerable<string> attrs)
        //{
        //    var attrsList = new List<string>(attrs);
        //    CodeExpression first = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(Worm.Entities.Meta.Field2DbRelations)), attrsList.Count == 0 ? Worm.Entities.Meta.Field2DbRelations.None.ToString() : attrsList[0]);
        //    if (attrsList.Count > 1)
        //        return new CodeBinaryOperatorExpression(first, CodeBinaryOperatorType.BitwiseOr, GetPropAttributesEnumValues(attrsList.GetRange(1, attrsList.Count - 1).ToArray()));
        //    return first;
        //}

        private static CodeExpression GetPropAttributesEnumValues(WXML.Model.Field2DbRelations attrs)
        {
            return CodeDom.GetExpression(()=>CodeDom.Field(new CodeTypeReference(typeof(Worm.Entities.Meta.Field2DbRelations)), attrs.ToString()));
        }

        private void OnPopulateTableMember()
        {
            if (m_entityClass.Entity.GetSourceFragments().Count() == 1 && m_entityClass.Entity.BaseEntity == null)
            {

                // private SourceFragment m_table;
                // private object m_tableLock = new object();
                // public virtual SourceFragment Table {
                //		get {
                //			if(m_table == null) {
                //				lock(m_tableLoack) {
                //					if(m_table == null) {
                //						m_table = new SourceFragment("..", "...");
                //					}
                //				}
                //			}
                //		}
                //	}

                CodeMemberField field = new CodeMemberField(new CodeTypeReference(typeof(SourceFragment)),
                                                            new WXMLCodeDomGeneratorNameHelper(_settings).GetPrivateMemberName("table"));
                Members.Add(field);

                CodeMemberField lockField = new CodeMemberField(new CodeTypeReference(typeof(object)),
                                                            new WXMLCodeDomGeneratorNameHelper(_settings).GetPrivateMemberName("tableLock"));
                Members.Add(lockField);

                lockField.InitExpression = new CodeObjectCreateExpression(lockField.Type);


                var table = m_entityClass.Entity.GetSourceFragments().First();

                CodeMemberProperty prop = new CodeMemberProperty();
                Members.Add(prop);
                prop.Name = "Table";
                prop.Type = new CodeTypeReference(typeof(SourceFragment));
                prop.Attributes = MemberAttributes.Public;
                prop.HasSet = false;
                prop.GetStatements.Add(
                    WormCodeDomGenerator.CodePatternDoubleCheckLock(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), lockField.Name),
                        new CodeBinaryOperatorExpression(
                            new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                field.Name),
                                CodeBinaryOperatorType.IdentityEquality,
                                new CodePrimitiveExpression(null)),
                        new CodeAssignStatement(
                            new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                field.Name),
                                new CodeObjectCreateExpression(field.Type, new CodePrimitiveExpression(table.Selector), new CodePrimitiveExpression(table.Name))
                                ))
                    );
                prop.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), field.Name)));

                prop.ImplementationTypes.Add(typeof(IEntitySchema));

            }
        }

        private void OnPopulateM2mMembers()
        {
            if (m_entityClass.Entity.GetAllRelations(false).Count == 0)
                return;

            CodeMemberMethod method;
            // список релейшенов относящихся к данной сущности
            List<RelationDefinition> usedM2MRelation = m_entityClass.Entity.GetRelations(false);

            List<SelfRelationDescription> usedM2MSelfRelation;
            usedM2MSelfRelation = m_entityClass.Entity.GetSelfRelations(false);

            if (m_entityClass.Entity.BaseEntity == null || usedM2MSelfRelation.Count > 0 || usedM2MRelation.Count > 0)
            {
                #region поле _m2mRelations

                CodeMemberField field = new CodeMemberField(new CodeTypeReference(typeof(M2MRelationDesc[])), "_m2mRelations");
                Members.Add(field);

                #endregion поле _m2mRelations

                #region метод M2MRelationDesc[] GetM2MRelations()

                method = new CodeMemberMethod();
                Members.Add(method);
                method.Name = "GetM2MRelations";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(M2MRelationDesc[]));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                if (m_entityClass.Entity.BaseEntity != null)
                {
                    method.Attributes |= MemberAttributes.Override;
                }
                else
                    // реализует метод базового класса
                    method.ImplementationTypes.Add(typeof(ISchemaWithM2M));
                // параметры
                //...
                // для лока
                CodeMemberField forM2MRelationsLockField = new CodeMemberField(
                    new CodeTypeReference(typeof(object)),
                    "_forM2MRelationsLock"
                    );
                forM2MRelationsLockField.InitExpression =
                    new CodeObjectCreateExpression(forM2MRelationsLockField.Type);
                Members.Add(forM2MRelationsLockField);
                // тело
                CodeExpression condition = new CodeBinaryOperatorExpression(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_m2mRelations"
                        ),
                    CodeBinaryOperatorType.IdentityEquality,
                    new CodePrimitiveExpression(null)
                    );
                CodeStatementCollection inlockStatemets = new CodeStatementCollection();
                CodeArrayCreateExpression m2mArrayCreationExpression = new CodeArrayCreateExpression(
                    new CodeTypeReference(typeof(M2MRelationDesc[]))
                    );
                foreach (RelationDefinition relationDescription in usedM2MRelation)
                {
                    m2mArrayCreationExpression.Initializers.AddRange(
                        GetM2MRelationCreationExpressions(relationDescription, m_entityClass.Entity));
                }
                foreach (SelfRelationDescription selfRelationDescription in usedM2MSelfRelation)
                {
                    m2mArrayCreationExpression.Initializers.AddRange(
                        GetM2MRelationCreationExpressions(selfRelationDescription, m_entityClass.Entity));
                }
                inlockStatemets.Add(new CodeVariableDeclarationStatement(
                                        method.ReturnType,
                                        "m2mRelations",
                                        m2mArrayCreationExpression
                                        ));
                if (m_entityClass.Entity.BaseEntity != null)
                {
                    // M2MRelationDesc[] basem2mRelations = base.GetM2MRelations()
                    inlockStatemets.Add(
                        new CodeVariableDeclarationStatement(
                            new CodeTypeReference(typeof(M2MRelationDesc[])),
                            "basem2mRelations",
                            new CodeMethodInvokeExpression(
                                new CodeBaseReferenceExpression(),
                                "GetM2MRelations"
                                )
                            )
                        );
                    // Array.Resize<M2MRelationDesc>(ref m2mRelation, basem2mRelation.Length, m2mRelation.Length)
                    inlockStatemets.Add(
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Array))),
                                "Resize",
                                new CodeTypeReference(typeof(M2MRelationDesc))),
                            new CodeDirectionExpression(FieldDirection.Ref,
                                                        new CodeVariableReferenceExpression("m2mRelations")),
                            new CodeBinaryOperatorExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("basem2mRelations"),
                                    "Length"
                                    ),
                                CodeBinaryOperatorType.Add,
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("m2mRelations"),
                                    "Length"
                                    )
                                )
                            )
                        );
                    // Array.Copy(basem2mRelation, 0, m2mRelations, m2mRelations.Length - basem2mRelation.Length, basem2mRelation.Length)
                    inlockStatemets.Add(
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(typeof(Array)),
                            "Copy",
                            new CodeVariableReferenceExpression("basem2mRelations"),
                            new CodePrimitiveExpression(0),
                            new CodeVariableReferenceExpression("m2mRelations"),
                            new CodeBinaryOperatorExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("m2mRelations"), "Length"),
                                CodeBinaryOperatorType.Subtract,
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("basem2mRelations"), "Length")
                                ),
                            new CodePropertyReferenceExpression(
                                new CodeVariableReferenceExpression("basem2mRelations"), "Length")
                            )
                        );
                }
                inlockStatemets.Add(
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_m2mRelations"),
                        new CodeVariableReferenceExpression("m2mRelations")
                        )
                    );
                List<CodeStatement> statements = new List<CodeStatement>(inlockStatemets.Count);
                foreach (CodeStatement statemet in inlockStatemets)
                {
                    statements.Add(statemet);
                }
                method.Statements.Add(
                    WormCodeDomGenerator.CodePatternDoubleCheckLock(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_forM2MRelationsLock"
                            ),
                        condition,
                        statements.ToArray()
                        )
                    );
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_m2mRelations"
                            )
                        )
                    );

                #endregion метод string[] GetTables()
            }
        }


        private CodeExpression[] GetM2MRelationCreationExpressions(RelationDefinition relationDescription, EntityDefinition entity)
        {
            if (relationDescription.Left.Entity != relationDescription.Right.Entity)
            {
                EntityDefinition relatedEntity = entity == relationDescription.Left.Entity
                    ? relationDescription.Right.Entity : relationDescription.Left.Entity;
                string fieldName = entity == relationDescription.Left.Entity ? relationDescription.Right.FieldName : relationDescription.Left.FieldName;
                bool cascadeDelete = entity == relationDescription.Left.Entity ? relationDescription.Right.CascadeDelete : relationDescription.Left.CascadeDelete;

                return new CodeExpression[] { GetM2MRelationCreationExpression(relatedEntity, relationDescription.SourceFragment, relationDescription.UnderlyingEntity, fieldName, cascadeDelete, null, relationDescription.Constants) };
            }
            throw new ArgumentException("To realize m2m relation on self use SelfRelation instead.");
        }

        private CodeExpression[] GetM2MRelationCreationExpressions(SelfRelationDescription relationDescription, EntityDefinition entity)
        {

            return new CodeExpression[]
				{
					GetM2MRelationCreationExpression(entity, relationDescription.SourceFragment, relationDescription.UnderlyingEntity,
					                                 relationDescription.Direct.FieldName, relationDescription.Direct.CascadeDelete,
					                                 true, relationDescription.Constants),
					GetM2MRelationCreationExpression(entity, relationDescription.SourceFragment, relationDescription.UnderlyingEntity,
					                                 relationDescription.Reverse.FieldName, relationDescription.Reverse.CascadeDelete, false, relationDescription.Constants)
				};

        }

        private CodeExpression GetM2MRelationCreationExpression(EntityDefinition relatedEntity, SourceFragmentDefinition relationTable, EntityDefinition underlyingEntity, string fieldName, bool cascadeDelete, bool? direct, IList<RelationConstantDescriptor> relationConstants)
        {
            //if (underlyingEntity != null && direct.HasValue)
            //    throw new NotImplementedException("M2M relation on self cannot have underlying entity.");
            // new Worm.Orm.M2MRelation(this._schema.GetTypeByEntityName("Album"), this.GetTypeMainTable(this._schema.GetTypeByEntityName("Album2ArtistRelation")), "album_id", false, new System.Data.Common.DataTableMapping(), this._schema.GetTypeByEntityName("Album2ArtistRelation")),

            CodeExpression entityTypeExpression;
            CodeExpression tableExpression;
            CodeExpression fieldExpression;
            CodeExpression cascadeDeleteExpression;
            CodeExpression mappingExpression;

            //entityTypeExpression = new CodeMethodInvokeExpression(
            //    new CodeMethodReferenceExpression(
            //        new CodeFieldReferenceExpression(
            //            new CodeThisReferenceExpression(),
            //            "_schema"
            //            ),
            //        "GetTypeByEntityName"
            //        ),
            //    OrmCodeGenHelper.GetEntityNameReferenceExpression(relatedEntity)
            //        //new CodePrimitiveExpression(relatedEntity.Name)
            //    );
            entityTypeExpression = WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings,relatedEntity);

            if (underlyingEntity == null)
                tableExpression = new CodeMethodInvokeExpression(
                    //new CodeCastExpression(
                    //new CodeTypeReference(typeof(Worm.IDbSchema)),
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema")
                        ,
                    "GetSharedSourceFragment",
                    new CodePrimitiveExpression(relationTable.Selector),
                    new CodePrimitiveExpression(relationTable.Name)
                    );
            else
                tableExpression = new CodePropertyReferenceExpression(
                    new CodeMethodInvokeExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
                        "GetEntitySchema", WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings, underlyingEntity)),
                    "Table");
            //tableExpression = new CodeMethodInvokeExpression(
            //    new CodeThisReferenceExpression(),
            //    "GetTypeMainTable",
            //    new CodeMethodInvokeExpression(
            //        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
            //        "GetTypeByEntityName",
            //        OrmCodeGenHelper.GetEntityNameReferenceExpression(underlyingEntity)
            //        //new CodePrimitiveExpression(underlyingEntity.Name)
            //        )
            //    );

            fieldExpression = new CodePrimitiveExpression(fieldName);

            cascadeDeleteExpression = new CodePrimitiveExpression(cascadeDelete);

            mappingExpression = new CodeObjectCreateExpression(new CodeTypeReference(typeof(DataTableMapping)));

            CodeObjectCreateExpression result =
                new CodeObjectCreateExpression(
                    new CodeTypeReference(typeof(M2MRelationDesc)),
                //new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
                    entityTypeExpression,
                    tableExpression,
                    fieldExpression,
                    cascadeDeleteExpression,
                    mappingExpression);

            string f = relationTable.Identifier;// "DirKey";
            if (direct.HasValue && !direct.Value)
            {
                f = M2MRelationDesc.ReversePrefix + f;
            }
            //result.Parameters.Add(
            //        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(M2MRelationDesc)), f)
            //    );
            result.Parameters.Add(new CodePrimitiveExpression(f));

            if (underlyingEntity != null)
            {
                CodeExpression connectedTypeExpression = new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_schema"),
                        "GetTypeByEntityName"
                        ),
                    new CodePrimitiveExpression(underlyingEntity.Name)
                    );
                result.Parameters.Add(
                    connectedTypeExpression
                );
            }
            else
            {
                result.Parameters.Add(new CodePrimitiveExpression(null));
            }
            if (relationConstants != null && relationConstants.Count > 0)
            {
                RelationConstantDescriptor constant = relationConstants[0];
                //Ctor.column(_schema.Table, "name").eq("value");
                CodeExpression exp = new CodeMethodInvokeExpression(
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(typeof(Ctor)),
                        "column",
                        tableExpression,
                        new CodePrimitiveExpression(constant.Name)
                        ),
                    "eq",
                    new CodePrimitiveExpression(constant.Value));
                for (int i = 1; i < relationConstants.Count; i++)
                {
                    constant = relationConstants[i];
                    exp = new CodeMethodInvokeExpression(new CodeMethodInvokeExpression(exp, "column", tableExpression, new CodePrimitiveExpression(constant.Name)), "eq", new CodePrimitiveExpression(constant.Value));
                }
                result.Parameters.Add(exp);
            }
            return result;
        }

        protected void OnPopulateIDefferedLoadingInterfaceMemebers()
        {
            if (m_entityClass == null || m_entityClass.Entity == null || !m_entityClass.Entity.HasDefferedLoadableProperties)
                return;

            var method = new CodeMemberMethod
                            {
                                Name = "GetDefferedLoadPropertiesGroups",
                                Attributes = MemberAttributes.Public,
                                ReturnType = new CodeTypeReference(typeof(string[][]))
                            };

            // string[][] result;
            //method.Statements.Add(new CodeVariableDeclarationStatement(method.ReturnType, "result"));

            var defferedLoadPropertiesGrouped = m_entityClass.Entity.GetDefferedLoadProperties();

            var baseFieldName = method.Name;

            var fieldName = new WXMLCodeDomGeneratorNameHelper(_settings).GetPrivateMemberName(method.Name);
            var dicFieldName = new WXMLCodeDomGeneratorNameHelper(_settings).GetPrivateMemberName(baseFieldName + "Dic");
            var dicFieldTypeReference = new CodeTypeReference(typeof(Dictionary<string, List<string>>));

            if (m_entityClass.Entity.BaseEntity == null ||
                !m_entityClass.Entity.BaseEntity.HasDefferedLoadablePropertiesInHierarhy)
            {

                var dicField = new CodeMemberField(dicFieldTypeReference, dicFieldName)
                                {
                                    Attributes = MemberAttributes.Family,
                                    InitExpression = new CodeObjectCreateExpression(dicFieldTypeReference)
                                };
                Members.Add(dicField);
            }

            var field = new CodeMemberField(method.ReturnType, fieldName);
            Members.Add(field);

            var lockObjFieldName = new WXMLCodeDomGeneratorNameHelper(_settings).GetPrivateMemberName(baseFieldName + "Lock");

            var lockObj = new CodeMemberField(new CodeTypeReference(typeof(object)), lockObjFieldName);
            lockObj.InitExpression = new CodeObjectCreateExpression(lockObj.Type);
            Members.Add(lockObj);

            CodeExpression condition = new CodeBinaryOperatorExpression(
                new CodeFieldReferenceExpression(
                    new CodeThisReferenceExpression(),
                    field.Name
                    ),
                CodeBinaryOperatorType.IdentityEquality,
                new CodePrimitiveExpression(null));

            CodeStatementCollection inlockStatemets = new CodeStatementCollection();

            CodeVariableDeclarationStatement listVar =
                new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(List<string>)), "lst");
            inlockStatemets.Add(listVar);

            foreach (var propertyDescriptions in defferedLoadPropertiesGrouped)
            {
                inlockStatemets.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeMethodInvokeExpression(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    dicFieldName
                                    ),
                                "TryGetValue",
                                new CodePrimitiveExpression(propertyDescriptions.Key),
                                new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression(listVar.Name))
                                ),
                            CodeBinaryOperatorType.ValueEquality,
                            new CodePrimitiveExpression(false)

                            ),
                        new CodeAssignStatement(new CodeVariableReferenceExpression(listVar.Name),
                                                new CodeObjectCreateExpression(
                                                    new CodeTypeReference(typeof(List<string>)))),
                        new CodeExpressionStatement(new CodeMethodInvokeExpression(
                                                        new CodeFieldReferenceExpression(
                                                            new CodeThisReferenceExpression(), dicFieldName
                                                            ),
                                                        "Add",
                                                        new CodePrimitiveExpression(propertyDescriptions.Key),
                                                        new CodeVariableReferenceExpression(listVar.Name))

                            ))
                    );

                foreach (var propertyDescription in propertyDescriptions.Value)
                {
                    inlockStatemets.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(listVar.Name), "Add",
                       WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(_settings,propertyDescription)));
                }
            }
            // List<string[]> res = new List<string[]>();
            // foreach(List<string> lst in m_GetDefferedLoadPropertiesGroupsDic.Values)
            // {
            //		res.Add(lst.ToArray());
            // }
            // m_GetDefferedLoadPropertiesGroups = res.ToArray()


            inlockStatemets.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(List<string[]>)), "res", new CodeObjectCreateExpression(new CodeTypeReference(typeof(List<string[]>)))));
            inlockStatemets.Add(
                //OrmCodeDomGenerator.Delegates.CodePatternForeachStatement(
                //    new CodeTypeReference(typeof(List<string>)), "l",
                //    new CodePropertyReferenceExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName + "Dic"), "Values"),
                Emit.@foreach("l", ()=>CodeDom.@this.Field<IDictionary<string, List<string>>>(fieldName + "Dic").Values,
                    new CodeExpressionStatement(new CodeMethodInvokeExpression(
                        new CodeVariableReferenceExpression("res"),
                        "Add",
                        new CodeMethodInvokeExpression(
                            new CodeArgumentReferenceExpression("l"),
                            "ToArray"
                        )
                    ))
                 )
            );

            inlockStatemets.Add(
                new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName),
                                        new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("res"), "ToArray")));


            //inlockStatemets.Add(new CodeVariableDeclarationStatement(
            //                            method.ReturnType,
            //                            "groups",
            //                            array
            //                            ));

            if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.CompleteEntity.HasDefferedLoadableProperties)
            {
                //method.Attributes |= MemberAttributes.Override;

                //// string[][] baseArray;
                //var tempVar = new CodeVariableDeclarationStatement(method.ReturnType, "baseGroups");

                //inlockStatemets.Add(tempVar);
                //// baseArray = base.GetDefferedLoadPropertiesGroups()
                //inlockStatemets.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("baseGroups"),
                //                                              new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(),
                //                                                                             method.Name)));

                //// Array.Resize<string[]>(ref groups, baseGroups.Length, groups.Length)
                //inlockStatemets.Add(
                //    new CodeMethodInvokeExpression(
                //        new CodeMethodReferenceExpression(
                //            new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Array))),
                //            "Resize",
                //            new CodeTypeReference(typeof(string[]))),
                //        new CodeDirectionExpression(FieldDirection.Ref,
                //                                    new CodeVariableReferenceExpression("groups")),
                //        new CodeBinaryOperatorExpression(
                //            new CodePropertyReferenceExpression(
                //                new CodeVariableReferenceExpression("baseGroups"),
                //                "Length"
                //                ),
                //            CodeBinaryOperatorType.Add,
                //            new CodePropertyReferenceExpression(
                //                new CodeVariableReferenceExpression("groups"),
                //                "Length"
                //                )
                //            )
                //        )
                //    );
                //// Array.Copy(baseGroups, 0, groups, groups.Length - baseGroups.Length, baseGroups.Length)
                //inlockStatemets.Add(
                //    new CodeMethodInvokeExpression(
                //        new CodeTypeReferenceExpression(typeof(Array)),
                //        "Copy",
                //        new CodeVariableReferenceExpression("baseGroups"),
                //        new CodePrimitiveExpression(0),
                //        new CodeVariableReferenceExpression("groups"),
                //        new CodeBinaryOperatorExpression(
                //            new CodePropertyReferenceExpression(
                //                new CodeVariableReferenceExpression("groups"), "Length"),
                //            CodeBinaryOperatorType.Subtract,
                //            new CodePropertyReferenceExpression(
                //                new CodeVariableReferenceExpression("baseGroups"), "Length")
                //            ),
                //        new CodePropertyReferenceExpression(
                //            new CodeVariableReferenceExpression("baseGroups"), "Length")
                //        )
                //    );
            }
            else
            {
                method.ImplementationTypes.Add(new CodeTypeReference(typeof(Worm.Entities.Meta.IDefferedLoading)));
            }

            //inlockStatemets.Add(
            //        new CodeAssignStatement(
            //            ,
            //            new CodeVariableReferenceExpression("groups")
            //            )
            //        );

            List<CodeStatement> statements = new List<CodeStatement>(inlockStatemets.Count);
            foreach (CodeStatement statemet in inlockStatemets)
            {
                statements.Add(statemet);
            }
            method.Statements.Add(
                WormCodeDomGenerator.CodePatternDoubleCheckLock(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        lockObj.Name
                        ),
                    condition,
                    statements.ToArray()
                    )
                );



            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)
                )
            );

            Members.Add(method);
        }

        private void OnPopulateM2mRealationsInterface()
        {
            if (m_entityClass.Entity.GetAllRelations(false).Count == 0)
                return;
            if (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.CompleteEntity.GetAllRelations(false).Count > 0)
                return;

            BaseTypes.Add(new CodeTypeReference(typeof(ISchemaWithM2M)));
        }

        private void OnPopulateBaseClass()
        {
            if (EntityClass.Entity.BaseEntity != null)
                BaseTypes.Add(new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(_settings).GetEntitySchemaDefClassQualifiedName(EntityClass.Entity.BaseEntity)));
        }

        private void OnPupulateSchemaInterfaces()
        {
            if (EntityClass.Entity.BaseEntity == null)
            {
                BaseTypes.Add(new CodeTypeReference(typeof(Worm.Entities.Meta.IEntitySchemaBase)));
                BaseTypes.Add(new CodeTypeReference(typeof(Worm.Entities.Meta.ISchemaInit)));
            }
        }

        private void OnPopulateIDefferedLoadingInterface()
        {
            if (m_entityClass == null || m_entityClass.Entity == null || !m_entityClass.Entity.HasDefferedLoadableProperties || (m_entityClass.Entity.BaseEntity != null && m_entityClass.Entity.BaseEntity.CompleteEntity.HasDefferedLoadableProperties))
                return;

            BaseTypes.Add(new CodeTypeReference(typeof(Worm.Entities.Meta.IDefferedLoading)));
        }

        public CodeSchemaDefTypeDeclaration(WXMLCodeDomGeneratorSettings settings, CodeEntityTypeDeclaration entityClass)
            : this(settings)
        {
            EntityClass = entityClass;
        }

        public CodeEntityTypeDeclaration EntityClass
        {
            get
            {
                return m_entityClass;
            }
            set
            {
                m_entityClass = value;
                RenewEntityClass();
            }
        }

        public CodeTypeReference TypeReference
        {
            get { return m_typeReference; }
        }

        public new string Name
        {
            get
            {
                if (m_entityClass != null && m_entityClass.Entity != null)
                    return new WXMLCodeDomGeneratorNameHelper(_settings).GetEntitySchemaDefClassName(m_entityClass.Entity);
                return null;
            }
        }

        public string FullName
        {
            get
            {
                if (m_entityClass != null && m_entityClass.Entity != null)
                    return new WXMLCodeDomGeneratorNameHelper(_settings).GetEntitySchemaDefClassQualifiedName(m_entityClass.Entity);
                return null;
            }
        }

        protected void RenewEntityClass()
        {
            base.Name = Name;
            m_typeReference.BaseType = FullName;

            IsPartial = m_entityClass.IsPartial;
            Attributes = m_entityClass.Attributes;
            if (m_entityClass.Entity.BaseEntity != null &&
                Name == new WXMLCodeDomGeneratorNameHelper(_settings).GetEntitySchemaDefClassName(m_entityClass.Entity.BaseEntity))
                Attributes |= MemberAttributes.New;
        }

    }
}
