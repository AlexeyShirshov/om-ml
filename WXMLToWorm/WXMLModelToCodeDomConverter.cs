using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WXML.Model.Descriptors;
using System.Linq;
using LinqToCodedom.Generator;
using LinqToCodedom;
using LinqToCodedom.CodeDomPatterns;
using WXML.Model;
using WXMLToWorm.CodeDomExtensions;
using Worm.Entities;
using Worm.Entities.Meta;
using Worm.Query;
using Worm.Cache;
using Worm;
using WXML.CodeDom;
using WXML.CodeDom.CodeDomExtensions;
using Field2DbRelations=WXML.Model.Field2DbRelations;

namespace WXMLToWorm
{
    public class WormCodeDomGenerator : WXMLCodeDomGenerator
    {
        #region Events

        protected internal class EntityClassCreatedEventArgs : EventArgs
        {
            private readonly CodeEntityTypeDeclaration m_typeDeclaration;
            private readonly CodeNamespace m_namespace;

            public EntityClassCreatedEventArgs(CodeNamespace typeNamespace, CodeEntityTypeDeclaration typeDeclaration)
            {
                m_typeDeclaration = typeDeclaration;
                m_namespace = typeNamespace;
            }

            public CodeEntityTypeDeclaration TypeDeclaration
            {
                get { return m_typeDeclaration; }
            }

            public CodeNamespace Namespace
            {
                get { return m_namespace; }
            }
        }
        protected internal class EntityPropertyCreatedEventArgs : EventArgs
        {
            private readonly PropertyDefinition m_propertyDescription;
            private readonly CodeMemberField m_fieldMember;
            private readonly CodeMemberProperty m_propertyMember;
            private readonly CodeEntityTypeDeclaration m_entityTypeDeclaration;

            public EntityPropertyCreatedEventArgs(PropertyDefinition propertyDescription, CodeEntityTypeDeclaration entityTypeDeclaration, CodeMemberField fieldMember, CodeMemberProperty propertyMember)
            {
                m_entityTypeDeclaration = entityTypeDeclaration;
                m_propertyMember = propertyMember;
                m_fieldMember = fieldMember;
                m_propertyDescription = propertyDescription;
            }

            public PropertyDefinition PropertyDescription
            {
                get { return m_propertyDescription; }
            }

            public CodeMemberField FieldMember
            {
                get { return m_fieldMember; }
            }

            public CodeMemberProperty PropertyMember
            {
                get { return m_propertyMember; }
            }

            public CodeEntityTypeDeclaration EntityTypeDeclaration
            {
                get { return m_entityTypeDeclaration; }
            }
        }
        protected internal class EntityCtorCreatedEventArgs : EventArgs
        {
            private readonly CodeEntityTypeDeclaration m_entityType;
            private readonly CodeConstructor m_ctor;

            public EntityCtorCreatedEventArgs(CodeEntityTypeDeclaration entityType, CodeConstructor ctor)
            {
                m_entityType = entityType;
                m_ctor = ctor;
            }

            public CodeEntityTypeDeclaration EntityTypeDeclaration
            {
                get { return m_entityType; }
            }

            public CodeConstructor CtorDeclaration
            {
                get { return m_ctor; }
            }
        }

        protected event EventHandler<EntityClassCreatedEventArgs> EntityClassCreated;
        //{
        //    add
        //    {
        //        s_ctrl.EventDelegates.AddHandler(EntityGeneratorController.EntityClassCreatedKey, value);
        //    }
        //    remove
        //    {
        //        s_ctrl.EventDelegates.RemoveHandler(EntityGeneratorController.EntityClassCreatedKey, value);
        //    }
        //}

        protected event EventHandler<EntityPropertyCreatedEventArgs> PropertyCreated;
        //{
        //    add
        //    {
        //        s_ctrl.EventDelegates.AddHandler(EntityGeneratorController.PropertyCreatedKey, value);
        //    }
        //    remove
        //    {
        //        s_ctrl.EventDelegates.RemoveHandler(EntityGeneratorController.PropertyCreatedKey, value);
        //    }
        //}
        
        protected event EventHandler<EntityCtorCreatedEventArgs> EntityClassCtorCreated;
        //{
        //    add
        //    {
        //        s_ctrl.EventDelegates.AddHandler(EntityGeneratorController.EntityClassCtorCreatedKey, value);
        //    }
        //    remove
        //    {
        //        s_ctrl.EventDelegates.RemoveHandler(EntityGeneratorController.EntityClassCtorCreatedKey, value);
        //    }

        //}

        #endregion

        //protected internal class EntityGeneratorController : IDisposable
        //{
        //    public WormCodeDomGenerator Current { get; private set; }
        //    public EntityGeneratorController(WormCodeDomGenerator gen)
        //    {
        //        Current = gen;
        //        if (WormCodeDomGenerator.s_ctrl == null)
        //            WormCodeDomGenerator.s_ctrl = this;
        //    }

        //    public System.ComponentModel.EventHandlerList EventDelegates = new System.ComponentModel.EventHandlerList();

        //    public static readonly object EntityClassCreatedKey = new object();
        //    public static readonly object PropertyCreatedKey = new object();
        //    public static readonly object EntityClassCtorCreatedKey = new object();

        //    //public event EventHandler<EntityClassCreatedEventArgs> EntityClassCreated;
        //    //public event EventHandler<EntityPropertyCreatedEventArgs> PropertyCreated;
        //    //public event EventHandler<EntityCtorCreatedEventArgs> EntityClassCtorCreated;	

        //    #region IDisposable Members

        //    public void Dispose()
        //    {
        //        if (EventDelegates != null)
        //            EventDelegates.Dispose();
        //    }

        //    #endregion
        //}

        private readonly WXMLModel _model;

        public WormCodeDomGenerator(WXMLModel ormObjectsDefinition, WXMLCodeDomGeneratorSettings settings)
            : base(settings)
        {
            _model = ormObjectsDefinition;
        }

        public Dictionary<string, CodeCompileFileUnit> GetEntitiesCompliteUnits(CodeDomGenerator.Language language)
        {
            var result = new Dictionary<string, CodeCompileFileUnit>(_model.GetActiveEntities().Count());
            foreach (EntityDefinition entity in _model.GetActiveEntities())
            {
                foreach (var pair in GetEntityCompileUnits(entity.Identifier, language))
                {
                    string key = pair.Filename;
                    for (int i = 0; result.ContainsKey(key); i++)
                    {
                        key = pair.Filename + i;
                    }

                    result.Add(key, pair);
                }
            }
            return result;
        }

        public Dictionary<string, CodeCompileFileUnit> GetCompileUnits(CodeDomGenerator.Language language)
        {
            var r = GetEntitiesCompliteUnits(language);
            var ctx = GetLinqContextCompliteUnit(language);
            if (ctx != null)
                r.Add(ctx.Filename, ctx);
            return r;
        }

        public CodeCompileFileUnit GetFullSingleUnit(CodeDomGenerator.Language language)
        {
            CodeCompileFileUnit unit = new CodeCompileFileUnit();
            Dictionary<string, CodeNamespace> unitNamespaces = new Dictionary<string, CodeNamespace>();
            foreach (CodeCompileFileUnit u in GetEntitiesCompliteUnits(language).Values)
            {
                foreach (CodeNamespace n in u.Namespaces)
                {
                    CodeNamespace ns;
                    if (!unitNamespaces.TryGetValue(n.Name, out ns))
                    {
                        ns = new CodeNamespace(n.Name);
                        unit.Namespaces.Add(ns);
                        unitNamespaces.Add(n.Name, ns);
                    }

                    foreach (CodeTypeDeclaration c in n.Types)
                    {
                        ns.Types.Add(c);
                    }
                }
            }
            CodeCompileFileUnit linq = GetLinqContextCompliteUnit(language);
            if (linq != null)
                foreach (CodeNamespace n in linq.Namespaces)
                {
                    CodeNamespace ns;
                    if (!unitNamespaces.TryGetValue(n.Name, out ns))
                    {
                        ns = new CodeNamespace(n.Name);
                        unit.Namespaces.Add(ns);
                        unitNamespaces.Add(n.Name, ns);
                    }
                    foreach (CodeTypeDeclaration c in n.Types)
                    {
                        ns.Types.Add(c);
                    }
                }

            StringBuilder commentBuilder = new StringBuilder();
            foreach (string comment in _model.SystemComments)
            {
                commentBuilder.AppendLine(comment);
            }

            if (_model.UserComments.Count > 0)
            {
                commentBuilder.AppendLine();
                foreach (string comment in _model.UserComments)
                {
                    commentBuilder.AppendLine(comment);
                }
            }

            if (commentBuilder.Length > 0)
            {
                CodeNamespace com = new CodeNamespace();
                com.Comments.Insert(0,new CodeCommentStatement(commentBuilder.ToString(), false));
                unit.Namespaces.Insert(0, com);
            }
            return unit;
        }

