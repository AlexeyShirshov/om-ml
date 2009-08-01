using System;
using System.CodeDom;
using System.Collections.Generic;
using WXML.Model.Descriptors;
using LinqToCodedom.Generator;
using Worm.Entities.Meta;
using WXML.Model;
using Worm.Query;
using WXML.CodeDom;

namespace WXMLToWorm.CodeDomExtensions
{
    /// <summary>
    /// Обертка над <see cref="CodeTypeDeclaration"/> применительно к <see cref="EntityDescription"/>
    /// </summary>
    public class CodeEntityTypeDeclaration : CodeTypeDeclaration
    {
        private EntityDescription m_entity;
        private CodeEntityInterfaceDeclaration m_entityInterface;
        private readonly CodeTypeReference m_typeReference;
        private CodeSchemaDefTypeDeclaration m_schema;
        private readonly Dictionary<string, CodePropertiesAccessorTypeDeclaration> m_propertiesAccessor;
        private bool _useType;

        public CodeEntityTypeDeclaration(bool useType)
        {
            m_typeReference = new CodeTypeReference();
            m_propertiesAccessor = new Dictionary<string, CodePropertiesAccessorTypeDeclaration>();
            PopulateMembers += OnPopulateMembers;
            _useType = useType;
        }

        protected virtual void OnPopulateMembers(object sender, System.EventArgs e)
        {
            if (!m_entity.OrmObjectsDef.GenerateSchemaOnly)
            {
                OnPopulatePropertiesAccessors();
                if (m_entity.HasSinglePk)
                {
                    OnPupulateEntityRelations();
                    OnPupulateM2MRelations();
                }
            }
            OnPopulateSchema();
        }

        protected virtual void OnPopulateSchema()
        {
            Members.Add(SchemaDef);
        }

