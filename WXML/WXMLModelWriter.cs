using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using WXML.Model.Descriptors;
using System.Linq;

namespace WXML.Model
{
    internal class WXMLModelWriter
    {
        private readonly OrmXmlDocumentSet _ormXmlDocumentSet;
        private XmlDocument _ormXmlDocumentMain;
        private readonly WXMLModel _ormObjectsDef;

        private readonly XmlNamespaceManager _nsMgr;
        private readonly XmlNameTable _nametable;

        private readonly WXMLModelWriterSettings _settings;

        internal WXMLModelWriter(WXMLModel ormObjectsDef, WXMLModelWriterSettings settings)
        {
            _ormObjectsDef = ormObjectsDef;
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(WXMLModel.NS_PREFIX, WXMLModel.NS_URI);
            _ormXmlDocumentSet = new OrmXmlDocumentSet();
            _settings = settings;
        }

        internal static OrmXmlDocumentSet Generate(WXMLModel schema, WXMLModelWriterSettings settings)
        {
            WXMLModelWriter generator;
            generator = new WXMLModelWriter(schema, settings);

            generator.GenerateXmlDocumentInternal();

            return generator._ormXmlDocumentSet;
        }

        private void GenerateXmlDocumentInternal()
        {
            CreateXmlDocument();

            FillFileDescriptions();

            FillLinqSettings();

            FillImports();           

        	FillSourceFragments();

            FillTypes();

            FillEntities();

            FillRelations();
        }

        private void FillLinqSettings()
        {
            if (_ormObjectsDef.LinqSettings == null)
                return;

            var linqSettings = CreateElement("Linq");
            _ormXmlDocumentMain.DocumentElement.AppendChild(linqSettings);

            linqSettings.SetAttribute("enable", XmlConvert.ToString(_ormObjectsDef.LinqSettings.Enable));

            if (!string.IsNullOrEmpty(_ormObjectsDef.LinqSettings.ContextName))
                linqSettings.SetAttribute("contextName", _ormObjectsDef.LinqSettings.ContextName);

            if (!string.IsNullOrEmpty(_ormObjectsDef.LinqSettings.FileName))
                linqSettings.SetAttribute("filename", _ormObjectsDef.LinqSettings.FileName);

            if (_ormObjectsDef.LinqSettings.ContextClassBehaviour.HasValue)
                linqSettings.SetAttribute("contextClassBehaviour",
                                          _ormObjectsDef.LinqSettings.ContextClassBehaviour.ToString());
        }

        private void FillImports()
        {
            if(_ormObjectsDef.Includes.Count == 0)
                return;
            XmlNode importsNode = CreateElement("Includes");
            _ormXmlDocumentMain.DocumentElement.AppendChild(importsNode);
            foreach (WXMLModel objectsDef in _ormObjectsDef.Includes)
            {
                WXMLModelWriterSettings settings = (WXMLModelWriterSettings)_settings.Clone();
                    //settings.DefaultMainFileName = _settings.DefaultIncludeFileName + _ormObjectsDef.Includes.IndexOf(objectsDef);
                    OrmXmlDocumentSet set;
                    set = Generate(objectsDef, _settings);
                    _ormXmlDocumentSet.AddRange(set);
                    foreach (OrmXmlDocument ormXmlDocument in set)
                    {
                        if ((_settings.IncludeBehaviour & IncludeGenerationBehaviour.Inline) ==
                            IncludeGenerationBehaviour.Inline)
                        {
                            XmlNode importedSchemaNode =
                                _ormXmlDocumentMain.ImportNode(ormXmlDocument.Document.DocumentElement, true);
                            importsNode.AppendChild(importedSchemaNode);
                        }
                        else
                        {
                            XmlElement includeElement =
                                _ormXmlDocumentMain.CreateElement("xi", "include", "http://www.w3.org/2001/XInclude");
                            includeElement.SetAttribute("parse", "xml");

                            string fileName = GetIncludeFileName(_ormObjectsDef, objectsDef, settings);

                            includeElement.SetAttribute("href", fileName);
                            importsNode.AppendChild(includeElement);
                        }

                    }
            }
        }

