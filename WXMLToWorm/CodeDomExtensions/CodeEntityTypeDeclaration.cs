using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using WXML.Model.Descriptors;
using LinqToCodedom.Generator;
using Worm.Entities.Meta;
using WXML.Model;
using Worm.Query;
using WXML.CodeDom;
using LinqToCodedom.Extensions;

namespace WXMLToWorm.CodeDomExtensions
{
    /// <summary>
    /// Обертка над <see cref="CodeTypeDeclaration"/> применительно к <see cref="EntityDefinition"/>
    /// </summary>
    public class CodeEntityTypeDeclaration : CodeTypeDeclaration
    {
        private EntityDefinition _entity;
        private CodeEntityInterfaceDeclaration m_entityInterface;
        private readonly CodeTypeReference m_typeReference;
        private CodeSchemaDefTypeDeclaration m_schema;
        private readonly Dictionary<string, CodePropertiesAccessorTypeDeclaration> m_propertiesAccessor;
        //private readonly bool _useType;
        private readonly WXMLCodeDomGeneratorSettings _settings;
        private readonly WormCodeDomGenerator _gen;

        public CodeEntityTypeDeclaration(WXMLCodeDomGeneratorSettings settings, WormCodeDomGenerator gen)
        {
            m_typeReference = new CodeTypeReference();
            m_propertiesAccessor = new Dictionary<string, CodePropertiesAccessorTypeDeclaration>();
            PopulateMembers += OnPopulateMembers;
            //_useType = useType;
            _settings = settings;
            _gen = gen;
        }

        protected virtual void OnPopulateMembers(object sender, EventArgs e)
        {
            if ((_settings.GenerateMode.HasValue ? _settings.GenerateMode.Value : _entity.Model.GenerateMode) != GenerateModeEnum.SchemaOnly)
            {
                PopulateDontRaise();
                OnPopulatePropertiesAccessors();
                if (_entity.GetPkProperties().Count() > 0 &&
                    (_settings.GenerateMode.HasValue ? _settings.GenerateMode.Value : _entity.Model.GenerateMode) != GenerateModeEnum.EntityOnly)
                {
                    OnPupulateEntityRelations();
                    OnPupulateM2MRelations();
                }
            }

            if ((_settings.GenerateMode.HasValue ? _settings.GenerateMode.Value : _entity.Model.GenerateMode) != GenerateModeEnum.EntityOnly)
            {
                if (_entity.NeedOwnSchema())
                    OnPopulateSchema();
            }
            //else
            //    throw new NotImplementedException();
        }

        private void PopulateDontRaise()
        {
            if (_entity.GetPkProperties().Count() == 0)
                return;

            var prop = new CodeMemberProperty()
            {
                Attributes = MemberAttributes.Override | MemberAttributes.Public,
                Name = "DontRaisePropertyChange",
                Type = new CodeTypeReference(typeof(bool)),
                HasSet = false,
                HasGet = true
            };

            Members.Add(prop);

            prop.GetStatements.Add(
                Emit.@return(()=>true)
            );
        }

