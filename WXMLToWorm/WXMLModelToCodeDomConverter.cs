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

namespace WXMLToWorm
{
    public class WormCodeDomGenerator
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
        private readonly WXMLCodeDomGeneratorSettings _generatorSettings;

        public WormCodeDomGenerator(WXMLModel ormObjectsDefinition, WXMLCodeDomGeneratorSettings settings)
        {
            _model = ormObjectsDefinition;
            _generatorSettings = settings;
            //WXMLCodeDomGeneratorNameHelper.OrmCodeDomGeneratorSettingsRequied += GetSettings;
            //WXMLCodeDomGenerator.Delegates.SettingsRequied += GetSettings;
        }

        protected WXMLCodeDomGeneratorSettings Settings
        {
            get { return _generatorSettings; }
        }

        public Dictionary<string, CodeCompileFileUnit> GetFullDom(LinqToCodedom.CodeDomGenerator.Language language)
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

        //[ThreadStatic]
        //internal static EntityGeneratorController s_ctrl;

        public CodeCompileFileUnit GetFullSingleUnit(LinqToCodedom.CodeDomGenerator.Language language)
        {
            CodeCompileFileUnit unit = new CodeCompileFileUnit();
            foreach (CodeCompileFileUnit u in GetFullDom(language).Values)
            {
                foreach (CodeNamespace n in u.Namespaces)
                {
                    CodeNamespace ns = n;
                    if (unit.Namespaces.Count == 0)
                    {
                        ns = new CodeNamespace(n.Name);
                        unit.Namespaces.Add(ns);
                    }
                    else
                        ns = unit.Namespaces[0];

                    foreach (CodeTypeDeclaration c in n.Types)
                    {
                        ns.Types.Add(c);
                    }
                }
            }
            CodeCompileFileUnit linq = GetLinqContext();
            if (linq != null)
                foreach (CodeNamespace n in linq.Namespaces)
                {
                    unit.Namespaces.Add(n);
                }

            return unit;
        }

        public CodeCompileFileUnit GetLinqContext()
        {
            if (_model.LinqSettings == null) return null;

            var ctx = new CodeLinqContextDeclaration(Settings, _model.LinqSettings);

            ctx.Entities.AddRange(_model.FileEntities);

            var result = new CodeCompileFileUnit
                             {
                                 Filename =
                                     !string.IsNullOrEmpty(_model.LinqSettings.FileName)
                                         ? _model.LinqSettings.FileName
                                         : ctx.Name
                             };

            var ns = new CodeNamespace(_model.Namespace);
            ns.Types.Add(ctx);
            result.Namespaces.Add(ns);

            return result;

        }