        public CodeCompileFileUnit GetLinqContextCompliteUnit(CodeDomGenerator.Language language)
        {
            if (_model.LinqSettings == null || !_model.LinqSettings.Enable) return null;

            var ctx = new CodeLinqContextDeclaration(Settings, _model.LinqSettings);

            ctx.Entities.AddRange(_model.OwnEntities);

            var result = new CodeCompileFileUnit
            {
                Filename = !string.IsNullOrEmpty(_model.LinqSettings.FileName)
                    ? _model.LinqSettings.FileName
                    : ctx.Name
            };

            var ns = new CodeNamespace(_model.Namespace);
            ns.Types.Add(ctx);
            //result.Namespaces.Add(ns);

            CodeDomTreeProcessor.ProcessNS(result, language, new[]{ns});

            return result;

        }

        [Obsolete("Use GetEntityCompileUnits instead.")]
        public Dictionary<string, CodeCompileUnit> GetEntityDom(string entityId,
            WXMLCodeDomGeneratorSettings settings, CodeDomGenerator.Language language)
        {
            var units = GetEntityCompileUnits(entityId, language);
            var result = new Dictionary<string, CodeCompileUnit>();
            foreach (var unit in units)
            {
                result.Add(unit.Filename, unit);
            }
            return result;
        }

        public CodeEntityTypeDeclaration GetEntityDeclaration(EntityDefinition entity)
        {
            foreach (CodeCompileFileUnit fu in GetEntityCompileUnits(entity.Identifier, CodeDomGenerator.Language.CSharp))
            {
                foreach (CodeNamespace ns in fu.Namespaces)
                {
                    return ns.Types.OfType<CodeEntityTypeDeclaration>().SingleOrDefault(e => e.Name == new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity, false));
                }
            }

            return null;
        }