        protected virtual void OnPopulateSchema()
        {
            Members.Add(SchemaDef);

            #region энам табличек

            CreateTablesLinkEnum(_entity, SchemaDef);

            #endregion энам табличек

            //#region bool ChangeValueType(EntityPropertyAttribute c, object value, ref object newvalue)

            //CreateChangeValueTypeMethod(_entity, SchemaDef);

            //#endregion bool ChangeValueType(EntityPropertyAttribute c, object value, ref object newvalue)

            //#region string[] GetSuppressedColumns()

            //// первоначальная реализация или есть отличие в suppressed properties
            //if (_entity.BaseEntity == null ||
            //    (_entity.BaseEntity.SuppressedProperties.Count + _entity.SuppressedProperties.Count != 0) ||
            //    _entity.BaseEntity.SuppressedProperties.Count != _entity.SuppressedProperties.Count ||
            //    !(_entity.SuppressedProperties.TrueForAll(p => _entity.BaseEntity.SuppressedProperties.Exists(pp => pp == p)) &&
            //    _entity.BaseEntity.SuppressedProperties.TrueForAll(p => _entity.SuppressedProperties.Exists(pp => pp == p))))
            //{

            //    var method = new CodeMemberMethod
            //    {
            //        Name = "GetSuppressedFields",
            //        // тип возвращаемого значения
            //        ReturnType = new CodeTypeReference(typeof(string[])),
            //        // модификаторы доступа
            //        Attributes = MemberAttributes.Public
            //    };

            //    SchemaDef.Members.Add(method);
            //    if (_entity.BaseEntity != null)
            //        method.Attributes |= MemberAttributes.Override;
            //    else
            //        // реализует метод базового класса
            //        method.ImplementationTypes.Add(typeof(IEntitySchemaBase));
            //    CodeArrayCreateExpression arrayExpression = new CodeArrayCreateExpression(
            //        new CodeTypeReference(typeof(string[]))
            //    );


            //    foreach (var suppressedProperty in _entity.SuppressedProperties)
            //    {
            //        arrayExpression.Initializers.Add(
            //            new CodePrimitiveExpression(suppressedProperty)
            //        );
            //    }

            //    method.Statements.Add(new CodeMethodReturnStatement(arrayExpression));
            //}

            //#endregion EntityPropertyAttribute[] GetSuppressedColumns()

            #region сущность реализует связь

            RelationDefinitionBase relation = _entity.Model.GetActiveRelations()
                .SingleOrDefault(item => item.UnderlyingEntity == _entity);

            if (relation != null)
            {
                SelfRelationDefinition sd = relation as SelfRelationDefinition;
                if (sd == null)
                    ImplementIRelation((RelationDefinition)relation, _entity, SchemaDef);
                else
                    ImplementIRelation(sd, _entity, SchemaDef);
            }

            #endregion сущность реализует связь

            //#region public void GetSchema(OrmSchemaBase schema, Type t)

            //if (_entity.BaseEntity == null)
            //{
            //    CodeMemberField schemaField = new CodeMemberField(
            //        new CodeTypeReference(typeof(Worm.ObjectMappingEngine)),
            //        "_schema"
            //        );
            //    CodeMemberField typeField = new CodeMemberField(
            //        new CodeTypeReference(typeof(Type)),
            //        "_entityType"
            //        );
            //    schemaField.Attributes = MemberAttributes.Family;
            //    SchemaDef.Members.Add(schemaField);
            //    typeField.Attributes = MemberAttributes.Family;
            //    SchemaDef.Members.Add(typeField);
            //    var method = new CodeMemberMethod
            //    {
            //        Name = "GetSchema",
            //        // тип возвращаемого значения
            //        ReturnType = null,
            //        // модификаторы доступа
            //        Attributes = MemberAttributes.Public | MemberAttributes.Final
            //    };

            //    SchemaDef.Members.Add(method);

            //    method.Parameters.Add(
            //        new CodeParameterDeclarationExpression(
            //            new CodeTypeReference(typeof(Worm.ObjectMappingEngine)),
            //            "schema"
            //            )
            //        );
            //    method.Parameters.Add(
            //        new CodeParameterDeclarationExpression(
            //            new CodeTypeReference(typeof(Type)),
            //            "t"
            //            )
            //        );
            //    // реализует метод базового класса
            //    method.ImplementationTypes.Add(typeof(ISchemaInit));
            //    method.Statements.Add(
            //        new CodeAssignStatement(
            //            new CodeFieldReferenceExpression(
            //                new CodeThisReferenceExpression(),
            //                "_schema"
            //                ),
            //            new CodeArgumentReferenceExpression("schema")
            //            )
            //        );
            //    method.Statements.Add(
            //        new CodeAssignStatement(
            //            new CodeFieldReferenceExpression(
            //                new CodeThisReferenceExpression(),
            //                "_entityType"
            //                ),
            //            new CodeArgumentReferenceExpression("t")
            //            )
            //        );
            //}

            //#endregion public void GetSchema(OrmSchemaBase schema, Type t)

        }