        private void CreateXmlDocument()
        {
            _ormXmlDocumentMain = new XmlDocument(_nametable);
            XmlDeclaration declaration = _ormXmlDocumentMain.CreateXmlDeclaration("1.0", Encoding.UTF8.WebName, null);
            _ormXmlDocumentMain.AppendChild(declaration);
            XmlElement root = CreateElement("WXMLModel");
            _ormXmlDocumentMain.AppendChild(root);
            string filename = GetFilename(_ormObjectsDef, _settings);
            OrmXmlDocument doc = new OrmXmlDocument(filename, _ormXmlDocumentMain);
            _ormXmlDocumentSet.Add(doc);
          
        }

        private string GetFilename(WXMLModel objectsDef, WXMLModelWriterSettings settings)
        {
            return string.IsNullOrEmpty(objectsDef.FileName)
                       ? settings.DefaultMainFileName
                       : objectsDef.FileName;
        }

        private string GetIncludeFileName(WXMLModel objectsDef, WXMLModel incldeObjectsDef, WXMLModelWriterSettings settings)
        {
            if (string.IsNullOrEmpty(incldeObjectsDef.FileName))
            {
                string filename =
                    settings.IncludeFileNamePattern.Replace("%MAIN_FILENAME%", GetFilename(objectsDef, settings)).
                        Replace(
                        "%INCLUDE_NAME%", GetFilename(incldeObjectsDef, settings)) +
                    objectsDef.Includes.IndexOf(incldeObjectsDef);
                return
                    (((settings.IncludeBehaviour & IncludeGenerationBehaviour.PlaceInFolder) ==
                      IncludeGenerationBehaviour.PlaceInFolder)
                         ? settings.IncludeFolderName + System.IO.Path.DirectorySeparatorChar
                         : string.Empty) + filename;
            }
            else
                return incldeObjectsDef.FileName;
        }

        private void FillRelations()
        {
            if (_ormObjectsDef.Relations.Count == 0)
                return;
            XmlNode relationsNode = CreateElement("EntityRelations");
            _ormXmlDocumentMain.DocumentElement.AppendChild(relationsNode);
            foreach (RelationDescriptionBase rel in _ormObjectsDef.Relations)
            {
                XmlElement relationElement;
                if (rel is RelationDescription)
                {
					RelationDescription relation = (RelationDescription)rel;

                    relationElement = CreateElement("Relation");

                    relationElement.SetAttribute("table", relation.SourceFragment.Identifier);
                    if (relation.Disabled)
                    {
                        relationElement.SetAttribute("disabled", XmlConvert.ToString(relation.Disabled));
                    }

                    XmlElement leftElement = CreateElement("Left");
                    leftElement.SetAttribute("entity", relation.Left.Entity.Identifier);
                    leftElement.SetAttribute("fieldName", relation.Left.FieldName);
                    leftElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Left.CascadeDelete));
					
                    if (!string.IsNullOrEmpty(relation.Left.AccessorName))
						leftElement.SetAttribute("accessorName", relation.Left.AccessorName);

                    if (!string.IsNullOrEmpty(relation.Left.AccessorDescription))
                        leftElement.SetAttribute("accessorDescription", relation.Left.AccessorDescription);

                    if (relation.Left.AccessedEntityType != null)
                        leftElement.SetAttribute("accessedEntityType", relation.Left.AccessedEntityType.Identifier);

                    XmlElement rightElement = CreateElement("Right");
                    rightElement.SetAttribute("entity", relation.Right.Entity.Identifier);
                    rightElement.SetAttribute("fieldName", relation.Right.FieldName);
                    rightElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Right.CascadeDelete));
					
