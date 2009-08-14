using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using WXML.Model.Descriptors;

namespace WXML.Model
{
    internal class WXMLModelReader
    {
        private const string SCHEMA_NAME = "OrmObjectsSchema";

        private readonly List<string> _validationResult;
        private readonly XmlReader _reader;
        private XmlDocument _ormXmlDocument;
        private readonly WXMLModel _ormObjectsDef;

        private readonly XmlNamespaceManager _nsMgr;
        private readonly XmlNameTable _nametable;

        private readonly XmlResolver _xmlResolver;

        internal protected WXMLModelReader(XmlReader reader) : this(reader, null)
        {
            
        }

        internal protected WXMLModelReader(XmlReader reader, XmlResolver xmlResolver)
        {
            _validationResult = new List<string>();
            _reader = reader;
            _ormObjectsDef = new WXMLModel();
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(WXMLModel.NS_PREFIX, WXMLModel.NS_URI);
            _xmlResolver = xmlResolver;
        }

        internal protected WXMLModelReader(XmlDocument document)
        {
            _ormObjectsDef = new WXMLModel();
            _ormXmlDocument = document;
            _nametable = document.NameTable;
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(WXMLModel.NS_PREFIX, WXMLModel.NS_URI);            
        }

        internal protected static WXMLModel Parse(XmlReader reader, XmlResolver xmlResolver)
        {
            WXMLModelReader parser;
            parser = new WXMLModelReader(reader, xmlResolver);

            parser.Read();

            parser.FillObjectsDef();

            return parser._ormObjectsDef;
        }