        public IList<CodeCompileFileUnit> GetEntityCompileUnits(string entityId, CodeDomGenerator.Language language)
        {
            //using (new EntityGeneratorController(this))
            {
                //using (new SettingsManager(settings, null))
                //{

                if (String.IsNullOrEmpty(entityId))
                    throw new ArgumentNullException("entityId");

                EntityDefinition entity = _model.GetEntity(entityId);

                if (entity == null)
                    throw new ArgumentException("entityId",
                                                string.Format("Entity with id '{0}' not found.", entityId));

                PropertyCreated += OnPropertyDocumentationRequiered;

                if (entity.MakeInterface || (entity.BaseEntity != null && entity.BaseEntity.MakeInterface))
                {
                    EntityClassCreated += OnMakeEntityInterfaceRequired;
                    PropertyCreated += OnPropertyCreatedFillEntityInterface;
                }

                if (!entity.EnableCommonEventRaise)
                {
                    EntityClassCtorCreated += OnEntityCtorCustomPropEventsImplementationRequired;
                    PropertyCreated += OnPropertyChangedImplementationRequired;
                }

                try
                {
                    List<CodeCompileFileUnit> result = new List<CodeCompileFileUnit>();

                    CodeTypeDeclaration fieldsClass = null;

                    #region определение класса сущности

                    WXMLCodeDomGeneratorNameHelper nameHelper = new WXMLCodeDomGeneratorNameHelper(Settings);

                    CodeCompileFileUnit entityUnit = new CodeCompileFileUnit
                    {
                        Filename = nameHelper.GetEntityFileName(entity)
                    };
                    result.Add(entityUnit);

                    // неймспейс
                    CodeNamespace nameSpace = new CodeNamespace(entity.Namespace);
                    entityUnit.Namespaces.Add(nameSpace);

                    // класс сущности
                    CodeEntityTypeDeclaration entityClass = new CodeEntityTypeDeclaration(Settings, entity, this);
                    nameSpace.Types.Add(entityClass);

                    // параметры класса
                    entityClass.IsClass = true;
                    var behaviour = entity.Behaviour;
                    entityClass.IsPartial = behaviour == EntityBehaviuor.PartialObjects ||
                                            behaviour == EntityBehaviuor.ForcePartial;
                    entityClass.Attributes = MemberAttributes.Public;
                    entityClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

                    if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.SchemaOnly)
                    {
                        // базовый класс
                        if (entity.BaseEntity == null)
                        {
                            if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                            {
                                CodeTypeReference entityType;
                                if (_model.EntityBaseType == null)
                                {
                                    entityType = new CodeTypeReference(entity.GetPkProperties().Count() == 1 ?
                                        typeof(KeyEntity) :
                                        entity.GetPkProperties().Count() > 0 ?
                                            typeof(CachedLazyLoad) :
                                            typeof(Entity));
                                }
                                else
                                {
                                    entityType =
                                        new CodeTypeReference(_model.EntityBaseType.IsEntityType
                                                                  ? nameHelper.GetEntityClassName(
                                                                        _model.EntityBaseType.Entity,
                                                                        true)
                                                                  : _model.EntityBaseType.GetTypeName(Settings));
                                    //entityType.TypeArguments.Add(
                                    //    new CodeTypeReference(nameHelper.GetEntityClassName(entity)));
                                }

                                entityClass.BaseTypes.Add(entityType);
                                entityClass.BaseTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
                            }
                        }
                        else
                            entityClass.BaseTypes.Add(
                                new CodeTypeReference(nameHelper.GetEntityClassName(entity.BaseEntity, true)));

                        RaiseEntityClassCreated(nameSpace, entityClass);
                    }

                    #region определение класса Descriptor
                    var descriptorClass = new CodeTypeDeclaration
                    {
                        Name = "Descriptor",
                        Attributes = MemberAttributes.Public,
                        TypeAttributes = (TypeAttributes.Class | TypeAttributes.NestedPublic),
                        IsPartial = true
                    };
                    var descConstr = new CodeConstructor
                    {
                        Attributes = MemberAttributes.Family
                    };
                    descriptorClass.Members.Add(descConstr);

                    SetMemberDescription(descriptorClass, "Описатель сущности.");

                    var entityNameField = new CodeMemberField
                    {
                        Type = new CodeTypeReference(typeof(string)),
                        Name = "EntityName",
                        InitExpression = new CodePrimitiveExpression(entity.FamilyName),
                        Attributes = (MemberAttributes.Public | MemberAttributes.Const)
                    };

                    descriptorClass.Members.Add(entityNameField);

                    SetMemberDescription(entityNameField, "Имя сущности в объектной модели.");

                    entityClass.Members.Add(descriptorClass);

                    if (entity.BaseEntity != null)
                        descriptorClass.Attributes |= MemberAttributes.New;

                    #endregion

                    #region определение класса Properties
                    CodeTypeDeclaration propertiesClass = new CodeTypeDeclaration("Properties")
                    {
                        Attributes = MemberAttributes.Public,
                        TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic,
                        IsPartial = true
                    };
                    propertiesClass.Members.Add(new CodeConstructor {Attributes = MemberAttributes.Family});
                    SetMemberDescription(propertiesClass, "Алиасы свойств сущностей испльзуемые в объектной модели.");

                    if (entity.BaseEntity != null)
                        propertiesClass.Attributes |= MemberAttributes.New;

                    entityClass.Members.Add(propertiesClass);

                    #endregion определение класса Properties

                    CodeTypeDeclaration propertyAliasClass = null;
                    CodeTypeDeclaration instancedPropertyAliasClass = null;

                    if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) == GenerateModeEnum.Full)
                    {
                        #region PropertyAlias class
                        propertyAliasClass = new CodeTypeDeclaration
                        {
                            Name = entity.Name + "Alias",
                            Attributes = MemberAttributes.Family,
                        };

                        var propertyAliasClassCtor = new CodeConstructor { Attributes = MemberAttributes.Public };

                        if (Settings.UseTypeInProps)
                            propertyAliasClassCtor.BaseConstructorArgs.Add(WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(Settings, entity, false));
                        else
                            propertyAliasClassCtor.BaseConstructorArgs.Add(WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(Settings, entity, false));

                        propertyAliasClass.Members.Add(propertyAliasClassCtor);
                        propertyAliasClass.BaseTypes.Add(new CodeTypeReference(typeof(QueryAlias)));


                        instancedPropertyAliasClass = new CodeTypeDeclaration
                        {
                            Name = entity.Name + "Properties",
                            Attributes = MemberAttributes.Family,
                        };

                        var instancedPropertyAliasClassCtor = new CodeConstructor { Attributes = MemberAttributes.Public };

                        instancedPropertyAliasClassCtor.Parameters.Add(
                            new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(QueryAlias)), "objectAlias"));

                        instancedPropertyAliasClassCtor.Statements.Add(
                            new CodeAssignStatement(
                                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                 nameHelper.GetPrivateMemberName("objectAlias")),
                                new CodeArgumentReferenceExpression("objectAlias")));

                        instancedPropertyAliasClass.Members.Add(instancedPropertyAliasClassCtor);

                        var instancedPropertyAliasfield = new CodeMemberField(new CodeTypeReference(typeof(QueryAlias)),
                                                        nameHelper.GetPrivateMemberName("objectAlias"));
                        instancedPropertyAliasClass.Members.Add(instancedPropertyAliasfield);

                        instancedPropertyAliasClass.Members.Add(
                            //Delegates.CodeMemberOperatorOverride(
                            //    CodeDomPatterns.OperatorType.Implicit,
                            //    new CodeTypeReference(typeof(EntityAlias)),
                            //    new[]{new CodeParameterDeclarationExpression(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity) + "." +
                            //                                          instancedPropertyAliasClass.Name), "entityAlias")},
                            Define.Operator(new CodeTypeReference(typeof(QueryAlias)),
                                (DynType entityAlias) => CodeDom.TypedSeq(OperatorType.Implicit,
                                    entityAlias.SetType(nameHelper.GetEntityClassName(entity, false) + "." + instancedPropertyAliasClass.Name)),
                                new CodeMethodReturnStatement(
                                    new CodeFieldReferenceExpression(
                                        new CodeArgumentReferenceExpression("entityAlias"),
                                        instancedPropertyAliasfield.Name
                                    )
                                )
                            )
                        );

                        if (entity.BaseEntity != null && entity.Name == entity.BaseEntity.Name)
                        {
                            propertyAliasClass.Attributes |= MemberAttributes.New;
                            instancedPropertyAliasClass.Attributes |= MemberAttributes.New;
                        }

                        entityClass.Members.Add(propertyAliasClass);
                        entityClass.Members.Add(instancedPropertyAliasClass);

                        #endregion

                        #region ObjectAlias methods
                        var createMethod = new CodeMemberMethod
                        {
                            Name = "CreateAlias",
                            ReturnType =
                             new CodeTypeReference(nameHelper.GetEntityClassName(entity, false) + "." +
                                                   propertyAliasClass.Name),
                            Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                        };
                        if (entity.BaseEntity != null)
                            createMethod.Attributes |= MemberAttributes.New;
                        createMethod.Statements.Add(
                            new CodeMethodReturnStatement(
                                new CodeObjectCreateExpression(
                                    new CodeTypeReference(nameHelper.GetEntityClassName(entity, true) + "." +
                                                          propertyAliasClass.Name))));
                        entityClass.Members.Add(createMethod);

                        var getMethod = new CodeMemberMethod
                        {
                            Name = "WrapAlias",
                            ReturnType =
                                new CodeTypeReference(nameHelper.GetEntityClassName(entity, false) + "." +
                                                      instancedPropertyAliasClass.Name),
                            Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                        };

                        if (entity.BaseEntity != null)
                            getMethod.Attributes |= MemberAttributes.New;
                        getMethod.Parameters.Add(new CodeParameterDeclarationExpression { Name = "objectAlias", Type = new CodeTypeReference(typeof(QueryAlias)) });

                        getMethod.Statements.Add(
                            new CodeMethodReturnStatement(
                                new CodeObjectCreateExpression(
                                    new CodeTypeReference(nameHelper.GetEntityClassName(entity, true) + "." +
                                                          instancedPropertyAliasClass.Name), new CodeArgumentReferenceExpression("objectAlias"))));
                        entityClass.Members.Add(getMethod);
                        #endregion
                    }

                    foreach (PropertyDefinition propertyDesc in
                        from k in entity.GetActiveProperties()
                        select k)
                    {
                        var propertyNameField = Define.Field(typeof(string),
                            MemberAttributes.Public | MemberAttributes.Const,
                            propertyDesc.PropertyAlias, () => propertyDesc.PropertyAlias);

                        propertiesClass.Members.Add(propertyNameField);

                        if (!string.IsNullOrEmpty(propertyDesc.Description))
                            SetMemberDescription(propertyNameField, propertyDesc.Description);

                        var propertyAliasProperty = Define.GetProperty(typeof(ObjectProperty),
                            MemberAttributes.Public | MemberAttributes.Final,
                            propertyDesc.PropertyAlias,
                            Emit.@return(() => CodeDom.@new(typeof(ObjectProperty), CodeDom.@this,
                                WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc, false))
                            )
                            //Emit.@return(() => new ObjectProperty(CodeDom.@this.cast<QueryAlias>(), 
                            //    WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc)
                            //))
                        );

                        if (propertyAliasClass != null)
                            propertyAliasClass.Members.Add(propertyAliasProperty);

                        if (!string.IsNullOrEmpty(propertyDesc.Description))
                            SetMemberDescription(propertyAliasProperty, propertyDesc.Description);

                        string privateMemberName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName("objectAlias");

                        var instancedPropertyAliasProperty = Define.GetProperty(typeof(ObjectProperty),
                            MemberAttributes.Public | MemberAttributes.Final,
                            propertyDesc.PropertyAlias,
                            Emit.@return(() => CodeDom.@new(typeof(ObjectProperty),
                                CodeDom.Field(CodeDom.@this, privateMemberName),
                                WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc, false)
                            ))
                        );

                        if (instancedPropertyAliasClass != null)
                            instancedPropertyAliasClass.Members.Add(instancedPropertyAliasProperty);

                        if (!string.IsNullOrEmpty(propertyDesc.Description))
                            SetMemberDescription(instancedPropertyAliasProperty, propertyDesc.Description);
                    }

                    // дескрипшн
                    SetMemberDescription(entityClass, entity.Description);

                    #endregion определение класса сущности

                    if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.SchemaOnly)
                    {
                        #region определение класса Fields
                        if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) == GenerateModeEnum.Full)
                        {
                            fieldsClass = new CodeTypeDeclaration("props")
                            {
                                Attributes = MemberAttributes.Public,
                                TypeAttributes = (TypeAttributes.Class | TypeAttributes.NestedPublic),
                                IsPartial = true
                            };
                            var propctr = new CodeConstructor { Attributes = MemberAttributes.Family };
                            fieldsClass.Members.Add(propctr);

                            SetMemberDescription(fieldsClass, "Ссылки на поля сущностей.");

                            entityClass.Members.Add(fieldsClass);

                            if (entity.BaseEntity != null)
                                fieldsClass.Attributes |= MemberAttributes.New;
                        }

                        #endregion определение класса Fields

                        #region конструкторы

                        // конструктор по умолчанию
                        CodeConstructor ctr = new CodeConstructor { Attributes = MemberAttributes.Public };
                        entityClass.Members.Add(ctr);

                        RaiseEntityCtorCreated(entityClass, ctr);

                        //if(
                        if (entity.GetPkProperties().Count() == 1 &&
                            (Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                        {
                            ScalarPropertyDefinition pkProperty = entity.GetPkProperties().Single();
                            // параметризированный конструктор
                            ctr = new CodeConstructor {Attributes = MemberAttributes.Public};
                            // параметры конструктора
                            ctr.Parameters.Add(new CodeParameterDeclarationExpression(pkProperty.PropertyType.ToCodeType(Settings), "id"));
                            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(CacheBase), "cache"));
                            ctr.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ObjectMappingEngine),
                                                                                      "schema"));

                            ctr.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "Init",
                                                                              new CodeArgumentReferenceExpression("id"),
                                                                              new CodeArgumentReferenceExpression("cache"),
                                                                              new CodeArgumentReferenceExpression("schema")));

                            entityClass.Members.Add(ctr);
                            RaiseEntityCtorCreated(entityClass, ctr);
                        }

                        #endregion конструкторы

                        #region метод OrmBase.CopyBody(CopyBody(...)

                        if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                        {
                            CopyPropertiesMethodGeneration(entityClass);
                        }

                        #endregion метод OrmBase.CopyBody(CopyBody(OrmBase from, OrmBase to)

                        #region void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

                        CodeMemberMethod setvalueMethod = null;
                        if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                        {
                            setvalueMethod = CreateSetValueMethod(entityClass);
                        }

                        #endregion void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

                        #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                        CodeMemberMethod getvalueMethod = null;
                        if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                        {
                            getvalueMethod = CreateGetValueMethod(entityClass);
                        }

                        #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                        #region проперти

                        CreateProperties(entity, entityClass, setvalueMethod, getvalueMethod, fieldsClass);

                        #endregion проперти

                        #region CachedEntity methods

                        //CodeMemberMethod createobjectMethod = null;

                        if (entity.GetPkProperties().Count() > 0 &&
                            (Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                        {
                            if (entity.GetPkProperties().Count() == 1)
                            {
                                if (entity.BaseEntity == null)
                                {
                                    OverrideIdentifierProperty(entityClass);
                                    CreateSetPKMethod(entityClass, false);
                                    CreateGetPKValuesMethod(entityClass);
                                }
                                else
                                {
                                    UpdateGetPKValuesMethod(entityClass);
                                    UpdateSetPKMethod(entityClass, false);
                                }
                            }
                            else
                            {
                                if (entity.BaseEntity == null)
                                {
                                    CreateGetKeyMethodCompositePK(entityClass);
                                    CreateGetPKValuesMethod(entityClass);
                                    CreateSetPKMethod(entityClass, true);
                                }
                                else
                                {
                                    UpdateGetKeyMethodCompositePK(entityClass);
                                    UpdateGetPKValuesMethod(entityClass);
                                    UpdateSetPKMethod(entityClass, true);
                                }

                                OverrideEqualsMethodCompositePK(entityClass);
                            }
                        }
                        #endregion

                        #region void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

                        if (entity.BaseEntity != null)
                            setvalueMethod.Statements.Add(
                                new CodeMethodInvokeExpression(
                                    new CodeMethodReferenceExpression(
                                        new CodeBaseReferenceExpression(),
                                        "SetValueOptimized"
                                        ),
                                    new CodeArgumentReferenceExpression("propertyAlias"),
                                    new CodeArgumentReferenceExpression("schema"),
                                    new CodeArgumentReferenceExpression("value")
                                    )
                                );

                        #endregion void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

                        #region m2m relation methods

                        //if (!Settings.RemoveOldM2M && entity.GetPkProperties().Count() == 1)
                        //    CreateM2MMethodsSet(entity, entityClass);

                        #endregion

                    }

                    #region custom attribute EntityAttribute
                    if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                    {
                        entityClass.CustomAttributes.AddRange(new[]{
                            new CodeAttributeDeclaration(
	                            new CodeTypeReference(typeof (EntityAttribute)),
	                            new CodeAttributeArgument(
	                                new CodeTypeOfExpression(
	                                    new CodeTypeReference(
	                                        nameHelper.GetEntitySchemaDefClassQualifiedName(entity, false)
                                        )
                                    )
                                ),
	                            new CodeAttributeArgument(
	                                new CodePrimitiveExpression(entity.Model.SchemaVersion)
	                            ),
	                            new CodeAttributeArgument(
	                                "EntityName",
	                                WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(Settings,entity, false)
	                            )
	                        ),
                            new CodeAttributeDeclaration(
                                new CodeTypeReference(typeof(SerializableAttribute))
                            )
   	                    });
                    }
                    #endregion custom attribute EntityAttribute

                    foreach (CodeCompileFileUnit compileUnit in result)
                    {
                        if ((Settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsExplicit) ==
                            LanguageSpecificHacks.AddOptionsExplicit)
                            compileUnit.UserData.Add("RequireVariableDeclaration",
                                                     (Settings.LanguageSpecificHacks &
                                                      LanguageSpecificHacks.OptionsExplicitOn) ==
                                                     LanguageSpecificHacks.OptionsExplicitOn);
                        if ((Settings.LanguageSpecificHacks & LanguageSpecificHacks.AddOptionsStrict) ==
                            LanguageSpecificHacks.AddOptionsStrict)
                            compileUnit.UserData.Add("AllowLateBound",
                                                     (Settings.LanguageSpecificHacks &
                                                      LanguageSpecificHacks.OptionsStrictOn) !=
                                                     LanguageSpecificHacks.OptionsStrictOn);

                        if (compileUnit.Namespaces.Count > 0)
                        {
                            StringBuilder commentBuilder = new StringBuilder();
                            foreach (string comment in _model.SystemComments)
                            {
                                commentBuilder.AppendLine(comment);
                            }

                            if (_model.UserComments.Count > 0)
                            {
                                commentBuilder.AppendLine();
                                foreach (string comment in _model.UserComments)
                                {
                                    commentBuilder.AppendLine(comment);
                                }
                            }
                            compileUnit.Namespaces[0].Comments.Insert(0,
                                new CodeCommentStatement(commentBuilder.ToString(), false));
                        }

                        foreach (CodeNamespace ns in compileUnit.Namespaces)
                        {
                            foreach (CodeTypeDeclaration type in ns.Types)
                            {
                                WXMLCodeDomGeneratorHelper.SetRegions(type);
                            }
                        }
                    }

                    List<CodeCompileFileUnit> res = new List<CodeCompileFileUnit>();
                    foreach (CodeCompileFileUnit compileUnit in result)
                    {
                        CodeCompileFileUnit newUnit = new CodeCompileFileUnit()
                        {
                            Filename = compileUnit.Filename
                        };

                        CodeNamespace[] namespaces = new CodeNamespace[compileUnit.Namespaces.Count];
                        compileUnit.Namespaces.CopyTo(namespaces, 0);
                        CodeDomTreeProcessor.ProcessNS(newUnit, language, namespaces);
                        res.Add(newUnit);
                    }
                    return result;
                }
                finally
                {
                    PropertyCreated -= OnPropertyDocumentationRequiered;

                    if (entity.MakeInterface || (entity.BaseEntity != null && entity.BaseEntity.MakeInterface))
                    {
                        EntityClassCreated -= OnMakeEntityInterfaceRequired;
                        PropertyCreated -= OnPropertyCreatedFillEntityInterface;
                    }

                    if (!entity.EnableCommonEventRaise)
                    {
                        EntityClassCtorCreated -= OnEntityCtorCustomPropEventsImplementationRequired;
                        PropertyCreated -= OnPropertyChangedImplementationRequired;
                    }
                }
            }
        }

        private void CopyPropertiesMethodGeneration(CodeEntityTypeDeclaration entityClass)
        {
            EntityDefinition entity = entityClass.Entity;

            EntityDefinition superbaseEntity;
            for (superbaseEntity = entity;
                 superbaseEntity.BaseEntity != null;
                 superbaseEntity = superbaseEntity.BaseEntity)
            {

            }

            bool isInitialImplemantation = entity == superbaseEntity;

            CodeMemberMethod copyMethod = new CodeMemberMethod();
            entityClass.Members.Add(copyMethod);
            copyMethod.Name = "CopyProperties";
            copyMethod.ReturnType = null;
            // модификаторы доступа
            copyMethod.Attributes = MemberAttributes.Family | MemberAttributes.Override;
            copyMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(typeof(_IEntity),
                                                       "from"));
            copyMethod.Parameters.Add(
                new CodeParameterDeclarationExpression(typeof(_IEntity),
                                                       "to"));
            copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(OrmManager), "mgr"));
            copyMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEntitySchema), "oschema"));

            if (!isInitialImplemantation)
                copyMethod.Statements.Add(
                    new CodeMethodInvokeExpression(
                        new CodeBaseReferenceExpression(),
                        "CopyProperties",
                        new CodeArgumentReferenceExpression("from"),
                        new CodeArgumentReferenceExpression("to"),
                        new CodeArgumentReferenceExpression("mgr"),
                        new CodeArgumentReferenceExpression("oschema")
                        )
                    );

            PropertyCreated += new ssss() { copyMethod = copyMethod, entity = entity, Settings = Settings }.jkjk;
        }

        class ssss
        {
            public CodeMemberMethod copyMethod;
            public EntityDefinition entity;
            public WXMLCodeDomGeneratorSettings Settings;

            public void jkjk(object sender, EntityPropertyCreatedEventArgs e)
            {
                if (e.FieldMember == null) return;
                if (e.EntityTypeDeclaration.Entity != entity) return;

                string fieldName = e.FieldMember.Name;

                CodeTypeReference entityType =
                    new CodeTypeReference(
                        new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity, true));

                CodeExpression leftTargetExpression =
                    new CodeArgumentReferenceExpression("to");

                CodeExpression rightTargetExpression =
                    new CodeArgumentReferenceExpression("from");

                leftTargetExpression = new CodeCastExpression(entityType,
                                                              leftTargetExpression);
                rightTargetExpression = new CodeCastExpression(entityType,
                                                               rightTargetExpression);

                copyMethod.Statements.Add(
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(leftTargetExpression,
                                                         fieldName),
                        new CodeFieldReferenceExpression(rightTargetExpression,
                                                         fieldName))
                    );
                //#endregion // реализация метода Copy
            }
        }

        private void OverrideEqualsMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            CodeMemberMethod method = new CodeMemberMethod
            {
                Name = "Equals",
                Attributes = MemberAttributes.Override,
            };

            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "obj"));

            CodeExpression exp = null;

            foreach (var pk in entityClass.Entity.GetPkProperties())
            {
                var tExp = new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                            new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pk.Name)), "Equals",
                                                          new CodeFieldReferenceExpression(new CodeArgumentReferenceExpression("obj"),
                                                            new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pk.Name)));
                if (exp == null)
                    exp = tExp;
                else
                    exp = new CodeBinaryOperatorExpression(exp, CodeBinaryOperatorType.BooleanAnd, tExp);

            }
            if (entityClass.Entity.BaseEntity != null)
                exp = new CodeBinaryOperatorExpression(exp, CodeBinaryOperatorType.BooleanAnd, new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "Equals", new CodeArgumentReferenceExpression("obj")));
            method.Statements.Add(new CodeMethodReturnStatement(exp));


        }

        private void UpdateSetPKMethod(CodeEntityTypeDeclaration entityClass, bool composite)
        {
            EntityDefinition entity = entityClass.Entity;
            if (entity.OwnProperties.Where(item => item.HasAttribute(Field2DbRelations.PK)).Count() == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod
            {
                Name = "SetPK",
                // модификаторы доступа
                Attributes = MemberAttributes.Family | MemberAttributes.Override
            };

            entityClass.Members.Add(meth);

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(new CodeTypeReference(typeof(PKDesc)), 1), "pks")
            );

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(ObjectMappingEngine)), "mpe")
            );

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), 
                meth.Name,
                meth.Parameters.OfType<CodeParameterDeclarationExpression>()
                    .Select(item => new CodeArgumentReferenceExpression(item.Name))
                    .ToArray()
            ));

            if (composite)
            {
                meth.Statements.Add(
                    //Delegates.CodePatternForeachStatement(
                    //    new CodeTypeReference(typeof(PKDesc)), "pk",
                    //    new CodeArgumentReferenceExpression("pks"),
                    Emit.@foreach("pk", () => CodeDom.VarRef<PKDesc[]>("pks"),
                        entity.OwnProperties.Where(item=>item.HasAttribute(Field2DbRelations.PK) && !item.Disabled)
                        .Select(pd_ =>
                            {
                                //var typeReference = new CodeTypeReference(pd_.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pd_.PropertyType.Entity, true) : pd_.PropertyType.TypeName);
                                CodeTypeReference typeReference = pd_.PropertyType.ToCodeType(Settings);
                                return new CodeConditionStatement(
                                    new CodeBinaryOperatorExpression(
                                        new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("pk"),
                                            "PropertyAlias"),
                                        CodeBinaryOperatorType.ValueEquality,
                                        new CodePrimitiveExpression(pd_.PropertyAlias)),
                                    new CodeAssignStatement(
                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pd_.Name)),
                                        new CodeCastExpression(
                                            typeReference,
                                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                           new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("pk"), "Value"),
                                           new CodeTypeOfExpression(typeReference))
                                       )
                                    )

                                );
                            }
                         ).ToArray()
                    )
                );
            }
            else
            {
                ScalarPropertyDefinition pkProperty = entity.GetPkProperties().Single();
                meth.Statements.Add(
                    new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                            new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pkProperty.Name)),
                        new CodeCastExpression(pkProperty.PropertyType.ToCodeType(Settings),
                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                new CodeFieldReferenceExpression(
                                    new CodeIndexerExpression(
                                        new CodeArgumentReferenceExpression("pks"), new CodePrimitiveExpression(0)),
                                        "Value"),
                                new CodeTypeOfExpression(pkProperty.PropertyType.ToCodeType(Settings))
                            )
                        )
                )
                );
            }
        }

        private void UpdateGetPKValuesMethod(CodeEntityTypeDeclaration entityClass)
        {
            EntityDefinition entity = entityClass.Entity;
            if (entity.OwnProperties.Where(item=>item.HasAttribute(Field2DbRelations.PK)).Count() == 0)
                return;

            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));
            CodeMemberMethod meth = new CodeMemberMethod
            {
                Name = "GetPKValues",
                ReturnType = new CodeTypeReference(tr, 1),
                Attributes = MemberAttributes.Public | MemberAttributes.Override
            };
            // тип возвращаемого значения

            // модификаторы доступа

            entityClass.Members.Add(meth);

            meth.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(tr, 1), "basePks", new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name)));
            meth.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(tr, 1),
                    "result",
                    new CodeArrayCreateExpression(
                        new CodeTypeReference(tr, 1),
                        new CodeBinaryOperatorExpression(
                            new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length"),
                            CodeBinaryOperatorType.Add,
                            new CodePrimitiveExpression(entity.GetPkProperties().Count())
                        )

                    )
                )
            );

            //int[] v = new int[10];
            //int[] f = new int[] {1, 2};
            //int[] s = new int[] {1, 2, 3};
            //Array.Copy(f, v, f.Length);
            //Array.Copy(s, 0, v, f.Length, s.Length);

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Array)), "Copy", new CodeVariableReferenceExpression("basePks"), new CodeVariableReferenceExpression("result"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length")));
            meth.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(tr, 1),
                    "newPks",
                    new CodeArrayCreateExpression(
                        new CodeTypeReference(tr, 1),
                        entity.OwnProperties.Where(item => item.HasAttribute(Field2DbRelations.PK) && !item.Disabled)
                        .Select(pd_ => new CodeObjectCreateExpression(tr, new CodePrimitiveExpression(pd_.PropertyAlias),
                              new CodeFieldReferenceExpression(
                                  new CodeThisReferenceExpression(),
                                  new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName
                                      (pd_.Name)
                              )
                        )).ToArray()
                    )
                )
            );
            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Array)), "Copy", new CodeVariableReferenceExpression("basePks"), new CodeVariableReferenceExpression("result"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length")));

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Array)), "Copy", new CodeVariableReferenceExpression("newPks"), new CodePrimitiveExpression(0), new CodeVariableReferenceExpression("result"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("basePks"), "Length"), new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("newPks"), "Length")));

            meth.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("result")));
        }

        private void UpdateGetKeyMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDefinition entity = entityClass.Entity;

            if (entity.OwnProperties.Where(item => item.HasAttribute(Field2DbRelations.PK)).Count() == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod
            {
                Name = "GetCacheKey",
                ReturnType = new CodeTypeReference(typeof (Int32)),
                Attributes = MemberAttributes.Family | MemberAttributes.Override
            };
            // тип возвращаемого значения
            // модификаторы доступа

            entityClass.Members.Add(meth);

            CodeExpression lf = new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name);

            foreach (ScalarPropertyDefinition pd in entity.OwnProperties
                .Where(item=>item.HasAttribute(Field2DbRelations.PK)))
            {
                string fn = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pd.Name);

                CodeExpression exp = new CodeMethodInvokeExpression(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fn),
                    "GetHashCode", new CodeExpression[0]);

                lf = new CodeXorExpression(lf, exp);
            }
            meth.Statements.Add(new CodeMethodReturnStatement(lf));
        }

        private void OverrideIdentifierProperty(CodeEntityTypeDeclaration entityClass)
        {
            var property = new CodeMemberProperty
            {
                Name = "Identifier",
                Type = new CodeTypeReference(typeof(object)),
                HasGet = true,
                HasSet = true,
                Attributes = MemberAttributes.Public | MemberAttributes.Override
            };
            ScalarPropertyDefinition pkProperty = entityClass.Entity.GetPkProperties().Single();
            property.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                           new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pkProperty.Name))));
            //Convert.ChangeType(object, type);
            //var typeReference = new CodeTypeReference(pkProperty.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pkProperty.PropertyType.Entity, true) : pkProperty.PropertyType.TypeName);
            CodeTypeReference typeReference = pkProperty.PropertyType.ToCodeType(Settings);
            property.SetStatements.Add(
                new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                           new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pkProperty.Name)),
                        new CodeCastExpression(typeReference,
                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                new CodePropertySetValueReferenceExpression(),
                                new CodeTypeOfExpression(typeReference)
                            )
                        )
                )
            );
            entityClass.Members.Add(property);
        }

        private void CreateSetPKMethod(CodeEntityTypeDeclaration entityClass, bool composite)
        {
            EntityDefinition entity = entityClass.Entity;
            CodeMemberMethod meth = new CodeMemberMethod
            {
                Name = "SetPK",
                // модификаторы доступа
                Attributes = MemberAttributes.Family | MemberAttributes.Override
            };

            entityClass.Members.Add(meth);

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(new CodeTypeReference(typeof(PKDesc)), 1), "pks")
            );
            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(ObjectMappingEngine)), "mpe")
            );

            if (composite)
            {
                meth.Statements.Add(
                    //Delegates.CodePatternForeachStatement(
                    //    new CodeTypeReference(typeof(PKDesc)), "pk",
                    //    new CodeArgumentReferenceExpression("pks"),
                    Emit.@foreach("pk", () => CodeDom.VarRef<PKDesc[]>("pks"),
                        entity.GetPkProperties().
                        Select(pd_ =>
                        {
                            //var typeReference = new CodeTypeReference(pd_.PropertyType.IsEntityType ? OrmCodeGenNameHelper.GetEntityClassName(pd_.PropertyType.Entity, true) : pd_.PropertyType.TypeName);
                            CodeTypeReference typeReference = pd_.PropertyType.ToCodeType(Settings);
                            return new CodeConditionStatement(
                                new CodeBinaryOperatorExpression(
                                    new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("pk"),
                                                                     "PropertyAlias"),
                                    CodeBinaryOperatorType.ValueEquality,
                                    new CodePrimitiveExpression(pd_.PropertyAlias)),
                                new CodeAssignStatement(
                                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                     new WXMLCodeDomGeneratorNameHelper(Settings)
                                                                         .GetPrivateMemberName(pd_.Name)),
                                    new CodeCastExpression(
                                        typeReference,
                                        new CodeMethodInvokeExpression(
                                            new CodeTypeReferenceExpression(typeof (Convert)), "ChangeType",
                                            new CodePropertyReferenceExpression(
                                                new CodeVariableReferenceExpression("pk"), "Value"),
                                            new CodeTypeOfExpression(typeReference))
                                        )
                                    )
                                );
                        }
                            ).ToArray()
                    )
                );
            }
            else
            {
                ScalarPropertyDefinition pkProperty = entity.GetPkProperties().Single();
                meth.Statements.Add(
                new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                            new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pkProperty.Name)),
                        new CodeCastExpression(pkProperty.PropertyType.ToCodeType(Settings),
                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(Convert)), "ChangeType",
                                new CodeFieldReferenceExpression(
                                    new CodeIndexerExpression(
                                        new CodeArgumentReferenceExpression("pks"), new CodePrimitiveExpression(0)),
                                        "Value"),
                                new CodeTypeOfExpression(pkProperty.PropertyType.ToCodeType(Settings))
                            )
                        )
                )
            );
            }
        }

        private void CreateGetPKValuesMethod(CodeEntityTypeDeclaration entityClass)
        {
            EntityDefinition entity = entityClass.Entity;
            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));

            CodeMemberMethod meth = new CodeMemberMethod
            {
                Name = "GetPKValues",
                // тип возвращаемого значения
                ReturnType = new CodeTypeReference(tr, 1),
                // модификаторы доступа
                Attributes = MemberAttributes.Public | MemberAttributes.Override
            };

            entityClass.Members.Add(meth);

            meth.Statements.Add(
                new CodeMethodReturnStatement(new CodeArrayCreateExpression(meth.ReturnType,
                    entity.OwnProperties
                    .Where(pd_ => pd_.HasAttribute(Field2DbRelations.PK))
                    .Cast<ScalarPropertyDefinition>()
                    .Select(pd_ => new CodeObjectCreateExpression(tr, new CodePrimitiveExpression(pd_.PropertyAlias),
                          new CodeFieldReferenceExpression(
                              new CodeThisReferenceExpression(),
                              new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(
                                  pd_.Name)))
                    ).ToArray()
                ))
            );
        }

        private void CreateGetKeyMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDefinition entity = entityClass.Entity;

            CodeMemberMethod meth = new CodeMemberMethod
            {
                Name = "GetCacheKey",
                // тип возвращаемого значения
                ReturnType = new CodeTypeReference(typeof(Int32)),
                // модификаторы доступа
                Attributes = MemberAttributes.Family | MemberAttributes.Override
            };

            entityClass.Members.Add(meth);

            CodeExpression lf = null;

            foreach (ScalarPropertyDefinition pd in entity.OwnProperties
                .Where(item => item.HasAttribute(WXML.Model.Field2DbRelations.PK)))
            {
                string fn = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pd.Name);

                CodeExpression exp = new CodeMethodInvokeExpression(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fn),
                    "GetHashCode", new CodeExpression[0]);

                lf = lf == null ? exp : new CodeXorExpression(lf, exp);
            }
            meth.Statements.Add(new CodeMethodReturnStatement(lf));
        }

        void OnPropertyChangedImplementationRequired(object sender, EntityPropertyCreatedEventArgs e)
        {
            PropertyDefinition propDesc = e.PropertyDescription;
            if (propDesc == null)
                return;

            CodeMemberProperty property = e.PropertyMember;
            CodeMemberField field = e.FieldMember;
            //CodeEntityTypeDeclaration entityClass = e.

            if (!propDesc.EnablePropertyChanged || propDesc.FromBase || !property.HasSet)
                return;

            CodeUsingStatement usingStatement = null;

            foreach (CodeStatement statement in property.SetStatements)
            {
                usingStatement = statement as CodeUsingStatement;
            }
            if (usingStatement != null)
            {
                List<CodeStatement> statements = new List<CodeStatement>(usingStatement.Statements);
                statements.InsertRange(0,
                   new CodeStatement[]
                   	{
                   		new CodeVariableDeclarationStatement(
                   			typeof (bool),
                   			"notChanged",
                   			new CodeBinaryOperatorExpression(
                   				new CodeFieldReferenceExpression(
                   					new CodeThisReferenceExpression(),
                   					field.Name
                   					),
                   				propDesc.PropertyType.IsValueType
                   					? CodeBinaryOperatorType.ValueEquality
                   					: CodeBinaryOperatorType.IdentityEquality,
                   				new CodePropertySetValueReferenceExpression()
                   				)
                   			),
                   		new CodeVariableDeclarationStatement(
                   			field.Type,
                   			"oldValue",
                   			new CodeFieldReferenceExpression(
                   				new CodeThisReferenceExpression(),
                   				field.Name
                   				)
                   			)
                   	}
                );

                statements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodePrimitiveExpression(false),
                            CodeBinaryOperatorType.ValueEquality,
                            new CodeVariableReferenceExpression("notChanged")
                            ),
                        new CodeExpressionStatement(
                            new CodeMethodInvokeExpression(
                                new CodeThisReferenceExpression(),
                                "RaisePropertyChanged",
                                WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propDesc, false),
                                new CodeVariableReferenceExpression("oldValue")
                                )
                            )
                        )
                    );
                usingStatement.Statements = statements.ToArray();
            }
            //else
            //{
            //    throw new NotImplementedException();
            //}
        }

        private void RaiseEntityCtorCreated(CodeEntityTypeDeclaration entityClass, CodeConstructor ctr)
        {
            if (EntityClassCtorCreated != null)
                EntityClassCtorCreated(this, new EntityCtorCreatedEventArgs(entityClass, ctr));
            //EventHandler<EntityCtorCreatedEventArgs> h =
            //    s_ctrl.EventDelegates[EntityGeneratorController.EntityClassCtorCreatedKey] as EventHandler<EntityCtorCreatedEventArgs>;
            //if (h != null)
            //{
            //    h(this, new EntityCtorCreatedEventArgs(entityClass, ctr));
            //}
        }

        private void RaiseEntityClassCreated(CodeNamespace nameSpace, CodeEntityTypeDeclaration entityClass)
        {
            //EventHandler<EntityClassCreatedEventArgs> h = s_ctrl.EventDelegates[EntityGeneratorController.EntityClassCreatedKey] as EventHandler<EntityClassCreatedEventArgs>;
            //if (h != null)
            //{
            //    h(this, new EntityClassCreatedEventArgs(nameSpace, entityClass));
            //}
            if (EntityClassCreated != null)
                EntityClassCreated(this, new EntityClassCreatedEventArgs(nameSpace, entityClass));
        }

        private void OnEntityCtorCustomPropEventsImplementationRequired(object sender, 
            EntityCtorCreatedEventArgs e)
        {
            if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
            {
                e.CtorDeclaration.Statements.Add(
                    new CodeAssignStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(),
                            "_dontRaisePropertyChange"
                            ),
                            new CodePrimitiveExpression(true)
                        )
                    );
            }
        }

        protected virtual void OnPropertyDocumentationRequiered(object sender, EntityPropertyCreatedEventArgs e)
        {
            PropertyDefinition propertyDesc = e.PropertyDescription;
            if (propertyDesc != null)
            {
                CodeMemberProperty property = e.PropertyMember;

                SetMemberDescription(property, propertyDesc.Description);
            }
        }

        protected virtual void OnPropertyCreatedFillEntityInterface(object sender, EntityPropertyCreatedEventArgs e)
        {
            CodeEntityTypeDeclaration entityClass = e.EntityTypeDeclaration;
            CodeMemberProperty propertyMember = e.PropertyMember;

            CodeEntityInterfaceDeclaration entityPropertiesInterface = entityClass.EntityPropertiesInterfaceDeclaration;

            if (entityPropertiesInterface != null)
                CreatePropertyInInterface(entityPropertiesInterface, propertyMember);
        }

        private static void CreatePropertyInInterface(CodeEntityInterfaceDeclaration entityInterface, CodeMemberProperty propertyMember)
        {
            if ((propertyMember.Attributes & MemberAttributes.Public) != MemberAttributes.Public)
                return;

            CodeMemberProperty interfaceProperty = new CodeMemberProperty();
            entityInterface.Members.Add(interfaceProperty);

            if (entityInterface.Entity.BaseEntity != null &&
                (entityInterface.Entity.BaseEntity.Items["TypeDeclaration"] as CodeEntityTypeDeclaration).EntityPropertiesInterfaceDeclaration != null &&
                (entityInterface.Entity.BaseEntity.Items["TypeDeclaration"] as CodeEntityTypeDeclaration).EntityPropertiesInterfaceDeclaration.Members.OfType<CodeMemberProperty>().Any(p => p.Name == propertyMember.Name))
            {
                interfaceProperty.Attributes |= MemberAttributes.New;
            }

            interfaceProperty.HasGet = propertyMember.HasGet;
            interfaceProperty.HasSet = propertyMember.HasSet;
            interfaceProperty.Name = propertyMember.Name;
            interfaceProperty.Type = propertyMember.Type;

            foreach (CodeCommentStatement comment in propertyMember.Comments)
            {
                interfaceProperty.Comments.Add(comment);
            }

            propertyMember.ImplementationTypes.Add(entityInterface.TypeReference);
        }

        protected virtual void OnMakeEntityInterfaceRequired(object sender, EntityClassCreatedEventArgs e)
        {
            CodeEntityTypeDeclaration entityClass = e.TypeDeclaration;
            CodeNamespace entityNamespace = e.Namespace;

            if (entityClass.Entity.MakeInterface)
                CreateEntityInterfaces(entityNamespace, entityClass);
        }

        private void CreateEntityInterfaces(CodeNamespace entityNamespace, 
            CodeEntityTypeDeclaration entityClass)
        {
            CodeEntityInterfaceDeclaration entityInterface = new CodeEntityInterfaceDeclaration(Settings, entityClass);
            CodeEntityInterfaceDeclaration entityPropertiesInterface = new CodeEntityInterfaceDeclaration(Settings, entityClass, null, "Properties");
            entityInterface.Attributes = entityPropertiesInterface.Attributes = MemberAttributes.Public;
            entityInterface.TypeAttributes = entityPropertiesInterface.TypeAttributes = TypeAttributes.Public | TypeAttributes.Interface;

            entityInterface.BaseTypes.Add(entityPropertiesInterface.TypeReference);
            if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
            {
                if (entityClass.Entity.GetPkProperties().Count() == 1)
                    entityInterface.BaseTypes.Add(new CodeTypeReference(typeof (_IKeyEntity)));
                else if (entityClass.Entity.GetPkProperties().Count() > 0)
                    entityInterface.BaseTypes.Add(new CodeTypeReference(typeof (_ICachedEntity)));
                else
                    entityInterface.BaseTypes.Add(new CodeTypeReference(typeof (_IEntity)));
            }

            entityClass.EntityInterfaceDeclaration = entityInterface;
            entityClass.EntityPropertiesInterfaceDeclaration = entityPropertiesInterface;
            entityNamespace.Types.Add(entityInterface);
            entityNamespace.Types.Add(entityPropertiesInterface);
        }

        private static CodeMemberMethod CreateGetValueMethod(CodeEntityTypeDeclaration entityClass)
        {
            CodeMemberMethod method = new CodeMemberMethod
            {
                Name = "GetValueOptimized",
                ReturnType = new CodeTypeReference(typeof (object)),
                Attributes = MemberAttributes.Public
            };

            if (entityClass.Entity.BaseEntity != null)
                method.Attributes |= MemberAttributes.Override;
            else
                method.ImplementationTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
            
            CodeParameterDeclarationExpression prm = new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(string)),
                "propertyAlias"
                );
            method.Parameters.Add(prm);

            prm = new CodeParameterDeclarationExpression(
                new CodeTypeReference(typeof(IEntitySchema)),
                "schema"
                );
            method.Parameters.Add(prm);

            if (entityClass.Entity.BaseEntity != null)
            {
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeBaseReferenceExpression(),
                            method.Name,
                            new CodeArgumentReferenceExpression("propertyAlias"),
                            new CodeArgumentReferenceExpression("schema")
                            )
                        )
                    );
            }
            else
            {
                method.Statements.Add(
                    //new CodeMethodReturnStatement(
                    //    new CodeMethodInvokeExpression(
                    //        new CodePropertyReferenceExpression(
                    //            new CodeThisReferenceExpression(),
                    //            "MappingEngine"
                    //        ),
                    //        "GetPropertyValue",
                    //        new CodeThisReferenceExpression(),
                    //        new CodeArgumentReferenceExpression("propertyAlias")
                    //    )
                    //)
                    Emit.@return((string propertyAlias) =>
                        CodeDom.@this.Call<ObjectMappingEngine>("GetMappingEngine")()
                            .GetPropertyValue(CodeDom.@this.cast<_IEntity>(), propertyAlias))
                );
            }
            entityClass.Members.Add(method);
            return method;
        }

        private void CreateProperties(EntityDefinition entity, CodeEntityTypeDeclaration entityClass, 
            CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod, CodeTypeDeclaration fieldsClass)
        {
            foreach (PropertyDefinition pd in
                from k in entity.GetActiveProperties()
                select k)
            {
                PropertyDefinition propertyDesc = pd;

                #region создание проперти и etc

                FilterPropertyName(propertyDesc);

                if (fieldsClass != null)
                {
                    var propConst = Define.Field(typeof(ObjectProperty),
                        MemberAttributes.Private | MemberAttributes.Static | MemberAttributes.Final,
                        new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.PropertyAlias),
                        ()=>CodeDom.@new(typeof(ObjectProperty),
                            Settings.UseTypeInProps ? 
                                WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(Settings, entity, false) : 
                                WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(Settings, entity, false),
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc, false)
                        )
                    );

                    fieldsClass.Members.Add(propConst);

                    //CodeTypeReferenceExpression classProps = new CodeTypeReferenceExpression(
                    //    /*new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity, true) + "." + */fieldsClass.Name
                    //);

                    var prop = Define.GetProperty(typeof(ObjectProperty),
                        MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                        propertyDesc.PropertyAlias,
                        Emit.@return(()=>CodeDom.VarRef(propConst.Name))
                    );
                    
                    fieldsClass.Members.Add(prop);

                    if (!string.IsNullOrEmpty(propertyDesc.Description))
                        SetMemberDescription(prop, propertyDesc.Description);

                }

                #endregion создание проперти и etc

                CodeMemberProperty property = null;
                if (!propertyDesc.FromBase)
                    property = CreateProperty(entityClass, propertyDesc, entity, setvalueMethod, getvalueMethod);
                else if (propertyDesc is EntityPropertyDefinition)
                {
                    TypeDefinition td = (propertyDesc as EntityPropertyDefinition).NeedReplace();
                    if (td != null)
                    {
                        //CreateProperty(copyMethod, createobjectMethod, entityClass, propertyDesc, settings, setvalueMethod, getvalueMethod);
                        property = CreateUpdatedProperty(entityClass, propertyDesc as EntityPropertyDefinition, td.ToCodeType(Settings));
                    }
                }

                if (property != null)
                {
                    #region property custom attribute Worm.Orm.EntityPropertyAttribute

                    if ((Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                        CreatePropertyColumnAttribute(property, propertyDesc);

                    #endregion property custom attribute Worm.Orm.EntityPropertyAttribute

                    #region property obsoletness

                    CheckPropertyObsoleteAttribute(property, propertyDesc);

                    #endregion
                }
            }
        }

        private static void FilterPropertyName(PropertyDefinition propertyDesc)
        {
            if (propertyDesc.Name == "Identifier")
                throw new ArgumentException("Used reserved property name 'Identifier'");
        }

        public void RaisePropertyCreated(PropertyDefinition propertyDesc, CodeEntityTypeDeclaration entityClass, CodeMemberProperty property, CodeMemberField field)
        {
            //EventHandler<EntityPropertyCreatedEventArgs> h = s_ctrl.EventDelegates[EntityGeneratorController.PropertyCreatedKey] as EventHandler<EntityPropertyCreatedEventArgs>;
            //if (h != null)
            //{
            //    h(null, new EntityPropertyCreatedEventArgs(propertyDesc, entityClass, field, property));
            //}
            if (PropertyCreated != null)
                PropertyCreated(this, new EntityPropertyCreatedEventArgs(propertyDesc, entityClass, field, property));
        }

        private static void CheckPropertyObsoleteAttribute(CodeMemberProperty property, PropertyDefinition propertyDesc)
        {
            if (propertyDesc.Obsolete != ObsoleteType.None)
            {
                CodeAttributeDeclaration attr =
                    new CodeAttributeDeclaration(new CodeTypeReference(typeof(ObsoleteAttribute)),
                                                 new CodeAttributeArgument(
                                                    new CodePrimitiveExpression(propertyDesc.ObsoleteDescripton)),
                                                 new CodeAttributeArgument(
                                                    new CodePrimitiveExpression(propertyDesc.Obsolete == ObsoleteType.Error)));
                if (property.CustomAttributes == null)
                    property.CustomAttributes = new CodeAttributeDeclarationCollection();
                property.CustomAttributes.Add(attr);
            }
        }

        private CodeMemberProperty CreateUpdatedProperty(CodeEntityTypeDeclaration entityClass, 
            EntityPropertyDefinition propertyDesc, CodeTypeReference propertyType)
        {
            CodeMemberProperty property = new CodeMemberProperty
            {
                HasGet = true,
                HasSet = true,
                Name = propertyDesc.Name,
                Type = propertyType,
                Attributes = MemberAttributes.Public | MemberAttributes.Final |
                    MemberAttributes.New
            };

            property.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(
                        propertyType,
                        new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name)
                    )
                )
            );
            
            if (!propertyDesc.HasAttribute(Field2DbRelations.ReadOnly))
            {

                property.SetStatements.Add(
                    new CodeAssignStatement(
                        new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name),
                        new CodePropertySetValueReferenceExpression()
                        )
                    );
            }
            else
            {
                property.HasSet = false;
            }

            if (propertyDesc.Group != null && propertyDesc.Group.Hide)
                property.Attributes = MemberAttributes.Family;

            RaisePropertyCreated(propertyDesc, entityClass, property, null);

            entityClass.Members.Add(property);

            return property;
        }

        private CodeMemberProperty CreateProperty(CodeEntityTypeDeclaration entityClass,
            PropertyDefinition propertyDesc, EntityDefinition entity,
            CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod)
        {
            CodeTypeReference fieldType = propertyDesc.PropertyType.ToCodeType(Settings);
            CodeMemberProperty property = null;

            bool emptyField = propertyDesc is ScalarPropertyDefinition ?
                string.IsNullOrEmpty((propertyDesc as ScalarPropertyDefinition).SourceFieldExpression) :
                ((EntityPropertyDefinition) propertyDesc).SourceFields.Count() == 0;
            
            if (emptyField && entity.GetPropertiesFromBase().Any(item => 
                !item.Disabled && item.Name == propertyDesc.Name))
            {
                var baseProp = entity.GetPropertiesFromBase().Single(item => item.Name == propertyDesc.Name);
                if (baseProp.HasAttribute(Field2DbRelations.ReadOnly))
                {
                    property = Define.GetProperty(fieldType,
                        GetMemberAttribute(propertyDesc) | MemberAttributes.New | MemberAttributes.Final,
                        propertyDesc.Name,
                        CodeDom.CombineStmts(
                            Emit.@return(() => CodeDom.cast(fieldType, CodeDom.@base.Property(propertyDesc.Name)))
                        )
                    );
                }
                else
                {
                    property = Define.Property(fieldType,
                        GetMemberAttribute(propertyDesc) | MemberAttributes.New | MemberAttributes.Final,
                        propertyDesc.Name,
                        CodeDom.CombineStmts(
                            Emit.@return(() => CodeDom.cast(fieldType, CodeDom.@base.Property(propertyDesc.Name)))
                        ),
                        Emit.assignProperty(new CodeBaseReferenceExpression(), propertyDesc.Name,
                            () => CodeDom.cast(baseProp.PropertyType.ToCodeType(Settings), CodeDom.VarRef("value"))
                        )
                    );
                }
            }
            else
            {
                string fieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.Name);

                CodeMemberField field = new CodeMemberField(fieldType, fieldName)
                {
                    Attributes = GetMemberAttribute(propertyDesc.FieldAccessLevel)
                };

                property = new CodeMemberProperty
                {
                    HasGet = true,
                    HasSet = true,
                    Name = propertyDesc.Name,
                    Type = fieldType,
                    Attributes = GetMemberAttribute(propertyDesc)
                };

                if (entity.GetPropertiesFromBase().Any(k => propertyDesc.Name == k.Name))
                {
                    property.Attributes |= MemberAttributes.Override;
                }

                #region property GetStatements

                CodeMethodInvokeExpression getUsingExpression = new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Read"),
                    WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(propertyDesc)
                    );

                if (propertyDesc.PropertyType.IsEntityType && propertyDesc.PropertyType.Entity.CacheCheckRequired)
                {
                    getUsingExpression.Parameters.Add(new CodePrimitiveExpression(true));
                }

                CodeStatement[] getInUsingStatements = new CodeStatement[]
                {
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(
                            new CodeThisReferenceExpression(), fieldName))
                };

                if (entity.GetPkProperties().Count() > 0 &&
                    (Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                    property.GetStatements.Add(new CodeUsingStatement(getUsingExpression, getInUsingStatements));
                else
                    property.GetStatements.AddRange(getInUsingStatements);

                #endregion property GetStatements

                #region property SetStatements

                if (_model.EnableReadOnlyPropertiesSetter ||
                    !propertyDesc.HasAttribute(Field2DbRelations.ReadOnly) ||
                    propertyDesc.HasAttribute(Field2DbRelations.PK))
                {
                    CodeExpression setUsingExpression = new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Write"),
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(propertyDesc)
                        );

                    CodeStatement[] setInUsingStatements = new CodeStatement[]
                    {
                        new CodeAssignStatement(
                            new CodeFieldReferenceExpression(
                                new CodeThisReferenceExpression(),
                                  fieldName
                                ),
                            new CodePropertySetValueReferenceExpression()
                        )
                    };

                    if (entity.GetPkProperties().Count() > 0 &&
                        (Settings.GenerateMode.HasValue ? Settings.GenerateMode.Value : _model.GenerateMode) != GenerateModeEnum.EntityOnly)
                        property.SetStatements.Add(new CodeUsingStatement(setUsingExpression, setInUsingStatements));
                    else
                        property.SetStatements.AddRange(setInUsingStatements);
                }
                else
                    property.HasSet = false;

                #endregion property SetStatements

                RaisePropertyCreated(propertyDesc, entityClass, property, field);

                #region добавление членов в класс

                entityClass.Members.Add(field);

                #endregion добавление членов в класс

                #region void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

                if (setvalueMethod != null)
                    UpdateSetValueMethodMethod(propertyDesc, setvalueMethod);

                #endregion void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

                #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectsSchema schema)

                if (getvalueMethod != null)
                    UpdateGetValueMethod(propertyDesc, getvalueMethod);

                #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectsSchema schema)

            }

            entityClass.Members.Add(property);
            return property;
        }

        private void UpdateGetValueMethod(PropertyDefinition propertyDesc, CodeMemberMethod getvalueMethod)
        {
            getvalueMethod.Statements.Insert(getvalueMethod.Statements.Count - 1,
                new CodeConditionStatement(
                    new CodeMethodInvokeExpression(
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(propertyDesc),
                        "Equals",
                        new CodeArgumentReferenceExpression("propertyAlias")
                    ),
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), 
                            new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.Name)
                        )
                    )
                )
            );
        }
        private static void CreatePropertyColumnAttribute(CodeMemberProperty property, PropertyDefinition propertyDesc)
        {
            CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(
                new CodeTypeReference(typeof(EntityPropertyAttribute))
            );

            if (!string.IsNullOrEmpty(propertyDesc.PropertyAlias))
            {
                declaration.Arguments.Add(
                    new CodeAttributeArgument("PropertyAlias", 
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(propertyDesc)
                    )
                );
            }

            property.CustomAttributes.Add(declaration);
        }

        //private static void UpdateCreateObjectMethod(CodeMemberMethod createobjectMethod, PropertyDescription propertyDesc)
        //{
        //    if (createobjectMethod == null)
        //        return;
        //    createobjectMethod.Statements.Insert(createobjectMethod.Statements.Count - 1,
        //        new CodeConditionStatement(
        //            new CodeBinaryOperatorExpression(
        //                new CodeArgumentReferenceExpression("fieldName"),
        //                CodeBinaryOperatorType.ValueEquality,
        //                new CodePrimitiveExpression(propertyDesc.PropertyAlias)
        //                ),
        //            new CodeThrowExceptionStatement(
        //                new CodeObjectCreateExpression(
        //                    new CodeTypeReference(typeof(NotImplementedException)),
        //                    new CodePrimitiveExpression("The method or operation is not implemented.")
        //                    )
        //                )
        //            )
        //        );
        //}

        //private static CodeMemberMethod CreateCreateObjectMethod(EntityDescription entity, CodeTypeDeclaration entityClass)
        //{
        //    CodeMemberMethod createobjectMethod;
        //    createobjectMethod = new CodeMemberMethod();
        //    if (entity.Behaviour != EntityBehaviuor.PartialObjects)
        //        entityClass.Members.Add(createobjectMethod);
        //    createobjectMethod.Name = "CreateObject";
        //    // тип возвращаемого значения
        //    createobjectMethod.ReturnType = null;
        //    // модификаторы доступа
        //    createobjectMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;

        //    createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fieldName"));
        //    createobjectMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));

        //    createobjectMethod.Statements.Add(
        //        new CodeThrowExceptionStatement(
        //            new CodeObjectCreateExpression(
        //                new CodeTypeReference(typeof(InvalidOperationException)),
        //                new CodePrimitiveExpression("Invalid method usage.")
        //            )
        //        )
        //        );

        //    return createobjectMethod;
        //}

        private static CodeMemberMethod CreateSetValueMethod(CodeEntityTypeDeclaration entityClass)
        {
            CodeMemberMethod setvalueMethod = new CodeMemberMethod();
            entityClass.Members.Add(setvalueMethod);
            setvalueMethod.Name = "SetValueOptimized";
            // тип возвращаемого значения
            setvalueMethod.ReturnType = null;
            // модификаторы доступа
            setvalueMethod.Attributes = MemberAttributes.Public;
            if (entityClass.Entity.BaseEntity != null)
                setvalueMethod.Attributes |= MemberAttributes.Override;
            else
                setvalueMethod.ImplementationTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "propertyAlias"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEntitySchema), "schema"));
            setvalueMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
            setvalueMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(string)),
                    "fieldName",
                    new CodeArgumentReferenceExpression("propertyAlias")
                )
            );
            return setvalueMethod;
        }

        public static CodeStatement CodePatternDoubleCheckLock(CodeExpression lockExpression, CodeExpression condition, params CodeStatement[] statements)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            return new CodeConditionStatement(
                condition,
                new CodeLockStatement(lockExpression,
                    new CodeConditionStatement(condition, statements)
                )
            );
        }
    }
}