        private static void CreateTablesLinkEnum(EntityDefinition entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            if (!entity.InheritsBaseTables || entity.GetSourceFragments().Count() > 0)
            {
                var fullTables = entity.GetSourceFragments();

                CodeTypeDeclaration tablesEnum = new CodeTypeDeclaration("TablesLink")
                {
                    Attributes = MemberAttributes.Public,
                    IsClass = false,
                    IsEnum = true,
                    IsPartial = false
                };

                if (entity.BaseEntity != null)
                    tablesEnum.Attributes |= MemberAttributes.New;

                int tableNum = 0;

                tablesEnum.Members.AddRange(fullTables.Select(tbl => new CodeMemberField
                {
                    InitExpression = new CodePrimitiveExpression(tableNum++),
                    Name = WXMLCodeDomGeneratorNameHelper.GetSafeName(tbl.Identifier)
                }).ToArray());
                entitySchemaDefClass.Members.Add(tablesEnum);
            }
        }

        private void ImplementIRelation(RelationDefinition relation, EntityDefinition entity,
            CodeTypeDeclaration entitySchemaDefClass)
        {
            var leftProp = entity.GetActiveProperties().OfType<EntityPropertyDefinition>().SingleOrDefault(item =>
                item.SourceFields.Any(sf => relation.Left.FieldName.Contains(sf.SourceFieldExpression)));

            var rightProp = entity.GetActiveProperties().OfType<EntityPropertyDefinition>().SingleOrDefault(item =>
                item.SourceFields.Any(sf => relation.Right.FieldName.Contains(sf.SourceFieldExpression)));

            if (leftProp != null && rightProp != null)
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));