        [Obsolete("Use GetEntityCompileUnits instead.")]
        public Dictionary<string, CodeCompileUnit> GetEntityDom(string entityId,
            WXMLCodeDomGeneratorSettings settings, LinqToCodedom.CodeDomGenerator.Language language)
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
                    return ns.Types.OfType<CodeEntityTypeDeclaration>().SingleOrDefault(e => e.Name == new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity));
                }
            }

            return null;
        }

        public IList<CodeCompileFileUnit> GetEntityCompileUnits(string entityId, LinqToCodedom.CodeDomGenerator.Language language)
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

                    CodeTypeDeclaration propertiesClass;
                    CodeTypeDeclaration fieldsClass = null;
                    CodeTypeDeclaration propertyAliasClass = null;
                    CodeTypeDeclaration instancedPropertyAliasClass = null;

                    CodeMemberMethod method;

                    #region определение класса сущности

                    CodeCompileFileUnit entityUnit = new CodeCompileFileUnit
                    {
                        Filename = new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityFileName(entity)
                    };
                    result.Add(entityUnit);

                    // неймспейс
                    CodeNamespace nameSpace = new CodeNamespace(entity.Namespace);
                    entityUnit.Namespaces.Add(nameSpace);

                    // класс сущности
                    CodeEntityTypeDeclaration entityClass = new CodeEntityTypeDeclaration(Settings, entity, Settings.UseTypeInProps, this);
                    nameSpace.Types.Add(entityClass);

                    // параметры класса
                    entityClass.IsClass = true;
                    var behaviour = entity.Behaviour;
                    entityClass.IsPartial = behaviour == EntityBehaviuor.PartialObjects ||
                                            behaviour == EntityBehaviuor.ForcePartial;
                    entityClass.Attributes = MemberAttributes.Public;
                    entityClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;

                    #endregion определение класса сущности

                    if (!_model.GenerateSchemaOnly)
                    {
                        // дескрипшн
                        WXMLCodeDomGenerator.SetMemberDescription(entityClass, entity.Description);

                        // интерфес сущности

                        // базовый класс
                        if (entity.BaseEntity == null)
                        {
                            CodeTypeReference entityType;
                            if (_model.EntityBaseType == null)
                            {
                                entityType = new CodeTypeReference(entity.HasSinglePk ? typeof(KeyEntity) : entity.HasPk ? typeof(CachedLazyLoad) : typeof(Entity));
                            }
                            else
                            {
                                entityType =
                                    new CodeTypeReference(_model.EntityBaseType.IsEntityType
                                                              ? new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(
                                                                    _model.EntityBaseType.Entity,
                                                                    true)
                                                              : _model.EntityBaseType.GetTypeName(Settings));
                                entityType.TypeArguments.Add(
                                    new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity)));
                            }

                            entityClass.BaseTypes.Add(entityType);
                            entityClass.BaseTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
                        }

                        else
                            entityClass.BaseTypes.Add(
                                new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity.BaseEntity, true)));

                        RaiseEntityClassCreated(nameSpace, entityClass);

                        #region определение класса Properties

                        {
                            propertiesClass = new CodeTypeDeclaration("Properties");
                            propertiesClass.Attributes = MemberAttributes.Public;
                            propertiesClass.TypeAttributes = TypeAttributes.Class | TypeAttributes.NestedPublic;
                            propertiesClass.IsPartial = true;
                            CodeConstructor propctr = new CodeConstructor();
                            propctr.Attributes = MemberAttributes.Family;
                            propertiesClass.Members.Add(propctr);
                            WXMLCodeDomGenerator.SetMemberDescription(propertiesClass, "Алиасы свойств сущностей испльзуемые в объектной модели.");

                            if (entity.BaseEntity != null)
                                propertiesClass.Attributes |= MemberAttributes.New;

                            entityClass.Members.Add(propertiesClass);
                        }

                        #endregion определение класса Properties

                        #region определение класса Fields
                        if (_model.GenerateEntityName)
                        {
                            fieldsClass = new CodeTypeDeclaration("props")
                                              {
                                                  Attributes = MemberAttributes.Public,
                                                  TypeAttributes = (TypeAttributes.Class | TypeAttributes.NestedPublic),
                                                  IsPartial = true
                                              };
                            var propctr = new CodeConstructor { Attributes = MemberAttributes.Family };
                            fieldsClass.Members.Add(propctr);

                            WXMLCodeDomGenerator.SetMemberDescription(fieldsClass, "Ссылки на поля сущностей.");

                            entityClass.Members.Add(fieldsClass);

                            if (entity.BaseEntity != null)
                                fieldsClass.Attributes |= MemberAttributes.New;
                        }

                        #endregion определение класса Fields

                        #region определение класса Descriptor
                        if (_model.GenerateEntityName)
                        {
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

                            WXMLCodeDomGenerator.SetMemberDescription(descriptorClass, "Описатель сущности.");

                            var entityNameField = new CodeMemberField
                            {
                                Type = new CodeTypeReference(typeof(string)),
                                Name = "EntityName",
                                InitExpression = new CodePrimitiveExpression(entity.FamilyName),
                                Attributes = (MemberAttributes.Public | MemberAttributes.Const)
                            };

                            descriptorClass.Members.Add(entityNameField);

                            WXMLCodeDomGenerator.SetMemberDescription(entityNameField, "Имя сущности в объектной модели.");

                            entityClass.Members.Add(descriptorClass);

                            if (entity.BaseEntity != null)
                                descriptorClass.Attributes |= MemberAttributes.New;
                        }

                        #endregion

                        #region PropertyAlias class
                        if (_model.GenerateEntityName)
                        {
                            propertyAliasClass = new CodeTypeDeclaration
                                                     {
                                                         Name = entity.Name + "Alias",
                                                         Attributes = MemberAttributes.Family,
                                                     };

                            var propertyAliasClassCtor = new CodeConstructor { Attributes = MemberAttributes.Public };

                            if (Settings.UseTypeInProps)
                                propertyAliasClassCtor.BaseConstructorArgs.Add(WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(Settings, entity));
                            else
                                propertyAliasClassCtor.BaseConstructorArgs.Add(WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(Settings, entity));

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
                                                                     new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName("objectAlias")),
                                    new CodeArgumentReferenceExpression("objectAlias")));

                            instancedPropertyAliasClass.Members.Add(instancedPropertyAliasClassCtor);

                            var instancedPropertyAliasfield = new CodeMemberField(new CodeTypeReference(typeof(QueryAlias)),
                                                            new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName("objectAlias"));
                            instancedPropertyAliasClass.Members.Add(instancedPropertyAliasfield);

                            instancedPropertyAliasClass.Members.Add(
                                //Delegates.CodeMemberOperatorOverride(
                                //    CodeDomPatterns.OperatorType.Implicit,
                                //    new CodeTypeReference(typeof(EntityAlias)),
                                //    new[]{new CodeParameterDeclarationExpression(new CodeTypeReference(OrmCodeGenNameHelper.GetEntityClassName(entity) + "." +
                                //                                          instancedPropertyAliasClass.Name), "entityAlias")},
                                Define.Operator(new CodeTypeReference(typeof(QueryAlias)),
                                    (DynType entityAlias) => CodeDom.TypedSeq(OperatorType.Implicit,
                                        entityAlias.SetType(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity) + "." + instancedPropertyAliasClass.Name)),
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
                        }

                        #endregion

                        #region ObjectAlias methods
                        if (propertyAliasClass != null)
                        {
                            var createMethod = new CodeMemberMethod
                           {
                               Name = "CreateAlias",
                               ReturnType =
                                new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity) + "." +
                                                      propertyAliasClass.Name),
                               Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                           };
                            if (entity.BaseEntity != null)
                                createMethod.Attributes |= MemberAttributes.New;
                            createMethod.Statements.Add(
                                new CodeMethodReturnStatement(
                                    new CodeObjectCreateExpression(
                                        new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity, true) + "." +
                                                              propertyAliasClass.Name))));
                            entityClass.Members.Add(createMethod);

                            var getMethod = new CodeMemberMethod
                            {
                                Name = "WrapAlias",
                                ReturnType =
                                    new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity) + "." +
                                                          instancedPropertyAliasClass.Name),
                                Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                            };

                            if (entity.BaseEntity != null)
                                getMethod.Attributes |= MemberAttributes.New;
                            getMethod.Parameters.Add(new CodeParameterDeclarationExpression { Name = "objectAlias", Type = new CodeTypeReference(typeof(QueryAlias)) });

                            getMethod.Statements.Add(
                                new CodeMethodReturnStatement(
                                    new CodeObjectCreateExpression(
                                        new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity, true) + "." +
                                                              instancedPropertyAliasClass.Name), new CodeArgumentReferenceExpression("objectAlias"))));
                            entityClass.Members.Add(getMethod);
                        }
                        #endregion

                        #region конструкторы

                        // конструктор по умолчанию
                        CodeConstructor ctr = new CodeConstructor();
                        ctr.Attributes = MemberAttributes.Public;
                        entityClass.Members.Add(ctr);

                        RaiseEntityCtorCreated(entityClass, ctr);

                        //if(
                        if (entity.HasSinglePk)
                        {
                            PropertyDefinition pkProperty = entity.GetPkProperties().Single();
                            // параметризированный конструктор
                            ctr = new CodeConstructor();
                            ctr.Attributes = MemberAttributes.Public;
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

                        CopyPropertiesMethodGeneration(entityClass);

                        #endregion метод OrmBase.CopyBody(CopyBody(OrmBase from, OrmBase to)

                        #region void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

                        CodeMemberMethod setvalueMethod = CreateSetValueMethod(entityClass);

                        #endregion void SetValue(System.Reflection.PropertyInfo pi, string propertyAlias, object value)

                        #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                        CodeMemberMethod getvalueMethod = CreateGetValueMethod(entityClass);

                        #endregion public override object GetValue(string propAlias, Worm.Orm.IOrmObjectSchemaBase schema)

                        #region CachedEntity methods

                        //CodeMemberMethod createobjectMethod = null;

                        if (entity.HasPk)
                        {
                            if (entity.HasSinglePk)
                            {
                                if (entity.BaseEntity == null || entity.GetPKCount(false) > 0)
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
                                if (entity.BaseEntity == null || entity.GetPKCount(false) > 0)
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

                        #region проперти

                        CreateProperties(entity, entityClass, setvalueMethod, getvalueMethod, propertiesClass, fieldsClass, propertyAliasClass, instancedPropertyAliasClass);

                        #endregion проперти

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

                        if (!Settings.RemoveOldM2M && entity.HasSinglePk)
                            CreateM2MMethodsSet(entity, entityClass);

                        #endregion

                        //if (setvalueMethod.Statements.Count <= 1)
                        //    entityClass.Members.Remove(setvalueMethod);
                    }

                    #region custom attribute EntityAttribute

                    entityClass.CustomAttributes = new CodeAttributeDeclarationCollection(
                        new[]
			                {
			                    new CodeAttributeDeclaration(
			                        new CodeTypeReference(typeof (EntityAttribute)),
			                        new CodeAttributeArgument(
			                            new CodeTypeOfExpression(
			                                new CodeTypeReference(
			                                    new WXMLCodeDomGeneratorNameHelper(Settings).GetEntitySchemaDefClassQualifiedName(entity))
			                                )
			                            ),
			                        new CodeAttributeArgument(
			                            new CodePrimitiveExpression(_model.SchemaVersion)
			                            ),
			                        new CodeAttributeArgument(
			                            "EntityName",
			                            WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(Settings,entity)
			                            )
			                        ),
			                }
                        );

                    if (!_model.GenerateSchemaOnly)
                        entityClass.CustomAttributes.Add(new CodeAttributeDeclaration(
                            new CodeTypeReference(typeof(SerializableAttribute))
                            )
                        );

                    #endregion custom attribute EntityAttribute

                    #region энам табличек

                    CreateTablesLinkEnum(entity, entityClass.SchemaDef);

                    #endregion энам табличек

                    #region bool ChangeValueType(EntityPropertyAttribute c, object value, ref object newvalue)

                    CreateChangeValueTypeMethod(entity, entityClass.SchemaDef);

                    #endregion bool ChangeValueType(EntityPropertyAttribute c, object value, ref object newvalue)

                    #region string[] GetSuppressedColumns()

                    // первоначальная реализация или есть отличие в suppressed properties
                    if (entity.BaseEntity == null ||
                        (entity.BaseEntity.SuppressedProperties.Count + entity.SuppressedProperties.Count != 0) ||
                        entity.BaseEntity.SuppressedProperties.Count != entity.SuppressedProperties.Count ||
                        !(entity.SuppressedProperties.TrueForAll(p => entity.BaseEntity.SuppressedProperties.Exists(pp => pp == p)) &&
                        entity.BaseEntity.SuppressedProperties.TrueForAll(p => entity.SuppressedProperties.Exists(pp => pp == p))))
                    {

                        method = new CodeMemberMethod();
                        entityClass.SchemaDef.Members.Add(method);
                        method.Name = "GetSuppressedFields";
                        // тип возвращаемого значения
                        method.ReturnType = new CodeTypeReference(typeof(string[]));
                        // модификаторы доступа
                        method.Attributes = MemberAttributes.Public;

                        if (entity.BaseEntity != null)
                            method.Attributes |= MemberAttributes.Override;
                        else
                            // реализует метод базового класса
                            method.ImplementationTypes.Add(typeof(IEntitySchemaBase));
                        CodeArrayCreateExpression arrayExpression = new CodeArrayCreateExpression(
                            new CodeTypeReference(typeof(string[]))
                        );


                        foreach (var suppressedProperty in entity.SuppressedProperties)
                        {
                            arrayExpression.Initializers.Add(
                                new CodePrimitiveExpression(suppressedProperty)
                            );
                        }

                        method.Statements.Add(new CodeMethodReturnStatement(arrayExpression));
                    }

                    #endregion EntityPropertyAttribute[] GetSuppressedColumns()

                    #region сущность реализует связь

                    RelationDefinitionBase relation = _model.ActiveRelations
                        .Find(match => match.UnderlyingEntity == entity);

                    if (relation != null)
                    {
                        SelfRelationDescription sd = relation as SelfRelationDescription;
                        if (sd == null)
                            ImplementIRelation((RelationDefinition)relation, entity, entityClass.SchemaDef);
                        else
                            ImplementIRelation(sd, entity, entityClass.SchemaDef);
                    }

                    #endregion сущность реализует связь

                    #region public void GetSchema(OrmSchemaBase schema, Type t)

                    if (entity.BaseEntity == null)
                    {
                        CodeMemberField schemaField = new CodeMemberField(
                            new CodeTypeReference(typeof(ObjectMappingEngine)),
                            "_schema"
                            );
                        CodeMemberField typeField = new CodeMemberField(
                            new CodeTypeReference(typeof(Type)),
                            "_entityType"
                            );
                        schemaField.Attributes = MemberAttributes.Family;
                        entityClass.SchemaDef.Members.Add(schemaField);
                        typeField.Attributes = MemberAttributes.Family;
                        entityClass.SchemaDef.Members.Add(typeField);
                        method = new CodeMemberMethod();
                        entityClass.SchemaDef.Members.Add(method);
                        method.Name = "GetSchema";
                        // тип возвращаемого значения
                        method.ReturnType = null;
                        // модификаторы доступа
                        method.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                        method.Parameters.Add(
                            new CodeParameterDeclarationExpression(
                                new CodeTypeReference(typeof(ObjectMappingEngine)),
                                "schema"
                                )
                            );
                        method.Parameters.Add(
                            new CodeParameterDeclarationExpression(
                                new CodeTypeReference(typeof(Type)),
                                "t"
                                )
                            );
                        // реализует метод базового класса
                        method.ImplementationTypes.Add(typeof(ISchemaInit));
                        method.Statements.Add(
                            new CodeAssignStatement(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "_schema"
                                    ),
                                new CodeArgumentReferenceExpression("schema")
                                )
                            );
                        method.Statements.Add(
                            new CodeAssignStatement(
                                new CodeFieldReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "_entityType"
                                    ),
                                new CodeArgumentReferenceExpression("t")
                                )
                            );
                    }

                    #endregion public void GetSchema(OrmSchemaBase schema, Type t)

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
            if (entity.GetPkProperties().Count() == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "SetPK";

            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(new CodeTypeReference(typeof(PKDesc)), 1), "pks")
            );

            meth.Parameters.Add(
                new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(ObjectMappingEngine)), "mpe")
            );

            meth.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name,
                                                               new CodeArgumentReferenceExpression(
                                                                   meth.Parameters[0].Name)));

            if (composite)
            {
                meth.Statements.Add(
                    //Delegates.CodePatternForeachStatement(
                    //    new CodeTypeReference(typeof(PKDesc)), "pk",
                    //    new CodeArgumentReferenceExpression("pks"),
                    Emit.@foreach("pk", () => CodeDom.VarRef<PKDesc[]>("pks"),
                        entity.GetPkProperties().
                        Select((PropertyDefinition pd_) =>
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
                PropertyDefinition pkProperty = entity.GetPkProperties().Single();
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
            if (entity.GetPkProperties().Count() == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetPKValues";
            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(tr, 1);

            // модификаторы доступа
            meth.Attributes = MemberAttributes.Public | MemberAttributes.Override;

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
                        entity.GetPkProperties().Select<PropertyDefinition, CodeExpression>(pd_ => new CodeObjectCreateExpression(tr, new CodePrimitiveExpression(pd_.PropertyAlias),
                                                              new CodeFieldReferenceExpression(
                                                                  new CodeThisReferenceExpression(),
                                                                  new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName
                                                                      (pd_.Name)))).ToArray()
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

            if (entity.GetPkProperties().Count() == 0)
                return;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetCacheKey";
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(typeof(Int32));
            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            CodeExpression lf = new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), meth.Name);

            foreach (PropertyDefinition pd in entity.SelfProperties)
            {
                if (pd.HasAttribute(WXML.Model.Field2DbRelations.PK))
                {
                    string fn = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pd.Name);

                    CodeExpression exp = new CodeMethodInvokeExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fn),
                        "GetHashCode", new CodeExpression[0]);

                    lf = new CodeXorExpression(lf, exp);

                }
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
            PropertyDefinition pkProperty = entityClass.Entity.GetPkProperties().Single();
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
            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "SetPK";

            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

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
                        Select(
                            delegate(PropertyDefinition pd_)
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
                PropertyDefinition pkProperty = entity.GetPkProperties().Single();
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
            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetPKValues";
            CodeTypeReference tr = new CodeTypeReference(typeof(PKDesc));
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(tr, 1);

            // модификаторы доступа
            meth.Attributes = MemberAttributes.Public | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            meth.Statements.Add(
                new CodeMethodReturnStatement(new CodeArrayCreateExpression(meth.ReturnType,
                    entity.SelfProperties.Where(
                        pd_ => pd_.HasAttribute(WXML.Model.Field2DbRelations.PK)).
                    Select<PropertyDefinition, CodeExpression>(
                        pd_ => new CodeObjectCreateExpression(tr, new CodePrimitiveExpression(pd_.PropertyAlias),
                                                              new CodeFieldReferenceExpression(
                                                                  new CodeThisReferenceExpression(),
                                                                  new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(
                                                                      pd_.Name)))
                        ).ToArray()))
                );
        }


        private void CreateGetKeyMethodCompositePK(CodeEntityTypeDeclaration entityClass)
        {
            EntityDefinition entity = entityClass.Entity;

            CodeMemberMethod meth = new CodeMemberMethod();
            meth.Name = "GetCacheKey";
            // тип возвращаемого значения
            meth.ReturnType = new CodeTypeReference(typeof(Int32));
            // модификаторы доступа
            meth.Attributes = MemberAttributes.Family | MemberAttributes.Override;

            entityClass.Members.Add(meth);

            CodeExpression lf = null;

            foreach (PropertyDefinition pd in entity.SelfProperties)
            {
                if (pd.HasAttribute(WXML.Model.Field2DbRelations.PK))
                {
                    string fn = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(pd.Name);

                    CodeExpression exp = new CodeMethodInvokeExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fn),
                        "GetHashCode", new CodeExpression[0]);

                    lf = lf == null ? exp : new CodeXorExpression(lf, exp);
                }
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
                                WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propDesc),
                                new CodeVariableReferenceExpression("oldValue")
                                )
                            )
                        )
                    );
                usingStatement.Statements = statements.ToArray();
            }
            else
            {
                throw new NotImplementedException();
            }
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

        static void OnEntityCtorCustomPropEventsImplementationRequired(object sender, EntityCtorCreatedEventArgs e)
        {
            CodeConstructor con = e.CtorDeclaration;

            con.Statements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(
                        new CodeThisReferenceExpression(),
                        "_dontRaisePropertyChange"
                        ),
                        new CodePrimitiveExpression(true)
                    )
                );
        }

        protected virtual void OnPropertyDocumentationRequiered(object sender, EntityPropertyCreatedEventArgs e)
        {
            PropertyDefinition propertyDesc = e.PropertyDescription;
            if (propertyDesc != null)
            {
                CodeMemberProperty property = e.PropertyMember;

                WXMLCodeDomGenerator.SetMemberDescription(property, propertyDesc.Description);
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

        private void CreateEntityInterfaces(CodeNamespace entityNamespace, CodeEntityTypeDeclaration entityClass)
        {
            CodeEntityInterfaceDeclaration entityInterface = new CodeEntityInterfaceDeclaration(Settings, entityClass);
            CodeEntityInterfaceDeclaration entityPropertiesInterface = new CodeEntityInterfaceDeclaration(Settings, entityClass, null, "Properties");
            entityInterface.Attributes = entityPropertiesInterface.Attributes = MemberAttributes.Public;
            entityInterface.TypeAttributes = entityPropertiesInterface.TypeAttributes = TypeAttributes.Public | TypeAttributes.Interface;

            entityInterface.BaseTypes.Add(entityPropertiesInterface.TypeReference);
            if (entityClass.Entity.HasSinglePk)
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_IKeyEntity)));
            else if (entityClass.Entity.HasPk)
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_ICachedEntity)));
            else
                entityInterface.BaseTypes.Add(new CodeTypeReference(typeof(_IEntity)));

            entityClass.EntityInterfaceDeclaration = entityInterface;
            entityClass.EntityPropertiesInterfaceDeclaration = entityPropertiesInterface;
            entityNamespace.Types.Add(entityInterface);
            entityNamespace.Types.Add(entityPropertiesInterface);
        }

        #region M2M methods
        private void CreateM2MMethodsSet(EntityDefinition entity, CodeTypeDeclaration entityClass)
        {
            foreach (RelationDefinitionBase relation in entity.GetAllRelations(false))
            {
                LinkTarget link = null;
                RelationDefinition rd = relation as RelationDefinition;
                if (rd != null)
                    link = rd.Left.Entity == entity ? rd.Right : rd.Left;
                if (!relation.HasAccessors || (link != null && !link.Entity.HasSinglePk))
                    continue;
                CreateM2MGenMethods(CreateM2MAddMethod, entity, relation, entityClass);
                CreateM2MGenMethods(CreateM2MRemoveMethod, entity, relation, entityClass);
                CreateM2MGenMethods(CreateM2MGetMethod, entity, relation, entityClass);
                CreateM2MGenMethods(CreateM2MMergeMethod, entity, relation, entityClass);
            }
        }

        private delegate CodeMemberMethod CreateM2MMethodDelegate(string accessorName, EntityDefinition relatedEntity, TypeDefinition relatedEntityType, bool? direct);

        private void CreateM2MGenMethods(CreateM2MMethodDelegate del, EntityDefinition entity, RelationDefinitionBase relation, CodeTypeDeclaration entityClass)
        {
            RelationDefinition rel = relation as RelationDefinition;
            if (rel != null)
            {
                LinkTarget link = rel.Left.Entity == entity ? rel.Right : rel.Left;
                string accessorName = link.AccessorName;
                EntityDefinition relatedEntity = link.Entity;
                if (!string.IsNullOrEmpty(accessorName))
                    entityClass.Members.Add(del(accessorName, relatedEntity, link.AccessedEntityType, null));
            }

            SelfRelationDescription selfRel = relation as SelfRelationDescription;
            if (selfRel != null)
            {

                if (!string.IsNullOrEmpty(selfRel.Direct.AccessorName))
                    entityClass.Members.Add(del(selfRel.Direct.AccessorName, entity, selfRel.Direct.AccessedEntityType, true));
                if (!string.IsNullOrEmpty(selfRel.Reverse.AccessorName))
                    entityClass.Members.Add(del(selfRel.Reverse.AccessorName, entity, selfRel.Reverse.AccessedEntityType, false));
            }
        }

        private CodeMemberMethod CreateM2MAddMethod(string accessorName, EntityDefinition relatedEntity, TypeDefinition relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Add" + accessorName;

            CodeParameterDeclarationExpression relObjectPrm = new CodeParameterDeclarationExpression();
            method.Parameters.Add(relObjectPrm);
            method.Attributes = MemberAttributes.Public;
            relObjectPrm.Name = accessorName.ToLower();
            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                relObjectPrm.Type = new CodeTypeReference(typePrm);
            }
            else
            {
                relObjectPrm.Type = new CodeTypeReference(relatedEntityType == null ? new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true) : relatedEntityType.GetTypeName(Settings));
            }
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                            new CodePropertyReferenceExpression(
                                new CodeThisReferenceExpression(),
                                "M2M"
                                ),
                            "Add",
                            new CodeArgumentReferenceExpression(relObjectPrm.Name)
                            );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeExpressionStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }

        private CodeMemberMethod CreateM2MRemoveMethod(string accessorName, EntityDefinition relatedEntity, TypeDefinition relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Remove" + accessorName;

            CodeParameterDeclarationExpression relObjectPrm = new CodeParameterDeclarationExpression();
            method.Parameters.Add(relObjectPrm);
            method.Attributes = MemberAttributes.Public;
            relObjectPrm.Name = accessorName.ToLower();
            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                relObjectPrm.Type = new CodeTypeReference(typePrm);
            }
            else
            {
                relObjectPrm.Type = new CodeTypeReference(relatedEntityType == null ? new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true) : relatedEntityType.GetTypeName(Settings));
            }
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                new CodePropertyReferenceExpression(
                    new CodeThisReferenceExpression(),
                    "M2M"
                    ),
                "Delete",
                new CodeArgumentReferenceExpression(relObjectPrm.Name)
                );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeExpressionStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }

        private CodeMemberMethod CreateM2MGetMethod(string accessorName, EntityDefinition relatedEntity, TypeDefinition relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Get" + WXMLCodeDomGeneratorNameHelper.GetMultipleForm(accessorName);
            method.Attributes = MemberAttributes.Public;
            CodeTypeReference callType;
            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                callType = new CodeTypeReference(typePrm);
            }
            else
            {
                callType = new CodeTypeReference(relatedEntityType == null ? new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true) : relatedEntityType.GetTypeName(Settings));
            }
            method.ReturnType = new CodeTypeReference(typeof(ReadOnlyList<>).FullName, callType);
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "M2M"
                                    ),
                                "Find",
                                callType),
                            new CodePrimitiveExpression(null),
                            new CodePrimitiveExpression(null),
                            new CodePrimitiveExpression(false)
                            );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }

        private CodeMemberMethod CreateM2MMergeMethod(string accessorName, EntityDefinition relatedEntity, TypeDefinition relatedEntityType, bool? direct)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "Merge" + WXMLCodeDomGeneratorNameHelper.GetMultipleForm(accessorName);
            method.Attributes = MemberAttributes.Public;

            CodeParameterDeclarationExpression colPrm = new CodeParameterDeclarationExpression();
            method.Parameters.Add(colPrm);
            method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(bool)), "removeNotInList"));

            colPrm.Name = "col";

            CodeTypeReference callType;

            if (relatedEntity.UseGenerics)
            {
                CodeTypeParameter typePrm = new CodeTypeParameter("T" + accessorName);
                typePrm.Constraints.Add(new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true)));
                typePrm.HasConstructorConstraint = true;
                method.TypeParameters.Add(typePrm);
                callType = new CodeTypeReference(typePrm);


            }
            else
            {
                callType = new CodeTypeReference(relatedEntityType == null ? new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(relatedEntity, true) : relatedEntityType.GetTypeName(Settings));
            }
            colPrm.Type = new CodeTypeReference(typeof(ReadOnlyList<>).FullName, callType);
            CodeMethodInvokeExpression methodInvokeExpression = new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeThisReferenceExpression(),
                                    "M2M"
                                    ),
                                "Merge",
                                callType),
                            new CodeArgumentReferenceExpression(colPrm.Name),
                            new CodeArgumentReferenceExpression("removeNotInList")
                            );
            if (direct.HasValue)
            {
                methodInvokeExpression.Parameters.Add(new CodePrimitiveExpression(direct.Value));
            }
            method.Statements.Add(
                new CodeExpressionStatement(
                    methodInvokeExpression
                    )
                );
            return method;
        }
        #endregion

        private void ImplementIRelation(RelationDefinition relation, EntityDefinition entity,
            CodeTypeDeclaration entitySchemaDefClass)
        {
            var leftProp = entity.GetActiveProperties().SingleOrDefault(match => match.FieldName == relation.Left.FieldName);
            var rightProp = entity.GetActiveProperties().SingleOrDefault(match => match.FieldName == relation.Right.FieldName);

            if (leftProp != null && rightProp != null)
            {
                entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));

                #region Pair<string, Type> GetFirstType()
                CodeMemberMethod method = new CodeMemberMethod();
                //method.StartDirectives.Add(Regions["IRelation Members"].Start);
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetFirstType";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IRelation));

                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(IRelation.RelationDesc)),
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, leftProp),
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
                #endregion Pair<string, Type> GetFirstType()

                #region Pair<string, Type> GetSecondType()
                method = new CodeMemberMethod();
                //method.EndDirectives.Add(Regions["IRelation Members"].End);
                entitySchemaDefClass.Members.Add(method);
                method.Name = "GetSecondType";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IRelation));
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(IRelation.RelationDesc)),
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, rightProp),
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
                    //new CodeTypeOfExpression(
                    //    new CodeTypeReference(GetEntityClassQualifiedName(relation.Left.Entity, settings))
                    //)
                            )
                        )
                    );
                #endregion Pair<string, Type> GetSecondType()
            }
        }

        private void ImplementIRelation(SelfRelationDescription relation, EntityDefinition entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            entitySchemaDefClass.BaseTypes.Add(new CodeTypeReference(typeof(IRelation)));
            #region Pair<string, Type> GetFirstType()
            CodeMemberMethod method = new CodeMemberMethod();
            //method.StartDirectives.Add(Regions["IRelation Members"].Start);
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetFirstType";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            // реализует метод базового класса
            method.ImplementationTypes.Add(typeof(IRelation));
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(IRelation.RelationDesc)),
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings,
                            entity.SelfProperties.First(
                                match => match.FieldName == relation.Direct.FieldName)),
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
            #endregion Pair<string, Type> GetFirstType()
            #region Pair<string, Type> GetSecondType()
            method = new CodeMemberMethod();
            //method.EndDirectives.Add(Regions["IRelation Members"].End);
            entitySchemaDefClass.Members.Add(method);
            method.Name = "GetSecondType";
            // тип возвращаемого значения
            method.ReturnType = new CodeTypeReference(typeof(IRelation.RelationDesc));
            // модификаторы доступа
            method.Attributes = MemberAttributes.Public;
            // реализует метод базового класса
            method.ImplementationTypes.Add(typeof(IRelation));
            method.Statements.Add(
                new CodeMethodReturnStatement(
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(IRelation.RelationDesc)),
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, entity.SelfProperties.First(
                            match => match.FieldName == relation.Reverse.FieldName)),
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
            #endregion Pair<string, Type> GetSecondType()
        }

        private static CodeMemberMethod CreateGetValueMethod(CodeEntityTypeDeclaration entityClass)
        {
            CodeMemberMethod method = new CodeMemberMethod();
            method.Name = "GetValueOptimized";
            method.ReturnType = new CodeTypeReference(typeof(object));
            method.Attributes = MemberAttributes.Public;
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

        private static void CreateChangeValueTypeMethod(EntityDefinition entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            CodeMemberMethod method;
            if (entity.Behaviour != EntityBehaviuor.PartialObjects && entity.BaseEntity == null)
            {
                method = new CodeMemberMethod();
                entitySchemaDefClass.Members.Add(method);
                method.Name = "ChangeValueType";
                // тип возвращаемого значения
                method.ReturnType = new CodeTypeReference(typeof(bool));
                // модификаторы доступа
                method.Attributes = MemberAttributes.Public;
                // реализует метод базового класса
                method.ImplementationTypes.Add(typeof(IEntitySchemaBase));
                // параметры
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(EntityPropertyAttribute)),
                        "c"
                        )
                    );
                method.Parameters.Add(
                    new CodeParameterDeclarationExpression(
                        new CodeTypeReference(typeof(object)),
                        "value"
                        )
                    );
                CodeParameterDeclarationExpression methodParam = new CodeParameterDeclarationExpression(
                    new CodeTypeReference(typeof(object)),
                    "newvalue"
                    );

                methodParam.Direction = FieldDirection.Ref;
                method.Parameters.Add(methodParam);
                method.Statements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeBinaryOperatorExpression(
                                new CodeBinaryOperatorExpression(
                                    new CodePropertyReferenceExpression(
                                        new CodeArgumentReferenceExpression("c"),
                                        "Behavior"
                                        ),
                                    CodeBinaryOperatorType.BitwiseAnd,
                                    new CodeFieldReferenceExpression(
                                        new CodeTypeReferenceExpression(typeof(Worm.Entities.Meta.Field2DbRelations)),
                                        "InsertDefault"
                                        )
                                    ),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodeFieldReferenceExpression(
                                    new CodeTypeReferenceExpression(typeof(Worm.Entities.Meta.Field2DbRelations)),
                                    "InsertDefault"
                                    )
                                ),
                            CodeBinaryOperatorType.BooleanAnd,
                            new CodeBinaryOperatorExpression(
                                new CodeBinaryOperatorExpression(
                                new CodeArgumentReferenceExpression("value"),
                                CodeBinaryOperatorType.IdentityEquality,
                                new CodePrimitiveExpression(null)
                                ),
                                CodeBinaryOperatorType.BooleanOr,
                                new CodeMethodInvokeExpression(
                                    new CodeMethodInvokeExpression(
                                        new CodeTypeReferenceExpression(typeof(Activator)),
                                        "CreateInstance",
                                        new CodeMethodInvokeExpression(
                                            new CodeArgumentReferenceExpression("value"),
                                            "GetType"
                                        )
                                    ),
                                    "Equals",
                                    new CodeArgumentReferenceExpression("value")
                                )

                            )
                            ),
                        new CodeAssignStatement(
                            new CodeArgumentReferenceExpression("newvalue"),
                            new CodeFieldReferenceExpression(
                                new CodeTypeReferenceExpression(typeof(DBNull)),
                                "Value"
                            )
                            ),
                        new CodeMethodReturnStatement(new CodePrimitiveExpression(true))
                        )
                    );
                // newvalue = value;
                method.Statements.Add(
                    new CodeAssignStatement(
                        new CodeArgumentReferenceExpression("newvalue"),
                        new CodeArgumentReferenceExpression("value")
                        )
                    );
                method.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodePrimitiveExpression(false)
                        )
                    );
            }
        }

        private void CreateProperties(EntityDefinition entity, CodeEntityTypeDeclaration entityClass,
            CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod,
            CodeTypeDeclaration propertiesClass, CodeTypeDeclaration fieldsClass,
            CodeTypeDeclaration propertyAliasClass, CodeTypeDeclaration instancedPropertyAliasClass)
        {
            foreach (PropertyDefinition pd in
                from k in entity.GetActiveProperties()
                select k)
            {
                PropertyDefinition propertyDesc = pd;

                #region создание проперти и etc

                FilterPropertyName(propertyDesc);

                var propertyNameField = Define.Field(typeof(string), 
                    MemberAttributes.Public | MemberAttributes.Const,
                    propertyDesc.PropertyAlias, () => propertyDesc.PropertyAlias);

                propertiesClass.Members.Add(propertyNameField);

                if (!string.IsNullOrEmpty(propertyDesc.Description))
                    WXMLCodeDomGenerator.SetMemberDescription(propertyNameField, propertyDesc.Description);
                
                if (propertyAliasClass != null && instancedPropertyAliasClass != null)
                {
                    var propertyAliasProperty = Define.GetProperty(typeof(ObjectProperty),
                        MemberAttributes.Public | MemberAttributes.Final,
                        propertyDesc.PropertyAlias,
                        Emit.@return(() => CodeDom.@new(typeof(ObjectProperty), CodeDom.@this,
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc))
                        )
                        //Emit.@return(() => new ObjectProperty(CodeDom.@this.cast<QueryAlias>(), 
                        //    WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc)
                        //))
                    );

                    propertyAliasClass.Members.Add(propertyAliasProperty);

                    if (!string.IsNullOrEmpty(propertyDesc.Description))
                        WXMLCodeDomGenerator.SetMemberDescription(propertyAliasProperty, propertyDesc.Description);

                    string privateMemberName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName("objectAlias");

                    var instancedPropertyAliasProperty = Define.GetProperty(typeof(ObjectProperty),
                        MemberAttributes.Public | MemberAttributes.Final,
                        propertyDesc.PropertyAlias,
                        Emit.@return(()=>CodeDom.@new(typeof(ObjectProperty),
                            CodeDom.Field(CodeDom.@this, privateMemberName),
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc)
                        ))
                    );

                    instancedPropertyAliasClass.Members.Add(instancedPropertyAliasProperty);

                    if (!string.IsNullOrEmpty(propertyDesc.Description))
                        WXMLCodeDomGenerator.SetMemberDescription(instancedPropertyAliasProperty, propertyDesc.Description);

                }

                if (fieldsClass != null)
                {
                    var propConst = Define.Field(typeof(ObjectProperty),
                        MemberAttributes.Private | MemberAttributes.Static | MemberAttributes.Final,
                        new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.PropertyAlias),
                        ()=>CodeDom.@new(typeof(ObjectProperty),
                            Settings.UseTypeInProps ? 
                                WXMLCodeDomGeneratorHelper.GetEntityClassTypeReferenceExpression(Settings, entity) : 
                                WXMLCodeDomGeneratorHelper.GetEntityNameReferenceExpression(Settings, entity),
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc)
                        )
                    );

                    fieldsClass.Members.Add(propConst);

                    CodeTypeReferenceExpression classProps = new CodeTypeReferenceExpression(
                        new WXMLCodeDomGeneratorNameHelper(Settings).GetEntityClassName(entity, true) + "." + fieldsClass.Name
                    );

                    var prop = Define.GetProperty(typeof(ObjectProperty),
                        MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final,
                        propertyDesc.PropertyAlias,
                        Emit.@return(()=>CodeDom.Field(classProps,propConst.Name))
                    );
                    
                    fieldsClass.Members.Add(prop);

                    if (!string.IsNullOrEmpty(propertyDesc.Description))
                        WXMLCodeDomGenerator.SetMemberDescription(prop, propertyDesc.Description);

                }

                #endregion создание проперти и etc

                CodeMemberProperty property = null;
                if (!propertyDesc.FromBase)
                    property = CreateProperty(entityClass, propertyDesc, entity, setvalueMethod, getvalueMethod);
                else
                {
                    TypeDefinition td = propertyDesc.NeedReplace();
                    if (td != null)
                    {
                        //CreateProperty(copyMethod, createobjectMethod, entityClass, propertyDesc, settings, setvalueMethod, getvalueMethod);
                        property = CreateUpdatedProperty(entityClass, propertyDesc, td.ToCodeType(Settings));
                    }
                }

                if (property != null)
                {
                    #region property custom attribute Worm.Orm.EntityPropertyAttribute

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
            PropertyDefinition propertyDesc, CodeTypeReference propertyType)
        {
            CodeMemberProperty property = new CodeMemberProperty();

            property.HasGet = true;
            property.HasSet = true;

            property.Name = propertyDesc.Name;
            property.Type = propertyType;
            property.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.New;

            property.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeCastExpression(
                        propertyType,
                        new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name)
                    )
                )
                );
            if (!propertyDesc.HasAttribute(WXML.Model.Field2DbRelations.ReadOnly))
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

            #region добавление членов в класс
            entityClass.Members.Add(property);
            #endregion добавление членов в класс

            return property;
        }

        private CodeMemberProperty CreateProperty(CodeEntityTypeDeclaration entityClass,
            PropertyDefinition propertyDesc, EntityDefinition entity,
            CodeMemberMethod setvalueMethod, CodeMemberMethod getvalueMethod)
        {
            CodeTypeReference fieldType = propertyDesc.PropertyType.ToCodeType(Settings);
            CodeMemberProperty property = null;

            if (string.IsNullOrEmpty(propertyDesc.FieldName) && entity.GetPropertiesFromBase().Any(item => !item.Disabled && item.Name == propertyDesc.Name))
            {
                property = Define.Property(fieldType,
                    WXMLCodeDomGenerator.GetMemberAttribute(propertyDesc) | MemberAttributes.New | MemberAttributes.Final,
                    propertyDesc.Name,
                    CodeDom.CombineStmts(
                        Emit.@return(()=>CodeDom.cast(fieldType, CodeDom.@base.Property(propertyDesc.Name)))
                    ),
                    Emit.assignProperty(new CodeBaseReferenceExpression(), propertyDesc.Name,
                        ()=>CodeDom.VarRef("value")
                    )
                );
            }
            else
            {
                string fieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.Name);

                CodeMemberField field = new CodeMemberField(fieldType, fieldName)
                {
                    Attributes = WXMLCodeDomGenerator.GetMemberAttribute(propertyDesc.FieldAccessLevel)
                };

                property = new CodeMemberProperty
                {
                    HasGet = true,
                    HasSet = true,
                    Name = propertyDesc.Name,
                    Type = fieldType,
                    Attributes = WXMLCodeDomGenerator.GetMemberAttribute(propertyDesc)
                };

                if (entity.GetPropertiesFromBase().Any(k => propertyDesc.Name == k.Name))
                {
                    property.Attributes |= MemberAttributes.Override;
                }

                #region property GetStatements

                CodeMethodInvokeExpression getUsingExpression = new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Read"),
                    WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc)
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

                if (entity.HasPkFlatEntity)
                    property.GetStatements.Add(new CodeUsingStatement(getUsingExpression, getInUsingStatements));
                else
                    property.GetStatements.AddRange(getInUsingStatements);

                #endregion property GetStatements

                #region property SetStatements

                if (_model.EnableReadOnlyPropertiesSetter ||
                    !propertyDesc.HasAttribute(WXML.Model.Field2DbRelations.ReadOnly) ||
                    propertyDesc.HasAttribute(WXML.Model.Field2DbRelations.PK))
                {
                    CodeExpression setUsingExpression = new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Write"),
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc)
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

                    if (entity.HasPkFlatEntity)
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

                WXMLCodeDomGenerator.Delegates.GetUpdateSetValueMethodMethod(Settings)(propertyDesc, setvalueMethod);

                #endregion void SetValue(System.Reflection.PropertyInfo pi, EntityPropertyAttribute c, object value)

                #region public override object GetValue(string propAlias, Worm.Orm.IOrmObjectsSchema schema)

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
                        WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc),
                        "Equals",
                        new CodeArgumentReferenceExpression("propertyAlias")
                    ),
                    new CodeMethodReturnStatement(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.Name))
                    )
                )
            );
        }
        private void CreatePropertyColumnAttribute(CodeMemberProperty property, PropertyDefinition propertyDesc)
        {
            CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(EntityPropertyAttribute)));

            if (!string.IsNullOrEmpty(propertyDesc.PropertyAlias))
            {
                declaration.Arguments.Add(
                    new CodeAttributeArgument("PropertyAlias", WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc)));
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

        private void CreateTablesLinkEnum(EntityDefinition entity, CodeTypeDeclaration entitySchemaDefClass)
        {
            if (!entity.InheritsBaseTables || entity.GetSourceFragments().Count() > 0)
            {
                var fullTables = entity.GetSourceFragments();

                CodeTypeDeclaration tablesEnum = new CodeTypeDeclaration("TablesLink");
                tablesEnum.Attributes = MemberAttributes.Public;
                if (entity.BaseEntity != null)
                    tablesEnum.Attributes |= MemberAttributes.New;

                tablesEnum.IsClass = false;
                tablesEnum.IsEnum = true;
                tablesEnum.IsPartial = false;
                int tableNum = 0;

                tablesEnum.Members.AddRange(fullTables.Select(tbl => new CodeMemberField
                {
                    InitExpression = new CodePrimitiveExpression(tableNum++),
                    Name = WXMLCodeDomGeneratorNameHelper.GetSafeName(tbl.Identifier)
                }).ToArray());
                entitySchemaDefClass.Members.Add(tablesEnum);
            }
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
