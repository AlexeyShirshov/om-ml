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
        private readonly WXMLModel _model;

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
            _model = new WXMLModel();
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(WXMLModel.NS_PREFIX, WXMLModel.NS_URI);
            _xmlResolver = xmlResolver;
        }

        internal protected WXMLModelReader(XmlDocument document)
        {
            _model = new WXMLModel();
            _ormXmlDocument = document;
            _nametable = document.NameTable;
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(WXMLModel.NS_PREFIX, WXMLModel.NS_URI);            
        }

        internal protected static WXMLModel Parse(XmlReader reader, XmlResolver xmlResolver)
        {
            WXMLModelReader parser = new WXMLModelReader(reader, xmlResolver);

            parser.Read();

            parser.FillModel();

            return parser._model;
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
            parser.FillModel();
            return parser._model;                
        }

        private void FillModel()
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

        private static void FillExtension(IDictionary<Extension, XmlDocument> dictionary, XmlElement extension)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(extension.InnerXml);
            Extension ext = new Extension()
            {
                Name = extension.Attributes["name"].Value
            };

            string mergeAction = extension.GetAttribute("action");

            if (!string.IsNullOrEmpty(mergeAction))
                ext.Action = (MergeAction)Enum.Parse(typeof(MergeAction), mergeAction);

            dictionary.Add(ext, xdoc);
        }

        private void FillLinqSettings()
        {
            var settingsNode =
                (XmlElement)_ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Linq", WXMLModel.NS_PREFIX),_nsMgr);

            if (settingsNode == null)
                return;

            _model.LinqSettings = new LinqSettingsDescriptor
            {
                Enable = XmlConvert.ToBoolean(settingsNode.GetAttribute("enable")),
                ContextName = settingsNode.GetAttribute("contextName"),
                FileName = settingsNode.GetAttribute("filename"),
                BaseContext = settingsNode.GetAttribute("baseContext")
            };

            string behaviourValue = settingsNode.GetAttribute("contextClassBehaviour");
            if(!string.IsNullOrEmpty(behaviourValue))
            {
                var type =
                    (ContextClassBehaviourType) Enum.Parse(typeof (ContextClassBehaviourType), behaviourValue);
                _model.LinqSettings.ContextClassBehaviour = type;
            }
        }

        private void FillImports()
        {
            XmlNodeList importNodes = _ormXmlDocument.DocumentElement.SelectNodes(
                string.Format("{0}:Includes/{0}:WXMLModel", WXMLModel.NS_PREFIX), _nsMgr);

            foreach (XmlNode importNode in importNodes)
            {
                XmlDocument tempDoc = new XmlDocument();
                XmlNode importedNode = tempDoc.ImportNode(importNode, true);
                tempDoc.AppendChild(importedNode);
                WXMLModel import = LoadXmlDocument(tempDoc, true);

                Model.Includes.Add(import);
            }
        }

        internal protected void FillTypes()
        {
            XmlNodeList typeNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Types/{0}:Type", WXMLModel.NS_PREFIX), _nsMgr);

            foreach (XmlNode typeNode in typeNodes)
            {
                TypeDefinition type;
                XmlElement typeElement = (XmlElement)typeNode;
                string id = typeElement.GetAttribute("id");
                
                XmlNode typeDefNode = typeNode.LastChild;
                XmlElement typeDefElement = (XmlElement)typeDefNode;
                if(typeDefNode.LocalName.Equals("Entity"))
                {
                    string entityId = typeDefElement.GetAttribute("ref");
                    EntityDefinition entity = _model.GetEntity(entityId);
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
                _model.AddType(type);
            }
        }

        internal protected void FillEntities()
        {
            foreach (EntityDefinition entity in _model.OwnEntities)
            {
                XmlElement entityElement = (XmlElement)_ormXmlDocument.DocumentElement.SelectSingleNode(
                        string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX,
                                      entity.Identifier), _nsMgr);
                
                string baseEntityId = entityElement.GetAttribute("baseEntity");

                if (!string.IsNullOrEmpty(baseEntityId))
                {
                    EntityDefinition baseEntity = Model.GetEntity(baseEntityId);
                    if (baseEntity == null)
                        throw new WXMLParserException(
                            string.Format("Base entity '{0}' for entity '{1}' not found.", baseEntityId,
                                          entity.Identifier));
                    entity.BaseEntity = baseEntity;
                }

                FillProperties(entity);
                FillSuppresedProperties(entity);
                FillEntityRelations(entity);

                var extensionsNode =
                    entityElement.SelectNodes(string.Format("{0}:extension", WXMLModel.NS_PREFIX), _nsMgr);

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

            XmlNode entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX, entity.Identifier), _nsMgr);

            XmlNodeList relationsList = entityNode.SelectNodes(
                string.Format("{0}:Relations/{0}:Relation", WXMLModel.NS_PREFIX), _nsMgr);

            foreach(XmlElement relationNode in relationsList)
            {
                string entityId = relationNode.GetAttribute("entity");

                var relationEntity = _model.GetEntity(entityId);

                string propertyAlias = relationNode.GetAttribute("propertyAlias");

                string name = relationNode.GetAttribute("name");

                string accessorName = relationNode.GetAttribute("accessorName");

                string disabledAttribute = relationNode.GetAttribute("disabled");
                string mergeAction = relationNode.GetAttribute("action");

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

                if (!string.IsNullOrEmpty(mergeAction))
                    relation.Action = (MergeAction)Enum.Parse(typeof(MergeAction), mergeAction);

                entity.AddEntityRelations(relation);
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
            _model.Namespace = _ormXmlDocument.DocumentElement.GetAttribute("defaultNamespace");
            _model.SchemaVersion = _ormXmlDocument.DocumentElement.GetAttribute("schemaVersion");
        	_model.EntityBaseTypeName = _ormXmlDocument.DocumentElement.GetAttribute("entityBaseType");

            //string generateEntityName = _ormXmlDocument.DocumentElement.GetAttribute("generateEntityName");            
            //_model.GenerateEntityName = string.IsNullOrEmpty(generateEntityName) ? true : XmlConvert.ToBoolean(generateEntityName);

            string baseUriString = _ormXmlDocument.DocumentElement.GetAttribute("xml:base");
            if (!string.IsNullOrEmpty(baseUriString))
            {
                Uri baseUri = new Uri(baseUriString, UriKind.RelativeOrAbsolute);
                _model.FileName = Path.GetFileName(baseUri.ToString());
            }
        	
            string enableCommonPropertyChangedFireAttr =
        		_ormXmlDocument.DocumentElement.GetAttribute("enableCommonPropertyChangedFire");
			
            if (!string.IsNullOrEmpty(enableCommonPropertyChangedFireAttr))
				_model.EnableCommonPropertyChangedFire = XmlConvert.ToBoolean(enableCommonPropertyChangedFireAttr);

            string mode = _ormXmlDocument.DocumentElement.GetAttribute("generateMode");
            if (!string.IsNullOrEmpty(mode))
                _model.GenerateMode = (GenerateModeEnum)Enum.Parse(typeof(GenerateModeEnum), mode);

            string addVersionToSchemaName = _ormXmlDocument.DocumentElement.GetAttribute("addVersionToSchemaName");
            if (!string.IsNullOrEmpty(addVersionToSchemaName))
                _model.AddVersionToSchemaName = XmlConvert.ToBoolean(addVersionToSchemaName);

            string singleFile = _ormXmlDocument.DocumentElement.GetAttribute("singleFile");
            if (!string.IsNullOrEmpty(singleFile))
                _model.GenerateSingleFile = XmlConvert.ToBoolean(singleFile);
        }

        internal protected void FindEntities()
        {
            XmlNodeList entitiesList = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:Entities/{0}:Entity", WXMLModel.NS_PREFIX), _nsMgr);

            _model.ClearEntities();

            foreach (XmlNode entityNode in entitiesList)
            {
                EntityBehaviuor behaviour = EntityBehaviuor.ForcePartial;

                XmlElement entityElement = (XmlElement) entityNode;
                string id = entityElement.GetAttribute("id");
                string name = entityElement.GetAttribute("name");
                string description = entityElement.GetAttribute("description");
                string nameSpace = entityElement.GetAttribute("namespace");
                string behaviourName = entityElement.GetAttribute("behaviour");
                string familyName = entityElement.GetAttribute("familyName");
            	string useGenericsAttribute = entityElement.GetAttribute("useGenerics");
            	string makeInterfaceAttribute = entityElement.GetAttribute("makeInterface");
            	string disbledAttribute = entityElement.GetAttribute("disabled");
				string cacheCheckRequiredAttribute = entityElement.GetAttribute("cacheCheckRequired");
                string mergeAction = entityElement.GetAttribute("action");

				bool useGenerics = !string.IsNullOrEmpty(useGenericsAttribute) && XmlConvert.ToBoolean(useGenericsAttribute);
            	bool makeInterface = !string.IsNullOrEmpty(makeInterfaceAttribute) &&
            	                     XmlConvert.ToBoolean(makeInterfaceAttribute);
            	bool disabled = !string.IsNullOrEmpty(disbledAttribute) && XmlConvert.ToBoolean(disbledAttribute);
            	bool cacheCheckRequired = !string.IsNullOrEmpty(cacheCheckRequiredAttribute) &&
            	                          XmlConvert.ToBoolean(cacheCheckRequiredAttribute);

                if (!string.IsNullOrEmpty(behaviourName))
                    behaviour = (EntityBehaviuor) Enum.Parse(typeof (EntityBehaviuor), behaviourName);


                EntityDefinition entity = new EntityDefinition(id, name, nameSpace, description, _model)
                {
                    Behaviour = behaviour,
                    UseGenerics = useGenerics,
                    MakeInterface = makeInterface,
                    Disabled = disabled,
                    CacheCheckRequired = cacheCheckRequired,
                    FamilyName = familyName
                };

                if (!string.IsNullOrEmpty(mergeAction))
                    entity.Action = (MergeAction)Enum.Parse(typeof(MergeAction), mergeAction);


                FillEntityTables(entity);
            }
        }

        internal protected void FillProperties(EntityDefinition entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            XmlNode entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX, entity.Identifier), _nsMgr);

            foreach (XmlElement node in entityNode.SelectNodes(string.Format("{0}:Properties/*", WXMLModel.NS_PREFIX), _nsMgr))
            {
                if (node.LocalName == "Property")
                    FillEntityProperties(entity, node, null);
                else if (node.LocalName == "EntityProperty")
                    FillEntityEProperties(entity, node, null);
                else if (node.LocalName == "Group")
                {
                    string hideValue = node.GetAttribute("hide");

                    PropertyGroup group = new PropertyGroup
                    {
                        Name = node.GetAttribute("name"),
                        Hide = string.IsNullOrEmpty(hideValue) ? true : XmlConvert.ToBoolean(hideValue)
                    };

                    foreach (XmlElement groupNode in node.SelectNodes("*", _nsMgr))
                    {
                        if (groupNode.LocalName == "Property")
                            FillEntityProperties(entity, groupNode, group);
                        else if (groupNode.LocalName == "EntityProperty")
                            FillEntityEProperties(entity, groupNode, group);
                        else
                            throw new NotSupportedException(groupNode.LocalName);
                    }
                }
                else
                    throw new NotSupportedException(node.LocalName);
            }
        }

        private void FillEntityEProperties(EntityDefinition entity, XmlNode propertyNode, PropertyGroup group)
        {
            AccessLevel fieldAccessLevel, propertyAccessLevel;
            bool disabled = false, enablePropertyChanged = false;

            XmlElement propertyElement = (XmlElement)propertyNode;
            string description = propertyElement.GetAttribute("description");
            string name = propertyElement.GetAttribute("propertyName");
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
            string mergeAction = propertyElement.GetAttribute("action");
            string defferedLoadGroup = propertyElement.GetAttribute("defferedLoadGroup");

            string[] attrString = sAttributes.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            Field2DbRelations attributes = Field2DbRelations.None;
            foreach (string attr in attrString)
            {
                attributes |= (Field2DbRelations)Enum.Parse(typeof(Field2DbRelations), attr);
            }

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

            TypeDefinition typeDesc = _model.GetType(typeId, true);

            ObsoleteType obsolete = ObsoleteType.None;
            if (!string.IsNullOrEmpty(propertyObsolete))
            {
                obsolete = (ObsoleteType)Enum.Parse(typeof(ObsoleteType), propertyObsolete);
            }

            if (!string.IsNullOrEmpty(enablePropertyChangedAttribute))
                enablePropertyChanged = XmlConvert.ToBoolean(enablePropertyChangedAttribute);

            EntityPropertyDefinition property = new EntityPropertyDefinition(name, propertyAlias, attributes, description, fieldAccessLevel, propertyAccessLevel, typeDesc, table, entity)
            {
                Disabled = disabled,
                Obsolete = obsolete,
                ObsoleteDescripton = propertyObsoleteDescription,
                EnablePropertyChanged = enablePropertyChanged,
                Group = group,
                DefferedLoadGroup = defferedLoadGroup,
            };

            if (!string.IsNullOrEmpty(mergeAction))
                property.Action = (MergeAction)Enum.Parse(typeof(MergeAction), mergeAction);

            entity.AddProperty(property);

            foreach (XmlElement fieldMap in propertyNode.SelectNodes(string.Format("{0}:field", WXMLModel.NS_PREFIX), _nsMgr))
            {
                string fieldAlias = fieldMap.GetAttribute("fieldAlias");
                string dbTypeNameAttribute = fieldMap.GetAttribute("dbTypeName");
                string dbTypeSizeAttribute = fieldMap.GetAttribute("dbTypeSize");
                string dbTypeNullableAttribute = fieldMap.GetAttribute("dbTypeNullable");
                string dbTypeDefault = fieldMap.GetAttribute("sourceFieldDefault");
                string fieldname = fieldMap.GetAttribute("fieldName");
                string propAlias = fieldMap.GetAttribute("relatedProperty");

                int? sz = null;
                if (!string.IsNullOrEmpty(dbTypeSizeAttribute))
                    sz = XmlConvert.ToInt32(dbTypeSizeAttribute);

                bool isNullable = true;
                if (!string.IsNullOrEmpty(dbTypeNullableAttribute))
                    isNullable = XmlConvert.ToBoolean(dbTypeNullableAttribute);

                property.AddSourceFieldUnckeck(propAlias, fieldname, fieldAlias, dbTypeNameAttribute,
                    sz, isNullable, dbTypeDefault);

            }
        }

        private void FillEntityProperties(EntityDefinition entity, XmlNode propertyNode, PropertyGroup group)
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
            string mergeAction = propertyElement.GetAttribute("action");
            string dbTypeNameAttribute = propertyElement.GetAttribute("dbTypeName");
            string dbTypeSizeAttribute = propertyElement.GetAttribute("dbTypeSize");
            string dbTypeNullableAttribute = propertyElement.GetAttribute("dbTypeNullable");
            string dbTypeDefault = propertyElement.GetAttribute("sourceFieldDefault");

        	string defferedLoadGroup = propertyElement.GetAttribute("defferedLoadGroup");

            string[] attrString = sAttributes.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            Field2DbRelations attributes = Field2DbRelations.None;
            foreach (string attr in attrString)
            {
                attributes |= (Field2DbRelations)Enum.Parse(typeof(Field2DbRelations), attr);
            }

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

            TypeDefinition typeDesc = _model.GetType(typeId, true);
            
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

            SourceFieldDefinition sf = new SourceFieldDefinition(table, fieldname)
            {
                SourceType = dbTypeNameAttribute,
                DefaultValue = dbTypeDefault
            };

            if (!string.IsNullOrEmpty(dbTypeSizeAttribute))
                sf.SourceTypeSize = XmlConvert.ToInt32(dbTypeSizeAttribute);

            if (!string.IsNullOrEmpty(dbTypeNullableAttribute))
                sf.IsNullable = XmlConvert.ToBoolean(dbTypeNullableAttribute);

            ScalarPropertyDefinition property = new ScalarPropertyDefinition(entity, name, propertyAlias, attributes, description, typeDesc, sf, fieldAccessLevel, propertyAccessLevel)
            {
                Disabled = disabled,
                Obsolete = obsolete,
                ObsoleteDescripton = propertyObsoleteDescription,
                EnablePropertyChanged = enablePropertyChanged,
                Group = group,
                SourceFieldAlias = fieldAlias,
                DefferedLoadGroup = defferedLoadGroup
            };

            if (!string.IsNullOrEmpty(mergeAction))
                property.Action = (MergeAction)Enum.Parse(typeof (MergeAction), mergeAction);

            if (typeDesc.IsEntityType)
            {
                EntityPropertyDefinition ep = EntityPropertyDefinition.FromScalar(property);
                entity.AddProperty(ep);
                if (string.IsNullOrEmpty(sf.SourceFieldExpression))
                    ep.RemoveSourceFieldByExpression(sf.SourceFieldExpression);
            }
            else
                entity.AddProperty(property);
        }

        internal protected void FillRelations()
        {
			XmlNodeList relationNodes = _ormXmlDocument.DocumentElement.SelectNodes(string.Format("{0}:EntityRelations/{0}:Relation", WXMLModel.NS_PREFIX), _nsMgr);

            #region Relations
            foreach (XmlElement relationElement in relationNodes)
			{
                XmlNode leftTargetNode = relationElement.SelectSingleNode(string.Format("{0}:Left", WXMLModel.NS_PREFIX), _nsMgr);
                XmlNode rightTargetNode = relationElement.SelectSingleNode(string.Format("{0}:Right", WXMLModel.NS_PREFIX), _nsMgr);

				string relationTableId = relationElement.GetAttribute("table");
				string underlyingEntityId = relationElement.GetAttribute("underlyingEntity");
				string disabledValue = relationElement.GetAttribute("disabled");
                string mergeAction = relationElement.GetAttribute("action");
			    string constraint = relationElement.GetAttribute("constraint");

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

                string leftEntityProperties = leftTargetElement.GetAttribute("entityProperties");
                string rightEntityProperties = rightTargetElement.GetAttribute("entityProperties");

				TypeDefinition leftAccessedEntityType = _model.GetType(leftAccessedEntityTypeId, true);
				TypeDefinition rightAccessedEntityType = _model.GetType(rightAccessedEntityTypeId, true);

				SourceFragmentDefinition relationTable = _model.GetSourceFragment(relationTableId);

				EntityDefinition underlyingEntity;
				if (string.IsNullOrEmpty(underlyingEntityId))
					underlyingEntity = null;
				else
					underlyingEntity = _model.GetEntity(underlyingEntityId);

				bool disabled;
				if (string.IsNullOrEmpty(disabledValue))
					disabled = false;
				else
					disabled = XmlConvert.ToBoolean(disabledValue);

				EntityDefinition leftLinkTargetEntity = _model.GetEntity(leftLinkTargetEntityId);

				EntityDefinition rightLinkTargetEntity = _model.GetEntity(rightLinkTargetEntityId);

                LinkTarget leftLinkTarget = new LinkTarget(leftLinkTargetEntity, leftFieldName.Split(' '), 
                    leftEntityProperties.Split(' '),
                    leftCascadeDelete, leftAccessorName) 
                { 
                    AccessorDescription = leftAccessorDescription,
                    AccessedEntityType = leftAccessedEntityType
                };

                LinkTarget rightLinkTarget = new LinkTarget(rightLinkTargetEntity, rightFieldName.Split(' '), 
                    rightEntityProperties.Split(' '),
                    rightCascadeDelete, rightAccessorName) 
                { 
                    AccessorDescription = rightAccessorDescription,
                    AccessedEntityType = rightAccessedEntityType
                };

                RelationDefinition relation = new RelationDefinition(leftLinkTarget, rightLinkTarget, relationTable, underlyingEntity, disabled);
			    relation.Constraint = (RelationConstraint) Enum.Parse(typeof (RelationConstraint), constraint, true);
                //SourceConstraint cns = null;
                //switch ((RelationConstraint)Enum.Parse(typeof(RelationConstraint), constraint, true))
                //{
                //    case RelationConstraint.None:
                //        break;
                //    case RelationConstraint.PrimaryKey:
                //        cns = new SourceConstraint();
                //}

                //if (cns != null)
                //{
                //    leftLinkTarget.
                //    cns.SourceFields.Add();
                //    relationTable.Constraints.Add(cns);
                //}

			    if (!string.IsNullOrEmpty(mergeAction))
                    relation.Action = (MergeAction)Enum.Parse(typeof(MergeAction), mergeAction);
                
                _model.AddRelation(relation);

			    XmlNodeList constantsNodeList =
			        relationElement.SelectNodes(string.Format("{0}:Constants/{0}:Constant", WXMLModel.NS_PREFIX), _nsMgr);

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

			foreach (XmlElement relationElement in relationNodes)
			{
                XmlNode directTargetNode = relationElement.SelectSingleNode(string.Format("{0}:Direct", WXMLModel.NS_PREFIX), _nsMgr);
                XmlNode reverseTargetNode = relationElement.SelectSingleNode(string.Format("{0}:Reverse", WXMLModel.NS_PREFIX), _nsMgr);

                string relationTableId = relationElement.GetAttribute("table");
				string underlyingEntityId = relationElement.GetAttribute("underlyingEntity");
				string disabledValue = relationElement.GetAttribute("disabled");
				string entityId = relationElement.GetAttribute("entity");
                string entityProperties = relationElement.GetAttribute("entityProperties");
                string mergeAction = relationElement.GetAttribute("action");
                string constraint = relationElement.GetAttribute("constraint");

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
                
                TypeDefinition directAccessedEntityType = _model.GetType(directAccessedEntityTypeId, true);
				TypeDefinition reverseAccessedEntityType = _model.GetType(reverseAccessedEntityTypeId, true);

				var relationTable = _model.GetSourceFragment(relationTableId);

			    EntityDefinition underlyingEntity = string.IsNullOrEmpty(underlyingEntityId) ? null : _model.GetEntity(underlyingEntityId);

			    bool disabled = !string.IsNullOrEmpty(disabledValue) && XmlConvert.ToBoolean(disabledValue);

				EntityDefinition entity = _model.GetEntity(entityId);

                SelfRelationTarget directTarget = new SelfRelationTarget(directFieldName.Split(' '), directCascadeDelete, directAccessorName)
                {
                    AccessorDescription = directAccessorDescription,
                    AccessedEntityType = directAccessedEntityType
                };

                SelfRelationTarget reverseTarget = new SelfRelationTarget(reverseFieldName.Split(' '), reverseCascadeDelete, reverseAccessorName)
                {
                    AccessorDescription = reverseAccessorDescription,
                    AccessedEntityType = reverseAccessedEntityType
                };

				SelfRelationDefinition relation = new SelfRelationDefinition(entity, entityProperties.Split(' '),
                    directTarget, reverseTarget, relationTable, underlyingEntity, disabled)
                {
                    Constraint = (RelationConstraint)Enum.Parse(typeof(RelationConstraint), constraint, true)
                };

                if (!string.IsNullOrEmpty(mergeAction))
                    relation.Action = (MergeAction)Enum.Parse(typeof(MergeAction), mergeAction);

				_model.AddRelation(relation);
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

				_model.AddSourceFragment(new SourceFragmentDefinition(id, name, selector));
			}
		}

        internal protected void FillEntityTables(EntityDefinition entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            entity.ClearSourceFragments();

            XmlNode entityNode = _ormXmlDocument.DocumentElement.SelectSingleNode(string.Format("{0}:Entities/{0}:Entity[@id='{1}']", WXMLModel.NS_PREFIX, entity.Identifier), _nsMgr);

            XmlNodeList tableNodes = entityNode.SelectNodes(string.Format("{0}:SourceFragments/{0}:SourceFragment", WXMLModel.NS_PREFIX), _nsMgr);

            foreach (XmlNode tableNode in tableNodes)
            {
                XmlElement tableElement = (XmlElement)tableNode;
                string tableId = tableElement.GetAttribute("ref");
                //string mergeAction = tableElement.GetAttribute("action");

                var table = entity.Model.GetSourceFragment(tableId);
                if (table == null)
                    throw new WXMLParserException(String.Format("Table {0} not found.", tableId));

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
                        SourceFragmentRefDefinition.Condition condition = new SourceFragmentRefDefinition.Condition(
                            joinNode.GetAttribute("refColumn"),
                            joinNode.GetAttribute("anchorColumn")
                        );

                        if (string.IsNullOrEmpty(condition.RightColumn))
                            condition.RightConstant = joinNode.GetAttribute("constant");

                        tableRef.Conditions.Add(condition);
                    }
                }

                //if (!string.IsNullOrEmpty(mergeAction))
                //    tableRef.Action = (MergeAction)Enum.Parse(typeof(MergeAction), mergeAction);

                entity.AddSourceFragment(tableRef);
            }

            XmlNode tablesNode = entityNode.SelectSingleNode(string.Format("{0}:SourceFragments", WXMLModel.NS_PREFIX), _nsMgr);
            string inheritsTablesValue = ((XmlElement)tablesNode).GetAttribute("inheritsBase");
            entity.InheritsBaseTables = string.IsNullOrEmpty(inheritsTablesValue) || XmlConvert.ToBoolean(inheritsTablesValue);
        }

        internal protected void Read()
        {
            XmlSchemaSet schemaSet = new XmlSchemaSet(_nametable);

            XmlSchema schema = ResourceManager.GetXmlSchema("XInclude");
            schemaSet.Add(schema);
            schema = ResourceManager.GetXmlSchema(SCHEMA_NAME);
            schemaSet.Add(schema);
            
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
            {
                CloseInput = false,
                ConformanceLevel = ConformanceLevel.Document,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                Schemas = schemaSet,
                ValidationFlags =
                    XmlSchemaValidationFlags.ReportValidationWarnings |
                    XmlSchemaValidationFlags.AllowXmlAttributes |
                    XmlSchemaValidationFlags.ProcessIdentityConstraints,
                ValidationType = ValidationType.Schema
            };

            xmlReaderSettings.ValidationEventHandler += xmlReaderSettings_ValidationEventHandler;

            XmlDocument xml = new XmlDocument(_nametable);

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
            throw new WXMLParserException(string.Format("Xml document format error{1}: {0}", e.Message, (e.Exception != null) ? string.Format("({0},{1})", e.Exception.LineNumber, e.Exception.LinePosition) : string.Empty));
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
            get { return _model; }
        }

    }
}