        protected virtual void OnPupulateM2MRelations()
        {
            var relationDescType = new CodeTypeReference(typeof(RelationDescEx));

            #region Relation
            foreach (var relation in m_entity.GetRelations(false))
            {
                if (relation.Left.Entity == relation.Right.Entity)
                    throw new ArgumentException("To realize m2m relation on self use SelfRelation instead.");

                LinkTarget link = relation.Left.Entity == m_entity ? relation.Right : relation.Left;

                var accessorName = link.AccessorName;
                var relatedEntity = link.Entity;

                if (string.IsNullOrEmpty(accessorName))
                {
                    // существуют похожие релейшены, но не имеющие имени акссесора
                    var lst =
                        link.Entity.GetRelations(false).FindAll(
                            r =>
                            r.Left != link && r.Right != link &&
                            ((r.Left.Entity == m_entity && string.IsNullOrEmpty(r.Right.AccessorName))
                                || (r.Right.Entity == m_entity && string.IsNullOrEmpty(r.Left.AccessorName))));

                    if (lst.Count > 0)
                        throw new OrmCodeGenException(
                            string.Format(
                                "Существуют неоднозначные связи между '{0}' и '{1}'. конкретизируйте их через accessorName.",
                                lst[0].Left.Entity.Name, lst[0].Right.Entity.Name));
                    accessorName = relatedEntity.Name;
                }
                accessorName = WXMLCodeDomGeneratorNameHelper.GetMultipleForm(accessorName);

                var entityTypeExpression = _useType ? WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(relatedEntity) : WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(relatedEntity);

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
                        WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(m_entity)
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
                                    WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(m_entity),
                                    staticProperty.Name
                                ),
                                "M2MRel"
                            )
                        )
                    )
                );

                Members.Add(memberProperty);

                WormCodeDomGenerator.RaisePropertyCreated(null, this, memberProperty, null);
            }

            #endregion

            #region SelfRelation
            foreach (var relation in m_entity.GetSelfRelations(false))
            {
                var accessorName = relation.Direct.AccessorName;

                if (!string.IsNullOrEmpty(accessorName))
                {
                    var entityTypeExpression = _useType ? WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(m_entity) : WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(m_entity);

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
                            WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(m_entity)
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
                                        WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(m_entity),
                                        staticProperty.Name
                                    ),
                                    "M2MRel"
                                )
                            )
                        )
                    );

                    Members.Add(memberProperty);

                    WormCodeDomGenerator.RaisePropertyCreated(null, this, memberProperty, null);
                }

                accessorName = relation.Reverse.AccessorName;

                if (!string.IsNullOrEmpty(accessorName))
                {
                    var entityTypeExpression = WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(m_entity);
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
                            WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(m_entity)
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
                                        WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(m_entity),
                                        staticProperty.Name
                                    ),
                                    "M2MRel"
                                )
                            )
                        )
                    );

                    Members.Add(memberProperty);

                    WormCodeDomGenerator.RaisePropertyCreated(null, this, memberProperty, null);
                }
            }
            
            #endregion

        }

        private void GetRelationMethods(string relationIdentifier, string propName)
        {
            string cln = WXMLCodeDomGeneratorNameHelper.GetEntityClassName(m_entity, true);
            string dn = cln + ".Descriptor";

            if (_useType)
            {
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
                Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                    typeof(RelationDescEx),
                    (EntityUnion hostEntity) => "Get" + propName,
                    Emit.@return((EntityUnion hostEntity) =>
                        new RelationDescEx(hostEntity, new M2MRelationDesc(
                            new EntityUnion(CodeDom.Field<string>(CodeDom.TypeRef(dn), "EntityName")),
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

            foreach (var entityRelation in m_entity.GetEntityRelations(false))
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


                staticProperty.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            relationDescType,
                            new CodeObjectCreateExpression(
                                typeof(EntityUnion),
                                WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(m_entity)
                            ),
                            new CodeObjectCreateExpression(
                                typeof(RelationDesc),
                                new CodeObjectCreateExpression(
                                    new CodeTypeReference(typeof(EntityUnion)),
                                    WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(entityRelation.Entity)
                                ),
                                WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(entityRelation.Property),
                                new CodePrimitiveExpression(entityRelation.Name ?? "default")
                            )
                        )
                    )
                );

                Members.Add(staticProperty);

                string cd = WXMLCodeDomGeneratorNameHelper.GetEntityClassName(entityRelation.Property.Entity, true) + ".Properties";
                string dn = WXMLCodeDomGeneratorNameHelper.GetEntityClassName(entityRelation.Entity, true) + ".Descriptor";

                Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static, 
                    typeof(RelationDescEx),
                    (EntityUnion hostEntity)=>"Get" + staticProperty.Name,
                    Emit.@return((EntityUnion hostEntity)=> 
                        new RelationDescEx(hostEntity, new RelationDesc(
                            new EntityUnion(CodeDom.Field<string>(CodeDom.TypeRef(dn), "EntityName")),
                            CodeDom.Field<string>(CodeDom.TypeRef(cd), entityRelation.Property.PropertyAlias),
                            entityRelation.Name
                        ))
                    )
                ));

                Members.Add(Define.Method(MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static,
                    typeof(RelationDescEx),
                    (EntityUnion hostEntity, EntityUnion joinEntity) => "Get" + staticProperty.Name,
                    Emit.@return((EntityUnion hostEntity, EntityUnion joinEntity) =>
                        new RelationDescEx(hostEntity, new RelationDesc(
                            joinEntity,
                            CodeDom.Field<string>(CodeDom.TypeRef(cd), entityRelation.Property.PropertyAlias),
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
                                    WXMLCodeDomGeneratorHelper.GetEntityClassReferenceExpression(m_entity),
                                    staticProperty.Name
                                ),
                                "Rel"
                            )
                        )
                    )
                );
                Members.Add(memberProperty);

                WormCodeDomGenerator.RaisePropertyCreated(null, this, memberProperty, null);
            }
        }

        protected virtual void OnPopulatePropertiesAccessors()
        {
            foreach (var propertyDescription in Entity.ActiveProperties)
            {
                if (propertyDescription.Group == null)
                    continue;
                CodePropertiesAccessorTypeDeclaration accessor;
                if (!m_propertiesAccessor.TryGetValue(propertyDescription.Group.Name, out accessor))
                    m_propertiesAccessor[propertyDescription.Group.Name] =
                        new CodePropertiesAccessorTypeDeclaration(Entity, propertyDescription.Group);

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

        public CodeEntityTypeDeclaration(EntityDescription entity, bool useType)
            : this(useType)
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
                    m_schema = new CodeSchemaDefTypeDeclaration(this);
                }
                return m_schema;
            }
        }

        public new string Name
        {
            get
            {
                if (Entity != null)
                    return WXMLCodeDomGeneratorNameHelper.GetEntityClassName(Entity, false);
                return null;
            }
        }

        public string FullName
        {
            get
            {
                return WXMLCodeDomGeneratorNameHelper.GetEntityClassName(Entity, true);
            }
        }

        public EntityDescription Entity
        {
            get { return m_entity; }
            set
            {
                m_entity = value;
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
                    BaseTypes.Add(m_entityInterface.TypeReference);
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
    }
}