        internal protected static WXMLModel LoadXmlDocument(XmlDocument document, bool skipValidation)
        {
            WXMLModelReader parser;
            if (skipValidation)
                parser = new WXMLModelReader(document);
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter xwr = XmlWriter.Create(ms))
                    {
                        document.WriteTo(xwr);
                    }
                    ms.Position = 0;
                    using (XmlReader xrd = XmlReader.Create(ms))
                    {
                        parser = new WXMLModelReader(xrd, null);
                        parser.Read();
                    }
                }
            }
            parser.FillObjectsDef();
            return parser._ormObjectsDef;                
        }

        private void FillObjectsDef()
        {
            FillFileDescriptions();

            FillLinqSettings();

            FillImports();

        	FillSourceFragments();

            FindEntities();

            FillTypes();

            FillEntities();

            FillRelations();

            FillExtensions();
        }

        private void FillExtensions()
        {
            var extensionsNode =
                (XmlElement)_ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:extensions", WXMLModel.NS_PREFIX), _nsMgr);

            if (extensionsNode == null)
                return;

            foreach (XmlElement extension in extensionsNode.ChildNodes)
            {
                FillExtension(Model.Extensions, extension);
            }
        }

        private void FillExtension(Dictionary<string, XmlDocument> dictionary, XmlElement extension)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(extension.InnerXml);
            dictionary.Add(extension.Attributes["name"].Value, xdoc);
        }

        private void FillLinqSettings()
        {
            var settingsNode =
                (XmlElement)_ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Linq", WXMLModel.NS_PREFIX),_nsMgr);

            if (settingsNode == null)
                return;

            _ormObjectsDef.LinqSettings = new LinqSettingsDescriptor();

            _ormObjectsDef.LinqSettings.Enable = XmlConvert.ToBoolean(settingsNode.GetAttribute("enable"));

            _ormObjectsDef.LinqSettings.ContextName = settingsNode.GetAttribute("contextName");
            _ormObjectsDef.LinqSettings.FileName = settingsNode.GetAttribute("filename");

            string behaviourValue = settingsNode.GetAttribute("contextClassBehaviour");
            if(!string.IsNullOrEmpty(behaviourValue))
            {
                var type =
                    (ContextClassBehaviourType) Enum.Parse(typeof (ContextClassBehaviourType), behaviourValue);
                _ormObjectsDef.LinqSettings.ContextClassBehaviour = type;
            }
        }

        private void FillImports()
        {
            XmlNodeList importNodes;
            importNodes =
                _ormXmlDocument.DocumentElement.SelectNodes(
                    string.Format("{0}:Includes/{0}:WXMLModel", WXMLModel.NS_PREFIX), _nsMgr);

            foreach (XmlNode importNode in importNodes)
            {
                WXMLModel import;

                XmlDocument tempDoc = new XmlDocument();
                XmlNode importedNode = tempDoc.ImportNode(importNode, true);
                tempDoc.AppendChild(importedNode);
                import = LoadXmlDocument(tempDoc, true);

                Model.Includes.Add(import);
            }
        }

        internal protected void FillTypes()
        {
            XmlNodeList typeNodes;
            typeNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Types/{0}:Type", WXMLModel.NS_PREFIX), _nsMgr);

            foreach (XmlNode typeNode in typeNodes)
            {
                TypeDefinition type;
                string id;
                XmlElement typeElement = (XmlElement)typeNode;
                id = typeElement.GetAttribute("id");
                
                XmlNode typeDefNode = typeNode.LastChild;
                XmlElement typeDefElement = (XmlElement)typeDefNode;
                if(typeDefNode.LocalName.Equals("Entity"))
                {
                    string entityId;
                    entityId = typeDefElement.GetAttribute("ref");
                    EntityDefinition entity = _ormObjectsDef.GetEntity(entityId);
                    if (entity == null)
                        throw new KeyNotFoundException(
                            string.Format("Underlying entity '{1}' in type '{0}' not found.", id, entityId));
                    type = new TypeDefinition(id, entity);
                }
                else
                {
                    string name = typeDefElement.GetAttribute("name");
                    if (typeDefNode.LocalName.Equals("UserType"))
                    {
                        UserTypeHintFlags? hint = null;
                        XmlAttribute hintAttribute = typeDefNode.Attributes["hint"];
                        if (hintAttribute != null)
                            hint = (UserTypeHintFlags) Enum.Parse(typeof (UserTypeHintFlags), hintAttribute.Value.Replace(" ", ", "));
                        type = new TypeDefinition(id, name, hint);
                    }
                    else
                    {
                        type = new TypeDefinition(id, name, false);
                    }
                }
                _ormObjectsDef.Types.Add(type);
            }
        }

        internal protected void FillEntities()
        {
            foreach (EntityDefinition entity in _ormObjectsDef.Entities)
            {
                XmlNode entityNode =
                    _ormXmlDocument.DocumentElement.SelectSingleNode(
                        string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX,
                                      entity.Identifier), _nsMgr);

                XmlElement entityElement = (XmlElement)entityNode;
                string baseEntityId = entityElement.GetAttribute("baseEntity");

                if (!string.IsNullOrEmpty(baseEntityId))
                {
                    EntityDefinition baseEntity = Model.GetEntity(baseEntityId);
                    if (baseEntity == null)
                        throw new OrmXmlParserException(
                            string.Format("Base entity '{0}' for entity '{1}' not found.", baseEntityId,
                                          entity.Identifier));
                    entity.BaseEntity = baseEntity;
                }
                FillProperties(entity);
                FillSuppresedProperties(entity);
                FillEntityRelations(entity);

                var extensionsNode =
                    entityNode.SelectNodes(string.Format("{0}:extension", WXMLModel.NS_PREFIX), _nsMgr);

                if (extensionsNode == null)
                    return;

                foreach (XmlElement extension in extensionsNode)
                {
                    FillExtension(entity.Extensions, extension);
                }
            }
        }

        private void FillEntityRelations(EntityDefinition entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            XmlNode entityNode;
            entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX, entity.Identifier), _nsMgr);

            XmlNodeList relationsList;
            relationsList = entityNode.SelectNodes(
                string.Format("{0}:Relations/{0}:Relation", WXMLModel.NS_PREFIX), _nsMgr);

            foreach(XmlElement relationNode in relationsList)
            {
                string entityId = relationNode.GetAttribute("entity");

                var relationEntity = _ormObjectsDef.GetEntity(entityId);

                string propertyAlias = relationNode.GetAttribute("propertyAlias");

                string name = relationNode.GetAttribute("name");

                string accessorName = relationNode.GetAttribute("accessorName");

                string disabledAttribute = relationNode.GetAttribute("disabled");

                bool disabled = string.IsNullOrEmpty(disabledAttribute)
                                    ? false
                                    : XmlConvert.ToBoolean(disabledAttribute);

                string accessorDescription = relationNode.GetAttribute("accessorDescription");

                EntityRelationDefinition relation = new EntityRelationDefinition
                                                         {
                                                             SourceEntity = entity,
                                                             Entity = relationEntity,
                                                             PropertyAlias = propertyAlias,
                                                             Name = name,
                                                             AccessorName = accessorName,
                                                             Disabled = disabled,
                                                             AccessorDescription = accessorDescription
                                                         };
                entity.EntityRelations.Add(relation);
            }
        }

        private void FillSuppresedProperties(EntityDefinition entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            XmlNode entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX, entity.Identifier), _nsMgr);

            XmlNodeList propertiesList = entityNode.SelectNodes(string.Format("{0}:SuppressedProperties/{0}:Property", WXMLModel.NS_PREFIX), _nsMgr);

            foreach (XmlNode propertyNode in propertiesList)
            {
                XmlElement propertyElement = (XmlElement) propertyNode;
                string name = propertyElement.GetAttribute("name");

                //PropertyDescription property = new PropertyDescription(name);

                entity.SuppressedProperties.Add(name);
            }
        }

        internal protected void FillFileDescriptions()
        {
            _ormObjectsDef.Namespace = _ormXmlDocument.DocumentElement.GetAttribute("defaultNamespace");
            _ormObjectsDef.SchemaVersion = _ormXmlDocument.DocumentElement.GetAttribute("schemaVersion");
        	_ormObjectsDef.EntityBaseTypeName = _ormXmlDocument.DocumentElement.GetAttribute("entityBaseType");

            string generateEntityName = _ormXmlDocument.DocumentElement.GetAttribute("generateEntityName");            
            _ormObjectsDef.GenerateEntityName = string.IsNullOrEmpty(generateEntityName) ? true : XmlConvert.ToBoolean(generateEntityName);

            string baseUriString = _ormXmlDocument.DocumentElement.GetAttribute("xml:base");
            if (!string.IsNullOrEmpty(baseUriString))
            {
                Uri baseUri = new Uri(baseUriString, UriKind.RelativeOrAbsolute);
                _ormObjectsDef.FileName = Path.GetFileName(baseUri.ToString());
            }
        	
            string enableCommonPropertyChangedFireAttr =
        		_ormXmlDocument.DocumentElement.GetAttribute("enableCommonPropertyChangedFire");
			
            if (!string.IsNullOrEmpty(enableCommonPropertyChangedFireAttr))
				_ormObjectsDef.EnableCommonPropertyChangedFire = XmlConvert.ToBoolean(enableCommonPropertyChangedFireAttr);

            string schemaOnly = _ormXmlDocument.DocumentElement.GetAttribute("generateSchemaOnly");
            if (!string.IsNullOrEmpty(schemaOnly))
                _ormObjectsDef.GenerateSchemaOnly = XmlConvert.ToBoolean(schemaOnly);

            string addVersionToSchemaName = _ormXmlDocument.DocumentElement.GetAttribute("addVersionToSchemaName");
            if (!string.IsNullOrEmpty(addVersionToSchemaName))
                _ormObjectsDef.AddVersionToSchemaName = XmlConvert.ToBoolean(addVersionToSchemaName);

            string singleFile = _ormXmlDocument.DocumentElement.GetAttribute("singleFile");
            if (!string.IsNullOrEmpty(singleFile))
                _ormObjectsDef.GenerateSingleFile = XmlConvert.ToBoolean(singleFile);
        }

        internal protected void FindEntities()
        {
            XmlNodeList entitiesList = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Entities/{0}:Entity", WXMLModel.NS_PREFIX), _nsMgr);

            _ormObjectsDef.ClearEntities();

            foreach (XmlNode entityNode in entitiesList)
            {
                EntityBehaviuor behaviour = EntityBehaviuor.ForcePartial;

                XmlElement entityElement = (XmlElement) entityNode;
                string id = entityElement.GetAttribute("id");
                string name = entityElement.GetAttribute("name");
                string description = entityElement.GetAttribute("description");
                string nameSpace = entityElement.GetAttribute("namespace");
                string behaviourName = entityElement.GetAttribute("behaviour");

            	string useGenericsAttribute = entityElement.GetAttribute("useGenerics");
            	string makeInterfaceAttribute = entityElement.GetAttribute("makeInterface");
            	string disbledAttribute = entityElement.GetAttribute("disabled");
				string cacheCheckRequiredAttribute = entityElement.GetAttribute("cacheCheckRequired");

				bool useGenerics = !string.IsNullOrEmpty(useGenericsAttribute) && XmlConvert.ToBoolean(useGenericsAttribute);
            	bool makeInterface = !string.IsNullOrEmpty(makeInterfaceAttribute) &&
            	                     XmlConvert.ToBoolean(makeInterfaceAttribute);
            	bool disabled = !string.IsNullOrEmpty(disbledAttribute) && XmlConvert.ToBoolean(disbledAttribute);
            	bool cacheCheckRequired = !string.IsNullOrEmpty(cacheCheckRequiredAttribute) &&
            	                          XmlConvert.ToBoolean(cacheCheckRequiredAttribute);

                if (!string.IsNullOrEmpty(behaviourName))
                    behaviour = (EntityBehaviuor) Enum.Parse(typeof (EntityBehaviuor), behaviourName);


                EntityDefinition entity = new EntityDefinition(id, name, nameSpace, description, _ormObjectsDef);

				entity.Behaviour = behaviour;
            	entity.UseGenerics = useGenerics;
            	entity.MakeInterface = makeInterface;
            	entity.Disabled = disabled;
            	entity.CacheCheckRequired = cacheCheckRequired;

                //_ormObjectsDef.AddEntity(entity);

                FillEntityTables(entity);
            }
        }

        internal protected void FillProperties(EntityDefinition entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            XmlNode entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX, entity.Identifier), _nsMgr);

            XmlNodeList propertiesList = entityNode.SelectNodes(string.Format("{0}:Properties/{0}:Property", WXMLModel.NS_PREFIX), _nsMgr);

            FillEntityProperties(entity, propertiesList, null);

            XmlNodeList groupsNodeList =
                entityNode.SelectNodes(string.Format("{0}:Properties/{0}:Group", WXMLModel.NS_PREFIX), _nsMgr);
            foreach (XmlElement groupNode in groupsNodeList)
            {
                string hideValue = groupNode.GetAttribute("hide");
                bool hide = string.IsNullOrEmpty(hideValue) ? true : XmlConvert.ToBoolean(hideValue);
                PropertyGroup group = new PropertyGroup
                                          {
                                              Name = groupNode.GetAttribute("name"),
                                              Hide =  hide
                                          };
                propertiesList = groupNode.SelectNodes(string.Format("{0}:Property", WXMLModel.NS_PREFIX), _nsMgr);
                FillEntityProperties(entity, propertiesList, group);
            }
        }

        private void FillEntityProperties(EntityDefinition entity, XmlNodeList propertiesList, PropertyGroup group)
        {
            foreach (XmlNode propertyNode in propertiesList)
            {
                AccessLevel fieldAccessLevel, propertyAccessLevel;
                bool disabled = false, enablePropertyChanged = false;
                ObsoleteType obsolete;

                XmlElement propertyElement = (XmlElement) propertyNode;
                string description = propertyElement.GetAttribute("description");
                string name = propertyElement.GetAttribute("propertyName");
                string fieldname = propertyElement.GetAttribute("fieldName");
                string typeId = propertyElement.GetAttribute("typeRef");
                string sAttributes = propertyElement.GetAttribute("attributes");
                string tableId = propertyElement.GetAttribute("table");
                string fieldAccessLevelName = propertyElement.GetAttribute("classfieldAccessLevel");
                string propertyAccessLevelName = propertyElement.GetAttribute("propertyAccessLevel");
                string propertyAlias = propertyElement.GetAttribute("propertyAlias");
                string propertyDisabled = propertyElement.GetAttribute("disabled");
                string propertyObsolete = propertyElement.GetAttribute("obsolete");
                string propertyObsoleteDescription = propertyElement.GetAttribute("obsoleteDescription");
                string enablePropertyChangedAttribute = propertyElement.GetAttribute("enablePropertyChanged");
                string fieldAlias = propertyElement.GetAttribute("fieldAlias");

                string dbTypeNameAttribute = propertyElement.GetAttribute("dbTypeName");
                string dbTypeSizeAttribute = propertyElement.GetAttribute("dbTypeSize");
                string dbTypeNullableAttribute = propertyElement.GetAttribute("dbTypeNullable");

            	string defferedLoadGroup = propertyElement.GetAttribute("defferedLoadGroup");

                //string[] attributes = sAttributes.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                Field2DbRelations attributes = Field2DbRelations.None;
                if (!string.IsNullOrEmpty(sAttributes))
                    attributes = (Field2DbRelations)Enum.Parse(typeof(Field2DbRelations), sAttributes);

                if (!string.IsNullOrEmpty(propertyAccessLevelName))
                    propertyAccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), propertyAccessLevelName);
                else
                    propertyAccessLevel = AccessLevel.Public;

                if (!string.IsNullOrEmpty(fieldAccessLevelName))
                    fieldAccessLevel = (AccessLevel)Enum.Parse(typeof(AccessLevel), fieldAccessLevelName);
                else
                    fieldAccessLevel = AccessLevel.Private;

                SourceFragmentDefinition table = entity.GetSourceFragment(tableId);

                if (!String.IsNullOrEmpty(propertyDisabled))
                    disabled = XmlConvert.ToBoolean(propertyDisabled);

                //if (table == null)
                //    throw new OrmXmlParserException(
                //        string.Format("SourceFragment '{0}' for property '{1}' of entity '{2}' not found.", tableId, name,
                //                      entity.Identifier));

                TypeDefinition typeDesc = _ormObjectsDef.GetType(typeId, true);
                
                if(!string.IsNullOrEmpty(propertyObsolete))
                {
                    obsolete = (ObsoleteType) Enum.Parse(typeof (ObsoleteType), propertyObsolete);
                }
                else
                {
                    obsolete = ObsoleteType.None;
                }

                if (!string.IsNullOrEmpty(enablePropertyChangedAttribute))
                    enablePropertyChanged = XmlConvert.ToBoolean(enablePropertyChangedAttribute);

                PropertyDefinition property = new PropertyDefinition(entity, name, propertyAlias, attributes, description, typeDesc, fieldname, table, fieldAccessLevel, propertyAccessLevel)
                {
                    Disabled = disabled,
                    Obsolete = obsolete,
                    ObsoleteDescripton = propertyObsoleteDescription,
                    EnablePropertyChanged = enablePropertyChanged,
                    Group = group,
                    FieldAlias = fieldAlias,
                    DbTypeName = dbTypeNameAttribute
                };

                if (!string.IsNullOrEmpty(dbTypeSizeAttribute))
                    property.DbTypeSize = XmlConvert.ToInt32(dbTypeSizeAttribute);
                if (!string.IsNullOrEmpty(dbTypeNullableAttribute))
                    property.DbTypeNullable = XmlConvert.ToBoolean(dbTypeNullableAttribute);
            	property.DefferedLoadGroup = defferedLoadGroup;

                entity.AddProperty(property);
            }
        }

        internal protected void FillRelations()
        {
			XmlNodeList relationNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:EntityRelations/{0}:Relation", WXMLModel.NS_PREFIX), _nsMgr);

            #region Relations
            foreach (XmlNode relationNode in relationNodes)
			{
				XmlNode leftTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Left", WXMLModel.NS_PREFIX), _nsMgr);
				XmlNode rightTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Right", WXMLModel.NS_PREFIX), _nsMgr);

				XmlElement relationElement = (XmlElement)relationNode;
				string relationTableId = relationElement.GetAttribute("table");
				string underlyingEntityId = relationElement.GetAttribute("underlyingEntity");
				string disabledValue = relationElement.GetAttribute("disabled");

				XmlElement leftTargetElement = (XmlElement)leftTargetNode;
				string leftLinkTargetEntityId = leftTargetElement.GetAttribute("entity");
				XmlElement rightTargetElement = (XmlElement)rightTargetNode;
				string rightLinkTargetEntityId = rightTargetElement.GetAttribute("entity");

				string leftFieldName = leftTargetElement.GetAttribute("fieldName");
				string rightFieldName = rightTargetElement.GetAttribute("fieldName");

				bool leftCascadeDelete = XmlConvert.ToBoolean(leftTargetElement.GetAttribute("cascadeDelete"));
				bool rightCascadeDelete = XmlConvert.ToBoolean(rightTargetElement.GetAttribute("cascadeDelete"));

				string leftAccessorName = leftTargetElement.GetAttribute("accessorName");
				string rightAccessorName = rightTargetElement.GetAttribute("accessorName");

				string leftAccessedEntityTypeId = leftTargetElement.GetAttribute("accessedEntityType");
				string rightAccessedEntityTypeId = rightTargetElement.GetAttribute("accessedEntityType");

                string leftAccessorDescription = leftTargetElement.GetAttribute("accessorDescription");
                string rightAccessorDescription = rightTargetElement.GetAttribute("accessorDescription");

				TypeDefinition leftAccessedEntityType = _ormObjectsDef.GetType(leftAccessedEntityTypeId, true);
				TypeDefinition rightAccessedEntityType = _ormObjectsDef.GetType(rightAccessedEntityTypeId, true);

				SourceFragmentDefinition relationTable = _ormObjectsDef.GetSourceFragment(relationTableId);

				EntityDefinition underlyingEntity;
				if (string.IsNullOrEmpty(underlyingEntityId))
					underlyingEntity = null;
				else
					underlyingEntity = _ormObjectsDef.GetEntity(underlyingEntityId);

				bool disabled;
				if (string.IsNullOrEmpty(disabledValue))
					disabled = false;
				else
					disabled = XmlConvert.ToBoolean(disabledValue);


				EntityDefinition leftLinkTargetEntity = _ormObjectsDef.GetEntity(leftLinkTargetEntityId);

				EntityDefinition rightLinkTargetEntity = _ormObjectsDef.GetEntity(rightLinkTargetEntityId);

                LinkTarget leftLinkTarget = new LinkTarget(leftLinkTargetEntity, leftFieldName, leftCascadeDelete, leftAccessorName) 
                { 
                    AccessorDescription = leftAccessorDescription,
                    AccessedEntityType = leftAccessedEntityType
                };

                LinkTarget rightLinkTarget = new LinkTarget(rightLinkTargetEntity, rightFieldName, rightCascadeDelete, rightAccessorName) 
                { 
                    AccessorDescription = rightAccessorDescription,
                    AccessedEntityType = rightAccessedEntityType
                };

				RelationDefinition relation = new RelationDefinition(leftLinkTarget, rightLinkTarget, relationTable, underlyingEntity, disabled);
				_ormObjectsDef.Relations.Add(relation);

			    XmlNodeList constantsNodeList =
			        relationNode.SelectNodes(string.Format("{0}:Constants/{0}:Constant", WXMLModel.NS_PREFIX), _nsMgr);

			    foreach (XmlElement constantNode in constantsNodeList)
			    {
			        string name = constantNode.GetAttribute("name");
			        string value = constantNode.GetAttribute("value");

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                        continue;

			        RelationConstantDescriptor con = new RelationConstantDescriptor
			                                             {
			                                                 Name = name,
			                                                 Value = value
			                                             };

			        relation.Constants.Add(con);
			    }
			} 
			#endregion
			#region SelfRelations
			relationNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:EntityRelations/{0}:SelfRelation", WXMLModel.NS_PREFIX), _nsMgr);

			foreach (XmlNode relationNode in relationNodes)
			{
				XmlNode directTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Direct", WXMLModel.NS_PREFIX), _nsMgr);
				XmlNode reverseTargetNode = relationNode.SelectSingleNode(string.Format("{0}:Reverse", WXMLModel.NS_PREFIX), _nsMgr);

				XmlElement relationElement = (XmlElement)relationNode;
				string relationTableId = relationElement.GetAttribute("table");
				string underlyingEntityId = relationElement.GetAttribute("underlyingEntity");
				string disabledValue = relationElement.GetAttribute("disabled");
				string entityId = relationElement.GetAttribute("entity");

				XmlElement directTargetElement = (XmlElement)directTargetNode;
				XmlElement reverseTargetElement = (XmlElement)reverseTargetNode;

				string directFieldName = directTargetElement.GetAttribute("fieldName");
				string reverseFieldName = reverseTargetElement.GetAttribute("fieldName");

				bool directCascadeDelete = XmlConvert.ToBoolean(directTargetElement.GetAttribute("cascadeDelete"));
				bool reverseCascadeDelete = XmlConvert.ToBoolean(reverseTargetElement.GetAttribute("cascadeDelete"));

				string directAccessorName = directTargetElement.GetAttribute("accessorName");
				string reverseAccessorName = reverseTargetElement.GetAttribute("accessorName");

				string directAccessedEntityTypeId = directTargetElement.GetAttribute("accessedEntityType");
				string reverseAccessedEntityTypeId = reverseTargetElement.GetAttribute("accessedEntityType");

                string directAccessorDescription = directTargetElement.GetAttribute("accessorDescription");
                string reverseAccessorDescription = reverseTargetElement.GetAttribute("accessorDescription");
                
                TypeDefinition directAccessedEntityType = _ormObjectsDef.GetType(directAccessedEntityTypeId, true);
				TypeDefinition reverseAccessedEntityType = _ormObjectsDef.GetType(reverseAccessedEntityTypeId, true);

				var relationTable = _ormObjectsDef.GetSourceFragment(relationTableId);

				EntityDefinition underlyingEntity;
				if (string.IsNullOrEmpty(underlyingEntityId))
					underlyingEntity = null;
				else
					underlyingEntity = _ormObjectsDef.GetEntity(underlyingEntityId);

				bool disabled;
				if (string.IsNullOrEmpty(disabledValue))
					disabled = false;
				else
					disabled = XmlConvert.ToBoolean(disabledValue);

				EntityDefinition entity = _ormObjectsDef.GetEntity(entityId);

                SelfRelationTarget directTarget = new SelfRelationTarget(directFieldName, directCascadeDelete, directAccessorName)
                {
                    AccessorDescription = directAccessorDescription,
                    AccessedEntityType = directAccessedEntityType
                };

                SelfRelationTarget reverseTarget = new SelfRelationTarget(reverseFieldName, reverseCascadeDelete, reverseAccessorName)
                {
                    AccessorDescription = reverseAccessorDescription,
                    AccessedEntityType = reverseAccessedEntityType
                };

				SelfRelationDescription relation = new SelfRelationDescription(entity, directTarget, reverseTarget, relationTable, underlyingEntity, disabled);
				_ormObjectsDef.Relations.Add(relation);
			}
			#endregion
        }

		internal protected void FillSourceFragments()
		{
			var sourceFragmentNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:SourceFragments/{0}:SourceFragment", WXMLModel.NS_PREFIX), _nsMgr);

			foreach (XmlNode tableNode in sourceFragmentNodes)
			{
				XmlElement tableElement = (XmlElement)tableNode;
				string id = tableElement.GetAttribute("id");
				string name = tableElement.GetAttribute("name");
				string selector = tableElement.GetAttribute("selector");

				_ormObjectsDef.SourceFragments.Add(new SourceFragmentDefinition(id, name, selector));
			}
		}

        internal protected void FillEntityTables(EntityDefinition entity)
        {
            XmlNodeList tableNodes;
            XmlNode entityNode;

            if (entity == null)
                throw new ArgumentNullException("entity");

            entity.ClearSourceFragments();

            entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX, entity.Identifier), _nsMgr);

            tableNodes = entityNode.SelectNodes(string.Format("{0}:SourceFragments/{0}:SourceFragment", WXMLModel.NS_PREFIX), _nsMgr);

            foreach (XmlNode tableNode in tableNodes)
            {
                XmlElement tableElement = (XmlElement)tableNode;
                string tableId = tableElement.GetAttribute("ref");
                var table = entity.Model.GetSourceFragment(tableId);
                if (table == null)
                    throw new OrmXmlParserException(String.Format("Table {0} not found.", tableId));

                var tableRef = new SourceFragmentRefDefinition(table);

                string anchorId = tableElement.GetAttribute("anchorTableRef");
                if (!string.IsNullOrEmpty(anchorId))
                {
                    tableRef.AnchorTable = entity.Model.GetSourceFragment(anchorId);
                    string jt = tableElement.GetAttribute("joinType");
                    if (string.IsNullOrEmpty(jt))
                        jt = "inner";
                    tableRef.JoinType = (SourceFragmentRefDefinition.JoinTypeEnum)Enum.Parse(typeof(SourceFragmentRefDefinition.JoinTypeEnum), jt);
                    var joinNodes = tableElement.SelectNodes(string.Format("{0}:join", WXMLModel.NS_PREFIX), _nsMgr);
                    foreach (XmlElement joinNode in joinNodes)
                    {
                        tableRef.Conditions.Add(new SourceFragmentRefDefinition.Condition(
                            joinNode.GetAttribute("refColumn"),
                            joinNode.GetAttribute("anchorColumn")
                        ));
                    }
                }

                entity.AddSourceFragment(tableRef);
            }

            XmlNode tablesNode = entityNode.SelectSingleNode(string.Format("{0}:SourceFragments", WXMLModel.NS_PREFIX), _nsMgr);
            string inheritsTablesValue = ((XmlElement)tablesNode).GetAttribute("inheritsBase");
            entity.InheritsBaseTables = string.IsNullOrEmpty(inheritsTablesValue) || XmlConvert.ToBoolean(inheritsTablesValue);
        }

        internal protected void Read()
        {
            XmlSchemaSet schemaSet;
            XmlSchema schema;
            XmlReaderSettings xmlReaderSettings;

            schemaSet = new XmlSchemaSet(_nametable);

            schema = ResourceManager.GetXmlSchema("XInclude");
            schemaSet.Add(schema);
            schema = ResourceManager.GetXmlSchema(SCHEMA_NAME);
            schemaSet.Add(schema);
            

            xmlReaderSettings = new XmlReaderSettings();
            xmlReaderSettings.CloseInput = false;
            xmlReaderSettings.ConformanceLevel = ConformanceLevel.Document;
            xmlReaderSettings.IgnoreComments = true;
            xmlReaderSettings.IgnoreWhitespace = true;
            xmlReaderSettings.Schemas = schemaSet;
            xmlReaderSettings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.AllowXmlAttributes | XmlSchemaValidationFlags.ProcessIdentityConstraints;
            xmlReaderSettings.ValidationType = ValidationType.Schema;
            xmlReaderSettings.ValidationEventHandler += xmlReaderSettings_ValidationEventHandler;

            XmlDocument xml;
            xml = new XmlDocument(_nametable);

            _validationResult.Clear();

            XmlDocument tDoc = new XmlDocument();
            using (Mvp.Xml.XInclude.XIncludingReader rdr = new Mvp.Xml.XInclude.XIncludingReader(_reader))
            {
                rdr.XmlResolver = _xmlResolver;
                tDoc.Load(rdr);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter wr = XmlWriter.Create(ms))
                {
                    tDoc.WriteTo(wr);
                }
                ms.Position = 0;
                using (XmlReader rdr = XmlReader.Create(ms, xmlReaderSettings))
                {
                    xml.Load(rdr);
                }
            }

            if (_validationResult.Count == 0)
                _ormXmlDocument = xml;
        }

        void xmlReaderSettings_ValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if(e.Severity == XmlSeverityType.Warning)
                return;
            throw new OrmXmlParserException(string.Format("Xml document format error{1}: {0}", e.Message, (e.Exception != null) ? string.Format("({0},{1})", e.Exception.LineNumber, e.Exception.LinePosition) : string.Empty));
        }

        internal protected XmlDocument SourceXmlDocument
        {
            get
            {
                return _ormXmlDocument;
            }
            set
            {
                _ormXmlDocument = value;
            }
        }

        internal protected WXMLModel Model
        {
            get { return _ormObjectsDef; }
        }

    }
}