                    if (!string.IsNullOrEmpty(relation.Right.AccessorName))
						rightElement.SetAttribute("accessorName", relation.Right.AccessorName);

                    if (!string.IsNullOrEmpty(relation.Right.AccessorDescription))
                        rightElement.SetAttribute("accessorDescription", relation.Right.AccessorDescription);

                    if (relation.Right.AccessedEntityType != null)
						rightElement.SetAttribute("accessedEntityType", relation.Right.AccessedEntityType.Identifier);

                    if (relation.UnderlyingEntity != null)
                    {
                        relationElement.SetAttribute("underlyingEntity", relation.UnderlyingEntity.Identifier);
                    }

                    relationElement.AppendChild(leftElement);
                    relationElement.AppendChild(rightElement);
                    relationsNode.AppendChild(relationElement);
                }
                else
                {
					SelfRelationDescription relation = (SelfRelationDescription)rel;

                    relationElement = CreateElement("SelfRelation");

                    relationElement.SetAttribute("table", relation.SourceFragment.Identifier);
                    relationElement.SetAttribute("entity", relation.Entity.Identifier);
                    if (relation.Disabled)
                    {
                        relationElement.SetAttribute("disabled", XmlConvert.ToString(relation.Disabled));
                    }

                    XmlElement directElement = CreateElement("Direct");

                    directElement.SetAttribute("fieldName", relation.Direct.FieldName);
                    directElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Direct.CascadeDelete));
					
                    if (!string.IsNullOrEmpty(relation.Direct.AccessorName))
						directElement.SetAttribute("accessorName", relation.Direct.AccessorName);

                    if (!string.IsNullOrEmpty(relation.Direct.AccessorDescription))
                        directElement.SetAttribute("accessorDescription", relation.Direct.AccessorDescription);

                    if (relation.Direct.AccessedEntityType != null)
                        directElement.SetAttribute("accessedEntityType", relation.Direct.AccessedEntityType.Identifier);

                    XmlElement reverseElement = CreateElement("Reverse");
                    reverseElement.SetAttribute("fieldName", relation.Reverse.FieldName);
                    reverseElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Reverse.CascadeDelete));
					
                    if (!string.IsNullOrEmpty(relation.Reverse.AccessorName))
						reverseElement.SetAttribute("accessorName", relation.Reverse.AccessorName);

                    if (!string.IsNullOrEmpty(relation.Reverse.AccessorDescription))
                        reverseElement.SetAttribute("accessorDescription", relation.Reverse.AccessorDescription);

                    if (relation.Reverse.AccessedEntityType != null)
						reverseElement.SetAttribute("accessedEntityType", relation.Reverse.AccessedEntityType.Identifier);

                    if (relation.UnderlyingEntity != null)
                    {
                        relationElement.SetAttribute("underlyingEntity", relation.UnderlyingEntity.Identifier);
                    }
                    relationElement.AppendChild(directElement);
                    relationElement.AppendChild(reverseElement);
                    
                }
                if (rel.Constants.Count > 0)
                {
                    var constantsElement = CreateElement("Constants");
                    relationElement.InsertBefore(constantsElement, relationElement.FirstChild);

                    foreach (var constantDescriptor in rel.Constants)
                    {
                        var constantElement = CreateElement("Constant");
                        constantsElement.AppendChild(constantElement);

                        constantElement.SetAttribute("name", constantDescriptor.Name);
                        constantElement.SetAttribute("value", constantDescriptor.Value);
                    }
                }
                relationsNode.AppendChild(relationElement);
			}
        }

        private void FillEntities()
        {
            XmlNode entitiesNode = CreateElement("Entities");
            _ormXmlDocumentMain.DocumentElement.AppendChild(entitiesNode);

            foreach (EntityDescription entity in _ormObjectsDef.Entities)
            {
                XmlElement entityElement = CreateElement("Entity");

                entityElement.SetAttribute("id", entity.Identifier);
                entityElement.SetAttribute("name", entity.Name);
                if (!string.IsNullOrEmpty(entity.Description))
                    entityElement.SetAttribute("description", entity.Description);
                if (entity.Namespace != entity.OrmObjectsDef.Namespace)
                    entityElement.SetAttribute("namespace", entity.Namespace);
				if(entity.Behaviour != EntityBehaviuor.Default)
					entityElement.SetAttribute("behaviour", entity.Behaviour.ToString());
				if (entity.UseGenerics)
					entityElement.SetAttribute("useGenerics", XmlConvert.ToString(entity.UseGenerics));
				if (entity.MakeInterface)
					entityElement.SetAttribute("makeInterface", XmlConvert.ToString(entity.MakeInterface));
                if (entity.BaseEntity != null)
                    entityElement.SetAttribute("baseEntity", entity.Identifier);
				if (entity.Disabled)
					entityElement.SetAttribute("disabled", XmlConvert.ToString(entity.Disabled));

				if (entity.CacheCheckRequired)
					entityElement.SetAttribute("cacheCheckRequired", XmlConvert.ToString(entity.CacheCheckRequired));
				

                XmlNode tablesNode = CreateElement("SourceFragments");
                foreach (SourceFragmentRefDescription table in entity.SourceFragments)
                {
					XmlElement tableElement = CreateElement("SourceFragment");
                    tableElement.SetAttribute("ref", table.Identifier);
                    if (table.AnchorTable != null)
                    {
                        tableElement.SetAttribute("anchorTableRef", table.AnchorTable.Identifier);
                        tableElement.SetAttribute("type", table.JoinType.ToString());
                        foreach (SourceFragmentRefDescription.Condition c in table.Conditions)
                        {
                            XmlElement join = CreateElement("join");
                            join.SetAttribute("refColumn", c.LeftColumn);
                            join.SetAttribute("anchorColumn", c.RightColumn);
                            tableElement.AppendChild(join);
                        }
                    }
                    tablesNode.AppendChild(tableElement);
                }
				if(!entity.InheritsBaseTables)
				{
					((XmlElement)tablesNode).SetAttribute("inheritsBase", XmlConvert.ToString(entity.InheritsBaseTables));
				}
				
				entityElement.AppendChild(tablesNode);	

                XmlNode propertiesNode = CreateElement("Properties");
                IEnumerable<PropertyDescription> properties = entity.Properties.Where(p => p.Group == null);
                FillEntityProperties(properties, propertiesNode);
                entityElement.AppendChild(propertiesNode);

                List<PropertyGroup> groups = new List<PropertyGroup>();
                properties = entity.Properties.Where(p => p.Group != null);

                foreach (var property in properties)
                {
                    if (!groups.Contains(property.Group))
                        groups.Add(property.Group);
                }

                foreach (var propertyGroup in groups)
                {
                    PropertyGroup propertyGroup1 = propertyGroup;
                    var props = properties.Where(p => p.Group == propertyGroup1);

                    XmlElement groupNode = CreateElement("Group");
                    groupNode.SetAttribute("name", propertyGroup.Name);

                    if (!propertyGroup.Hide)
                        groupNode.SetAttribute("hide", XmlConvert.ToString(propertyGroup.Hide));

                    FillEntityProperties(props, groupNode);
                }
                if (entity.EntityRelations.Count > 0)
                {
                    XmlNode relationsNode = CreateElement("Relations");

                    foreach (var entityRelation in entity.EntityRelations)
                    {
                        var relationNode = CreateElement("Relation");

                        relationNode.SetAttribute("entity", entityRelation.Entity.Identifier);

                        if (!string.IsNullOrEmpty(entityRelation.PropertyAlias))
                            relationNode.SetAttribute("propertyAlias", entityRelation.PropertyAlias);

                        if (!string.IsNullOrEmpty(entityRelation.Name))
                            relationNode.SetAttribute("name", entityRelation.Name);

                        if (!string.IsNullOrEmpty(entityRelation.AccessorName))
                            relationNode.SetAttribute("accessorName", entityRelation.AccessorName);

                        if (!entityRelation.Disabled)
                            relationNode.SetAttribute("disabled", XmlConvert.ToString(entityRelation.Disabled));

                        if (!string.IsNullOrEmpty(entityRelation.AccessorDescription))
                            relationNode.SetAttribute("accessorDescription", entityRelation.AccessorDescription);

                        relationsNode.AppendChild(relationNode);
                    }

                    entityElement.AppendChild(relationsNode);
                }

                entitiesNode.AppendChild(entityElement);
            }
        }

        private void FillEntityProperties(IEnumerable<PropertyDescription> properties, XmlNode propertiesNode)
        {
            foreach (PropertyDescription property in properties)
            {
                XmlElement propertyElement =
                    CreateElement("Property");
                propertyElement.SetAttribute("propertyName", property.Name);
                if(property.Attributes != null && property.Attributes.Length > 0)
                {
                    propertyElement.SetAttribute("attributes", string.Join(" ", property.Attributes));
                }
                propertyElement.SetAttribute("table", property.SourceFragment.Identifier);
                propertyElement.SetAttribute("fieldName", property.FieldName);
                propertyElement.SetAttribute("typeRef", property.PropertyType.Identifier);
                if (!string.IsNullOrEmpty(property.Description))
                    propertyElement.SetAttribute("description", property.Description);
                if (property.FieldAccessLevel != AccessLevel.Private)
                    propertyElement.SetAttribute("classfieldAccessLevel", property.FieldAccessLevel.ToString());
                if (property.PropertyAccessLevel != AccessLevel.Public)
                    propertyElement.SetAttribute("propertyAccessLevel", property.PropertyAccessLevel.ToString());
                if (property.PropertyAlias != property.Name)
                    propertyElement.SetAttribute("propertyAlias", property.PropertyAlias);
                if (property.Disabled)
                    propertyElement.SetAttribute("disabled", XmlConvert.ToString(true));
                if (property.Obsolete != ObsoleteType.None)
                    propertyElement.SetAttribute("obsolete", property.Obsolete.ToString());
                if(!string.IsNullOrEmpty(property.ObsoleteDescripton))
                    propertyElement.SetAttribute("obsoleteDescription", property.ObsoleteDescripton);
                if (property.EnablePropertyChanged)
                    propertyElement.SetAttribute("enablePropertyChanged", XmlConvert.ToString(property.EnablePropertyChanged));
                if (!string.IsNullOrEmpty(property.DbTypeName))
                    propertyElement.SetAttribute("dbTypeName", property.DbTypeName);
                if (property.DbTypeSize.HasValue)
                    propertyElement.SetAttribute("dbTypeSize", XmlConvert.ToString(property.DbTypeSize.Value));
                if (property.DbTypeNullable.HasValue)
                    propertyElement.SetAttribute("dbTypeNullable", XmlConvert.ToString(property.DbTypeNullable.Value));
				if (!string.IsNullOrEmpty(property.DefferedLoadGroup))
					propertyElement.SetAttribute("defferedLoadGroup", property.DefferedLoadGroup);
                if (!string.IsNullOrEmpty(property.ColumnName))
                    propertyElement.SetAttribute("fieldAlias", property.ColumnName);
                propertiesNode.AppendChild(propertyElement);
            }
        }

        private void FillTypes()
        {
            XmlNode typesNode = CreateElement("Types");
            _ormXmlDocumentMain.DocumentElement.AppendChild(typesNode);
            foreach (TypeDescription type in _ormObjectsDef.Types)
            {
                XmlElement typeElement = CreateElement("Type");

                typeElement.SetAttribute("id", type.Identifier);

                XmlElement typeSubElement;
                if(type.IsClrType)
                {
                    typeSubElement = CreateElement("ClrType");
                    typeSubElement.SetAttribute("name", type.ClrTypeName);
                }
                else if(type.IsUserType)
                {
                    typeSubElement = CreateElement("UserType");
                    typeSubElement.SetAttribute("name", type.GetTypeName(null));
                    if(type.UserTypeHint.HasValue && type.UserTypeHint != UserTypeHintFlags.None)
                    {
                        typeSubElement.SetAttribute("hint", type.UserTypeHint.ToString().Replace(",", string.Empty));
                    }
                }
                else
                {
                    typeSubElement = CreateElement("Entity");
                    typeSubElement.SetAttribute("ref", type.Entity.Identifier);
                }
                typeElement.AppendChild(typeSubElement);
                typesNode.AppendChild(typeElement);
            }
        }

		private void FillSourceFragments()
		{
			XmlElement tablesNode = CreateElement("SourceFragments");
			_ormXmlDocumentMain.DocumentElement.AppendChild(tablesNode);
			foreach (SourceFragmentDescription table in _ormObjectsDef.SourceFragments)
			{
				XmlElement tableElement = CreateElement("SourceFragment");
				tableElement.SetAttribute("id", table.Identifier);
				tableElement.SetAttribute("name", table.Name);
				if(!string.IsNullOrEmpty(table.Selector))
					tableElement.SetAttribute("selector", table.Selector);
				tablesNode.AppendChild(tableElement);
			}
		}

        private XmlElement CreateElement(string name)
        {
            return _ormXmlDocumentMain.CreateElement(WXMLModel.NS_PREFIX, name, WXMLModel.NS_URI);
        }

        private void FillFileDescriptions()
        {
            _ormXmlDocumentMain.DocumentElement.SetAttribute("defaultNamespace", _ormObjectsDef.Namespace);
            _ormXmlDocumentMain.DocumentElement.SetAttribute("schemaVersion", _ormObjectsDef.SchemaVersion);
			if (!string.IsNullOrEmpty(_ormObjectsDef.EntityBaseTypeName))
				_ormXmlDocumentMain.DocumentElement.SetAttribute("entityBaseType", _ormObjectsDef.EntityBaseTypeName);
			if (_ormObjectsDef.EnableCommonPropertyChangedFire)
				_ormXmlDocumentMain.DocumentElement.SetAttribute("enableCommonPropertyChangedFire",
				                                                 XmlConvert.ToString(_ormObjectsDef.EnableCommonPropertyChangedFire));
            if (!_ormObjectsDef.GenerateEntityName)
                _ormXmlDocumentMain.DocumentElement.SetAttribute("generateEntityName",
                                                                 XmlConvert.ToString(_ormObjectsDef.GenerateEntityName));

            StringBuilder commentBuilder = new StringBuilder();
            foreach (string comment in _ormObjectsDef.SystemComments)
            {
                commentBuilder.AppendLine(comment);
            }

            if(_ormObjectsDef.UserComments.Count > 0)
            {
                commentBuilder.AppendLine();
                foreach (string comment in _ormObjectsDef.UserComments)
                {
                    commentBuilder.AppendLine(comment);
                }
            }

            XmlComment commentsElement =
                _ormXmlDocumentMain.CreateComment(commentBuilder.ToString());
            _ormXmlDocumentMain.InsertBefore(commentsElement, _ormXmlDocumentMain.DocumentElement);
        }
    }

    public class OrmXmlDocument
    {
        private XmlDocument m_document;
        private string m_fileName;

        public OrmXmlDocument(string filename, XmlDocument document)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException("filename");
            if (document == null)
                throw new ArgumentNullException("document");
            m_document = document;
            m_fileName = filename;
        }

        public XmlDocument Document
        {
            get { return m_document; }
            set { m_document = value; }
        }

        public string FileName
        {
            get { return m_fileName; }
            set { m_fileName = value; }
        }
    }

    public class OrmXmlDocumentSet : List<OrmXmlDocument>
    {
    }
}
