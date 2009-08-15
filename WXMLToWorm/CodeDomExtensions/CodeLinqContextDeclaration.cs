using System;
using System.CodeDom;
using System.Collections.Generic;
using WXML.Model.Descriptors;
using Worm;
using Worm.Database;
using WXML.Model;
using WXML.CodeDom;

namespace WXMLToWorm.CodeDomExtensions
{
    public class CodeLinqContextDeclaration : CodeTypeDeclaration
    {
        private WXMLCodeDomGeneratorSettings _settings;

        public CodeLinqContextDeclaration(WXMLCodeDomGeneratorSettings settings, LinqSettingsDescriptor linqSettings)
        {
            _settings = settings;
            PopulateMembers += OnPopulateMembers;
            PopulateBaseTypes += OnPopulateBaseTypes;
            LinqSettings = linqSettings;
            if (ContextClassBehaviour == ContextClassBehaviourType.PartialClass || ContextClassBehaviour == ContextClassBehaviourType.BasePartialClass)
            {
                IsPartial = true;
            }

            Name = !String.IsNullOrEmpty(LinqSettings.ContextName) ? LinqSettings.ContextName : "LinqContext";
        }

        protected LinqSettingsDescriptor LinqSettings
        {
            get;
            private set;
        }

        protected virtual void OnPopulateBaseTypes(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(LinqSettings.BaseContext))
            {
                BaseTypes.Add(LinqSettings.BaseContext);
            }
            else
            {
                BaseTypes.Add(new CodeTypeReference("Worm.Linq.WormLinqContext"));
            }
        }

        protected virtual void OnPopulateMembers(object sender, EventArgs e)
        {
            foreach (var entityDescription in m_entities)
            {
                Members.Add(new CodeContextEntityWraperMember(_settings, entityDescription));
            }
            if(ContextClassBehaviour == ContextClassBehaviourType.BaseClass || ContextClassBehaviour == ContextClassBehaviourType.BasePartialClass)
            {
                var ctor = new CodeConstructor {Attributes = MemberAttributes.Public};
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "conn"));
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Worm.Cache.OrmCache)), "cache"));
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(ObjectMappingEngine)), "schema"));
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof (SQLGenerator)), "gen"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("conn"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("cache"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("schema"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("gen"));
                Members.Add(ctor);
                ctor = new CodeConstructor {Attributes = MemberAttributes.Public};
                ctor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(string)), "conn"));
                ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("conn"));
                Members.Add(ctor);
            }
        }

        public ContextClassBehaviourType ContextClassBehaviour
        {
            get
            {
                return LinqSettings.ContextClassBehaviour ?? ContextClassBehaviourType.BaseClass;
            }
        }

        public List<EntityDefinition> Entities
        {
            get { return m_entities; }
        }

        private readonly List<EntityDefinition> m_entities = new List<EntityDefinition>();
    }

    public class CodeContextEntityWraperMember : CodeMemberProperty
    {
        private readonly EntityDefinition m_entity;
        private WXMLCodeDomGeneratorSettings _settings;

        public CodeContextEntityWraperMember(WXMLCodeDomGeneratorSettings settings, EntityDefinition entity)
        {
            _settings = settings;
            m_entity = entity;
            string entityName = entity.Name;
            if (entityName.EndsWith("s"))
                entityName += "es";
            else if(entityName.EndsWith("y"))
                entityName += "ies";
            else
                entityName += "s";
            Attributes = MemberAttributes.Public | MemberAttributes.Final;
            Name = (entity.RawNamespace ?? string.Empty) + entityName;
            var entityTypeReference = new CodeTypeReference(new WXMLCodeDomGeneratorNameHelper(_settings).GetEntityClassName(Entity, true));
            Type = new CodeTypeReference("Worm.Linq.QueryWrapperT", entityTypeReference);
            SetStatements.Clear();
            GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeThisReferenceExpression(), "CreateQueryWrapper", entityTypeReference)
                        )
                    )
                );
        }

        public EntityDefinition Entity
        {
            get
            {
                return m_entity;
            }
        }
    }
}