                #region Pair<string, Type> GetFirstType()
                CodeMemberMethod method = new CodeMemberMethod
                {
                    Name = "GetFirstType",
                    // тип возвращаемого значения
                    ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc)),
                    // модификаторы доступа
                    Attributes = MemberAttributes.Public
                };
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IRelation));

                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(IRelation.RelationDesc)),
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, leftProp, false),
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(
                                    new CodeFieldReferenceExpression(
                                        new CodeThisReferenceExpression(),
                                        "_schema"
                                        ),
                                    "GetTypeByEntityName"
                                    ),
                                new CodePrimitiveExpression(relation.Right.Entity.Name)
                                )
                            )
                        )
                    );
                entitySchemaDefClass.Members.Add(method);
                #endregion Pair<string, Type> GetFirstType()

                #region Pair<string, Type> GetSecondType()
                method = new CodeMemberMethod
                {
                    Name = "GetSecondType",
                    // тип возвращаемого значения
                    ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc)),
                    // модификаторы доступа
                    Attributes = MemberAttributes.Public
                };
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IRelation));
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(IRelation.RelationDesc)),
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, rightProp, false),
                            new CodeMethodInvokeExpression(
                                new CodeMethodReferenceExpression(
                                    new CodeFieldReferenceExpression(
                                        new CodeThisReferenceExpression(),
                                        "_schema"
                                        ),
                                    "GetTypeByEntityName"
                                    ),
                                new CodePrimitiveExpression(relation.Left.Entity.Name)
                                )
                            )
                        )
                    );
                entitySchemaDefClass.Members.Add(method);
                #endregion Pair<string, Type> GetSecondType()
            }
        }

        private void ImplementIRelation(SelfRelationDefinition relation, EntityDefinition entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));

            #region Pair<string, Type> GetFirstType()
            CodeMemberMethod method = new CodeMemberMethod
            {
                Name = "GetFirstType",
                // тип возвращаемого значения
                ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc)),
                // модификаторы доступа
                Attributes = MemberAttributes.Public
            };
            // реализует метод базового класса
            method.ImplementationTypes.Add(typeof(IRelation));
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(IRelation.RelationDesc)),
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings,
                            entity.GetActiveProperties().OfType<EntityPropertyDefinition>().SingleOrDefault(item =>
                                item.SourceFields.Any(sf => relation.Direct.FieldName.Contains(sf.SourceFieldExpression)))
                        , false),
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "_schema"
                                ),
                                "GetTypeByEntityName"
                            ),
                            new CodePrimitiveExpression(relation.Entity.Name)
                        ),
                        new CodePrimitiveExpression(true)
                    )
                )
            );
            entitySchemaDefClass.Members.Add(method);
            #endregion Pair<string, Type> GetFirstType()

            #region Pair<string, Type> GetSecondType()
            method = new CodeMemberMethod
            {
                Name = "GetSecondType",
                // тип возвращаемого значения
                ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc)),
                // модификаторы доступа
                Attributes = MemberAttributes.Public
            };
            // реализует метод базового класса
            method.ImplementationTypes.Add(typeof(IRelation));
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(IRelation.RelationDesc)),
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings,
                            entity.GetActiveProperties().OfType<EntityPropertyDefinition>().SingleOrDefault(item =>
                                item.SourceFields.Any(sf => relation.Reverse.FieldName.Contains(sf.SourceFieldExpression)))
                        , false),
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "_schema"
                                ),
                                "GetTypeByEntityName"
                            ),
                            new CodePrimitiveExpression(relation.Entity.Name)
                        ),
                        new CodePrimitiveExpression(false)
                    )
                )
            );
            entitySchemaDefClass.Members.Add(method);
            #endregion Pair<string, Type> GetSecondType()
        }

        protected virtual void OnPupulateM2MRelations()
        {
            var relationDescType = new CodeTypeReference(typeof(RelationDescEx));

            #region Relation
            foreach (var relation in _entity.GetM2MRelations(false))
            {
                if (relation.Left.Entity == relation.Right.Entity)
                    throw new ArgumentException("To realize m2m relation on self use SelfRelation instead.");

                LinkTarget link = relation.Left.Entity == _entity ? relation.Right : relation.Left;

                var accessorName = link.AccessorName;
                var relatedEntity = link.Entity;

                if (string.IsNullOrEmpty(accessorName))
                {
                    // существуют похожие релейшены, но не имеющие имени акссесора
                    var lst =
                        link.Entity.GetM2MRelations(false).FindAll(
                            r =>
                            r.Left != link && r.Right != link &&
                            ((r.Left.Entity == _entity && string.IsNullOrEmpty(r.Right.AccessorName))
                                || (r.Right.Entity == _entity && string.IsNullOrEmpty(r.Left.AccessorName))));

                    if (lst.Count > 0)
                        throw new WXMLException(
                            string.Format(
                                "Существуют неоднозначные связи между '{0}' и '{1}'. конкретизируйте их через accessorName.",
                                lst[0].Left.Entity.Name, lst[0].Right.Entity.Name));
                    accessorName = relatedEntity.Name;
                }
                accessorName = WXMLCodeDomGeneratorNameHelper.GetMultipleForm(accessorName);

                var entityTypeExpression = Settings.UseTypeInProps ? WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(_settings, relatedEntity, relatedEntity.Namespace != _entity.Namespace) : WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings, relatedEntity, relatedEntity.Namespace != _entity.Namespace);

                var desc = new CodeObjectCreateExpression(
                    new CodeTypeReference(typeof(M2MRelationDesc)),
                    entityTypeExpression);

                var staticProperty = new CodeMemberProperty
                {
                    Name = accessorName + "Relation",
                    HasGet = true,
                    HasSet = false,
                    Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                    Type = relationDescType
                };

                staticProperty.GetStatements.Add(new CodeMethodReturnStatement(
                    new CodeObjectCreateExpression(typeof(RelationDescEx),
                    new CodeObjectCreateExpression(typeof(EntityUnion),
                        WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings,_entity, false)
                    ), desc)
                ));

                desc.Parameters.Add(new CodePrimitiveExpression(relation.SourceFragment.Identifier));

                Members.Add(staticProperty);

                GetRelationMethods(relation.SourceFragment.Identifier, staticProperty.Name);

                var memberProperty = new CodeMemberProperty
                {
                    Name = accessorName,
                    HasGet = true,
                    HasSet = false,
                    Attributes =
                        MemberAttributes.Public | MemberAttributes.Final,
                    Type = new CodeTypeReference(typeof(RelationCmd))
                };

                if (!string.IsNullOrEmpty(link.AccessorDescription))
                    WXMLCodeDomGenerator.SetMemberDescription(memberProperty, link.AccessorDescription);

                memberProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),
                            "GetCmd",
                            new CodePropertyReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    null, //WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(_settings, _entity, false),
                                    staticProperty.Name
                                ),
                                "M2MRel"
                            )
                        )
                    )
                );

                Members.Add(memberProperty);

                _gen.RaisePropertyCreated(null, this, memberProperty, null);
            }

            #endregion

            #region SelfRelation
            foreach (var relation in _entity.GetM2MSelfRelations(false))
            {
                var accessorName = relation.Direct.AccessorName;

                if (!string.IsNullOrEmpty(accessorName))
                {
                    var entityTypeExpression = Settings.UseTypeInProps ? WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(_settings, _entity, false) : WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings, _entity, false);

                    var desc = new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(M2MRelationDesc)),
                        entityTypeExpression);

                    accessorName = WXMLCodeDomGeneratorNameHelper.GetMultipleForm(accessorName);

                    var staticProperty = new CodeMemberProperty
                    {
                        Name = accessorName + "Relation",
                        HasGet = true,
                        HasSet = false,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                        Type = relationDescType
                    };

                    staticProperty.GetStatements.Add(new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(typeof(RelationDescEx),
                        new CodeObjectCreateExpression(typeof(EntityUnion),
                            WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings,_entity, false)
                        ), desc)
                    ));

                    GetRelationMethods(relation.SourceFragment.Identifier, staticProperty.Name);

                    //desc.Parameters.Add(new CodePrimitiveExpression(relation.Direct.FieldName));
                    //desc.Parameters.Add(new CodeFieldReferenceExpression(
                    //    new CodeTypeReferenceExpression(typeof(M2MRelationDesc)),"DirKey")
                    //);
                    desc.Parameters.Add(new CodePrimitiveExpression(relation.SourceFragment.Identifier));

                    Members.Add(staticProperty);
                    
                    var memberProperty = new CodeMemberProperty
                    {
                        Name = accessorName,
                        HasGet = true,
                        HasSet = false,
                        Attributes =
                            MemberAttributes.Public | MemberAttributes.Final,
                        Type = new CodeTypeReference(typeof(RelationCmd))
                    };

                    if (!string.IsNullOrEmpty(relation.Direct.AccessorDescription))
                        WXMLCodeDomGenerator.SetMemberDescription(memberProperty, relation.Direct.AccessorDescription);

                    memberProperty.GetStatements.Add(
                        new CodeMethodReturnStatement(
                            new CodeMethodInvokeExpression(
                                new CodeThisReferenceExpression(),
                                "GetCmd",
                                new CodePropertyReferenceExpression(
                                    new CodePropertyReferenceExpression(
                                        null, //WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(_settings, _entity, false),
                                        staticProperty.Name
                                    ),
                                    "M2MRel"
                                )
                            )
                        )
                    );

                    Members.Add(memberProperty);

                    _gen.RaisePropertyCreated(null, this, memberProperty, null);
                }

                accessorName = relation.Reverse.AccessorName;

                if (!string.IsNullOrEmpty(accessorName))
                {
                    var entityTypeExpression = WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings,_entity,false);
                    var desc = new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(M2MRelationDesc)),
                        entityTypeExpression);

                    accessorName = WXMLCodeDomGeneratorNameHelper.GetMultipleForm(accessorName);

                    var staticProperty = new CodeMemberProperty
                    {
                        Name = accessorName + "Relation",
                        HasGet = true,
                        HasSet = false,
                        Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                        Type = relationDescType
                    };

                    staticProperty.GetStatements.Add(new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(typeof(RelationDescEx), 
                        new CodeObjectCreateExpression(typeof(EntityUnion),
                            WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings,_entity,false)
                        ), desc)
                    ));
                    //desc.Parameters.Add(new CodePrimitiveExpression(relation.Reverse.FieldName));
                    //desc.Parameters.Add(new CodeFieldReferenceExpression(
                    //    new CodeTypeReferenceExpression(typeof(M2MRelationDesc)),"RevKey")
                    //);
                    desc.Parameters.Add(new CodePrimitiveExpression(M2MRelationDesc.ReversePrefix+relation.SourceFragment.Identifier));

                    GetRelationMethods(relation.SourceFragment.Identifier, staticProperty.Name);

                    Members.Add(staticProperty);
                    
                    var memberProperty = new CodeMemberProperty
                    {
                        Name = accessorName,
                        HasGet = true,
                        HasSet = false,
                        Attributes =
                            MemberAttributes.Public | MemberAttributes.Final,
                        Type = new CodeTypeReference(typeof(RelationCmd))
                    };

                    if (!string.IsNullOrEmpty(relation.Reverse.AccessorDescription))
                        WXMLCodeDomGenerator.SetMemberDescription(memberProperty, relation.Reverse.AccessorDescription);

                    memberProperty.GetStatements.Add(
                        new CodeMethodReturnStatement(
                            new CodeMethodInvokeExpression(
                                new CodeThisReferenceExpression(),
                                "GetCmd",
                                new CodePropertyReferenceExpression(
                                    new CodePropertyReferenceExpression(
                                        null, //WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(_settings, _entity, false),
                                        staticProperty.Name
                                    ),
                                    "M2MRel"
                                )
                            )
                        )
                    );

                    Members.Add(memberProperty);

                    _gen.RaisePropertyCreated(null, this, memberProperty, null);
                }
            }
            
            #endregion

        }

        private void GetRelationMethods(string relationIdentifier, string propName)
        {
            if (Settings.UseTypeInProps)
            {
                string cln = new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(_entity, false);
                //string dn = cln + ".Descriptor";

                Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                    typeof(RelationDescEx),
                    (EntityUnion hostEntity) => "Get" + propName,
                    Emit.@return((EntityUnion hostEntity) =>
                        new RelationDescEx(hostEntity, new M2MRelationDesc(
                            new EntityUnion(CodeDom.TypeOf(cln)),
                            relationIdentifier
                        ))
                    )
                ));
            }
            else
            {
                //string cln = new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(_entity, true);
                const string dn = "Descriptor";//cln + ".Descriptor";

                Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                    typeof(RelationDescEx),
                    (EntityUnion hostEntity) => "Get" + propName,
                    Emit.@return((EntityUnion hostEntity) =>
                        new RelationDescEx(hostEntity, new M2MRelationDesc(
                            new EntityUnion(CodeDom.Field<string>(CodeDom.TypeRef_str(dn), "EntityName")),
                            relationIdentifier
                        ))
                    )
                ));
            }

            Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                typeof(RelationDescEx),
                (EntityUnion hostEntity, EntityUnion joinEntity) => "Get" + propName,
                Emit.@return((EntityUnion hostEntity, EntityUnion joinEntity) =>
                    new RelationDescEx(hostEntity, new M2MRelationDesc(
                        joinEntity,
                        relationIdentifier
                    ))
                )
            ));
        }

        protected virtual void OnPupulateEntityRelations()
        {
            var relationDescType = new CodeTypeReference(typeof(RelationDescEx));

            foreach (var entityRelation in _entity.GetActiveOne2ManyRelations())
            {
                string accessorName = string.IsNullOrEmpty(entityRelation.AccessorName) ? WXMLCodeDomGeneratorNameHelper.GetMultipleForm(entityRelation.Entity.Name) : entityRelation.AccessorName;

                var staticProperty = new CodeMemberProperty
                 {
                     Name = accessorName + "Relation",
                     HasGet = true,
                     HasSet = false,
                     Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                     Type = relationDescType
                 };

                var entityTypeExpression = Settings.UseTypeInProps ? WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(_settings, entityRelation.Entity, entityRelation.Entity.Namespace != _entity.Namespace || _entity.ScopeNames().Any(item => item == entityRelation.Entity.Name)) : WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings, entityRelation.Entity, entityRelation.Entity.Namespace != _entity.Namespace || _entity.ScopeNames().Any(item => item == entityRelation.Entity.Name));
                var selfEntityTypeExpression = Settings.UseTypeInProps ? WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(_settings, _entity, false) : WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(_settings, _entity, false);

                staticProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            relationDescType,
                            new CodeObjectCreateExpression(
                                typeof(EntityUnion),
                                selfEntityTypeExpression
                            ),
                            new CodeObjectCreateExpression(
                                typeof(RelationDesc),
                                new CodeObjectCreateExpression(
                                    new CodeTypeReference(typeof(EntityUnion)),
                                    entityTypeExpression
                                ),
                                WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(_settings, entityRelation.Property, entityRelation.Entity.Namespace != _entity.Namespace || _entity.ScopeNames().Any(item => item == entityRelation.Entity.Name)),
                                new CodePrimitiveExpression(entityRelation.Name ?? "default")
                            )
                        )
                    )
                );

                Members.Add(staticProperty);

                string cd = new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(entityRelation.Property.Entity, entityRelation.Entity.Namespace != _entity.Namespace || _entity.ScopeNames().Any(item => item == entityRelation.Entity.Name)) + ".Properties";
                //string dn = new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(entityRelation.Entity, true) + ".Descriptor";

                //CodeDom.Field<string>(CodeDom.TypeRef(dn), "EntityName")

                CodeExpression exp = CodeDom.GetExpression((EntityUnion hostEntity) =>
                    new RelationDescEx(hostEntity, new RelationDesc(
                        new EntityUnion(CodeDom.InjectExp<string>(0)),
                        CodeDom.Field<string>(CodeDom.TypeRef_str(cd), entityRelation.Property.PropertyAlias),
                        entityRelation.Name
                )), entityTypeExpression);

                Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static, 
                    typeof(RelationDescEx),
                    (EntityUnion hostEntity)=>"Get" + staticProperty.Name,
                    Emit.@return(exp)
                ));

                Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                    typeof(RelationDescEx),
                    (EntityUnion hostEntity, EntityUnion joinEntity) => "Get" + staticProperty.Name,
                    Emit.@return((EntityUnion hostEntity, EntityUnion joinEntity) =>
                        new RelationDescEx(hostEntity, new RelationDesc(
                            joinEntity,
                            CodeDom.Field<string>(CodeDom.TypeRef_str(cd), entityRelation.Property.PropertyAlias),
                            entityRelation.Name
                        ))
                    )
                ));

                var memberProperty = new CodeMemberProperty
                 {
                     Name = accessorName,
                     HasGet = true,
                     HasSet = false,
                     Attributes =
                         MemberAttributes.Public | MemberAttributes.Final,
                     Type = new CodeTypeReference(typeof(RelationCmd))
                 };

                if (!string.IsNullOrEmpty(entityRelation.AccessorDescription))
                    WXMLCodeDomGenerator.SetMemberDescription(memberProperty, entityRelation.AccessorDescription);

                memberProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeThisReferenceExpression(),
                            "GetCmd",
                            new CodePropertyReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    null, //WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(_settings, _entity, false),
                                    staticProperty.Name
                                ),
                                "Rel"
                            )
                        )
                    )
                );
                Members.Add(memberProperty);

                _gen.RaisePropertyCreated(null, this, memberProperty, null);
            }
        }

        protected virtual void OnPopulatePropertiesAccessors()
        {
            foreach (var propertyDescription in Entity.GetActiveProperties())
            {
                if (propertyDescription.Group == null)
                    continue;
                CodePropertiesAccessorTypeDeclaration accessor;
                if (!m_propertiesAccessor.TryGetValue(propertyDescription.Group.Name, out accessor))
                    m_propertiesAccessor[propertyDescription.Group.Name] =
                        new CodePropertiesAccessorTypeDeclaration(_settings,Entity, propertyDescription.Group);

            }
            foreach (var accessor in m_propertiesAccessor.Values)
            {
                Members.Add(accessor);
                //var field = new CodeMemberField(new CodeTypeReference(accessor.FullName),
                //                                OrmCodeGenNameHelper.GetPrivateMemberName(accessor.Name));
                //field.InitExpression = new CodeObjectCreateExpression(field.Type, new CodeThisReferenceExpression());

                var property = new CodeMemberProperty
                                   {
                                       Type = new CodeTypeReference(accessor.FullName),
                                       Name = accessor.Group.Name,
                                       HasGet = true,
                                       HasSet = false
                                   };
                property.GetStatements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(property.Type, new CodeThisReferenceExpression())));
                Members.Add(property);
            }

        }

        public CodeEntityTypeDeclaration(WXMLCodeDomGeneratorSettings settings, EntityDefinition entity, WormCodeDomGenerator gen)
            : this(settings, gen)
        {
            Entity = entity;
            m_typeReference.BaseType = FullName;
            entity.Items["TypeDeclaration"] = this;
        }

        public CodeSchemaDefTypeDeclaration SchemaDef
        {
            get
            {
                if (m_schema == null)
                {
                    m_schema = new CodeSchemaDefTypeDeclaration(_settings, this);
                }
                return m_schema;
            }
        }

        public new string Name
        {
            get
            {
                if (Entity != null)
                    return new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(Entity, false);
                return null;
            }
        }

        public string FullName
        {
            get
            {
                return new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(Entity, true);
            }
        }

        public EntityDefinition Entity
        {
            get { return _entity; }
            set
            {
                _entity = value;
                EnsureName();
            }
        }

        public CodeEntityInterfaceDeclaration EntityInterfaceDeclaration
        {
            get
            {
                return m_entityInterface;
            }
            set
            {
                if (m_entityInterface != null)
                {
                    m_entityInterface.EnsureData();
                    // удалить существующий из списка дочерних типов
                    if (this.BaseTypes.Contains(m_entityInterface.TypeReference))
                    {
                        BaseTypes.Remove(m_entityInterface.TypeReference);
                    }

                }
                m_entityInterface = value;
                if (m_entityInterface != null)
                {
                    ((CodeTypeDeclaration)this).Implements(m_entityInterface.TypeReference);
                    //BaseTypes.Add(m_entityInterface.TypeReference);
                    m_entityInterface.EnsureData();
                }
            }
        }

        public CodeEntityInterfaceDeclaration EntityPropertiesInterfaceDeclaration { get; set; }

        public CodeSchemaDefTypeDeclaration SchemaDefDeclaration { get; set; }

        public CodeTypeReference TypeReference
        {
            get { return m_typeReference; }
        }

        protected void EnsureName()
        {
            base.Name = Name;
            m_typeReference.BaseType = FullName;
        }

        public WXMLCodeDomGeneratorSettings Settings
        {
            get
            {
                return _settings;
            }
        }
    }
}
