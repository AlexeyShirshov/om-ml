using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using WXML.Model.Descriptors;
using System.Linq;
using System.Xml.Linq;

namespace WXML.Model
{
    internal class WXMLModelWriter
    {
        private readonly WXMLDocumentSet _wxmlDocumentSet;
        private XmlDocument _ormXmlDocumentMain;
        private readonly WXMLModel _model;

        private readonly XmlNamespaceManager _nsMgr;
        private readonly XmlNameTable _nametable;

        private readonly WXMLModelWriterSettings _settings;

        internal WXMLModelWriter(WXMLModel model, WXMLModelWriterSettings settings)
        {
            _model = model;
            _nametable = new NameTable();
            _nsMgr = new XmlNamespaceManager(_nametable);
            _nsMgr.AddNamespace(WXMLModel.NS_PREFIX, WXMLModel.NS_URI);
            _wxmlDocumentSet = new WXMLDocumentSet();
            _settings = settings;
        }

        internal static WXMLDocumentSet Generate(WXMLModel schema, WXMLModelWriterSettings settings)
        {
            WXMLModelWriter generator = new WXMLModelWriter(schema, settings);

            generator.GenerateXmlDocumentInternal();

            return generator._wxmlDocumentSet;
        }

        public WXMLModel Model
        {
            get
            {
                return _model;
            }
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

            FillExtensions();
        }

        private void FillExtensions()
        {
            if (Model.Extensions.Count > 0)
            {
                var extensionsContainer = CreateElement("extensions");
                _ormXmlDocumentMain.DocumentElement.AppendChild(extensionsContainer);

                foreach (var extension in Model.Extensions)
                {
                    FillExtension(extensionsContainer, extension);
                }
            }
        }

        private void FillExtension(XmlElement extensionsContainer, KeyValuePair<Extension, XElement> extension)
        {
            var extensionElement = CreateElement("extension");
            extensionsContainer.AppendChild(extensionElement);

            extensionElement.SetAttribute("name", extension.Key.Name);
            if (extension.Key.Action != MergeAction.Merge)
                extensionElement.SetAttribute("action", extension.Key.Action.ToString());

            var el = extension.Value;
            //if (el.Name != XName.Get("extension", WXMLModel.NS_PREFIX))
            //{
            //    var e = _ormXmlDocumentMain.CreateElement(el.Name.LocalName, el.Name.NamespaceName);
            //    extensionElement.AppendChild(e);
            //    foreach (var attr in el.Attributes())
            //    {
            //        e.SetAttribute(attr.)
            //    }
            //    extensionElement = e;
            //}
            using (var reader = el.CreateReader())
            {
                reader.MoveToContent();
                extensionElement.InnerXml = reader.ReadInnerXml();
            }
        }

        private void FillLinqSettings()
        {
            if (_model.LinqSettings == null)
                return;

            var linqSettings = CreateElement("Linq");
            _ormXmlDocumentMain.DocumentElement.AppendChild(linqSettings);

            linqSettings.SetAttribute("enable", XmlConvert.ToString(_model.LinqSettings.Enable));

            if (!string.IsNullOrEmpty(_model.LinqSettings.ContextName))
                linqSettings.SetAttribute("contextName", _model.LinqSettings.ContextName);

            if (!string.IsNullOrEmpty(_model.LinqSettings.FileName))
                linqSettings.SetAttribute("filename", _model.LinqSettings.FileName);

            if (!string.IsNullOrEmpty(_model.LinqSettings.BaseContext))
                linqSettings.SetAttribute("baseContext", _model.LinqSettings.BaseContext);

            if (_model.LinqSettings.ContextClassBehaviour.HasValue)
                linqSettings.SetAttribute("contextClassBehaviour",
                                          _model.LinqSettings.ContextClassBehaviour.ToString());
        }

        private void FillImports()
        {
            if (_model.Includes.Count == 0)
                return;
            XmlNode importsNode = CreateElement("Includes");
            _ormXmlDocumentMain.DocumentElement.AppendChild(importsNode);
            foreach (WXMLModel objectsDef in _model.Includes)
            {
                WXMLModelWriterSettings settings = (WXMLModelWriterSettings)_settings.Clone();
                //settings.DefaultMainFileName = _settings.DefaultIncludeFileName + _ormObjectsDef.Includes.IndexOf(objectsDef);
                WXMLDocumentSet set = Generate(objectsDef, _settings);
                _wxmlDocumentSet.AddRange(set);
                foreach (WXMLDocument ormXmlDocument in set)
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

                        string fileName = GetIncludeFileName(_model, objectsDef, settings);

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
            string filename = GetFilename(_model, _settings);
            WXMLDocument doc = new WXMLDocument(filename, _ormXmlDocumentMain);
            _wxmlDocumentSet.Add(doc);

        }

        private static string GetFilename(WXMLModel model, WXMLModelWriterSettings settings)
        {
            return string.IsNullOrEmpty(model.FileName)
                       ? settings.DefaultMainFileName
                       : model.FileName;
        }

        private static string GetIncludeFileName(WXMLModel model, WXMLModel model2include, WXMLModelWriterSettings settings)
        {
            if (string.IsNullOrEmpty(model2include.FileName))
            {
                string filename =
                    settings.IncludeFileNamePattern.Replace("%MAIN_FILENAME%", GetFilename(model, settings)).
                        Replace(
                        "%INCLUDE_NAME%", GetFilename(model2include, settings)) +
                    model.Includes.IndexOf(model2include);
                return
                    (((settings.IncludeBehaviour & IncludeGenerationBehaviour.PlaceInFolder) ==
                      IncludeGenerationBehaviour.PlaceInFolder)
                         ? settings.IncludeFolderName + System.IO.Path.DirectorySeparatorChar
                         : string.Empty) + filename;
            }
            else
                return model2include.FileName;
        }

        private void FillRelations()
        {
            if (_model.OwnRelations.Count() == 0)
                return;
            XmlNode relationsNode = CreateElement("EntityRelations");
            _ormXmlDocumentMain.DocumentElement.AppendChild(relationsNode);
            foreach (RelationDefinitionBase rel in _model.OwnRelations)
            {
                XmlElement relationElement;
                if (rel is RelationDefinition)
                {
                    RelationDefinition relation = (RelationDefinition)rel;

                    relationElement = CreateElement("Relation");

                    relationElement.SetAttribute("table", relation.SourceFragment.Identifier);
                    if (relation.Disabled)
                    {
                        relationElement.SetAttribute("disabled", XmlConvert.ToString(relation.Disabled));
                    }

                    XmlElement leftElement = CreateElement("Left");
                    leftElement.SetAttribute("entity", relation.Left.Entity.Identifier);
                    leftElement.SetAttribute("entityProperties", string.Join(" ", relation.Left.EntityProperties));
                    leftElement.SetAttribute("fieldName", string.Join(" ", relation.Left.FieldName));
                    leftElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Left.CascadeDelete));

                    if (!string.IsNullOrEmpty(relation.Left.AccessorName))
                        leftElement.SetAttribute("accessorName", relation.Left.AccessorName);

                    if (!string.IsNullOrEmpty(relation.Left.AccessorDescription))
                        leftElement.SetAttribute("accessorDescription", relation.Left.AccessorDescription);

                    if (relation.Left.AccessedEntityType != null)
                        leftElement.SetAttribute("accessedEntityType", relation.Left.AccessedEntityType.Identifier);

                    XmlElement rightElement = CreateElement("Right");
                    rightElement.SetAttribute("entity", relation.Right.Entity.Identifier);
                    rightElement.SetAttribute("entityProperties", string.Join(" ", relation.Right.EntityProperties));
                    rightElement.SetAttribute("fieldName", string.Join(" ", relation.Right.FieldName));
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

                    if (relation.Constraint != RelationConstraint.None)
                        relationElement.SetAttribute("constraint", relation.Constraint.ToString().ToLower());

                    relationElement.AppendChild(leftElement);
                    relationElement.AppendChild(rightElement);
                    relationsNode.AppendChild(relationElement);
                }
                else
                {
                    SelfRelationDefinition relation = (SelfRelationDefinition)rel;

                    relationElement = CreateElement("SelfRelation");

                    relationElement.SetAttribute("table", relation.SourceFragment.Identifier);
                    relationElement.SetAttribute("entity", relation.Entity.Identifier);
                    relationElement.SetAttribute("entityProperties", string.Join(" ", relation.EntityProperties));
                    if (relation.Disabled)
                    {
                        relationElement.SetAttribute("disabled", XmlConvert.ToString(relation.Disabled));
                    }

                    XmlElement directElement = CreateElement("Direct");

                    directElement.SetAttribute("fieldName", string.Join(" ", relation.Direct.FieldName));
                    directElement.SetAttribute("cascadeDelete", XmlConvert.ToString(relation.Direct.CascadeDelete));

                    if (!string.IsNullOrEmpty(relation.Direct.AccessorName))
                        directElement.SetAttribute("accessorName", relation.Direct.AccessorName);

                    if (!string.IsNullOrEmpty(relation.Direct.AccessorDescription))
                        directElement.SetAttribute("accessorDescription", relation.Direct.AccessorDescription);

                    if (relation.Direct.AccessedEntityType != null)
                        directElement.SetAttribute("accessedEntityType", relation.Direct.AccessedEntityType.Identifier);

                    XmlElement reverseElement = CreateElement("Reverse");
                    reverseElement.SetAttribute("fieldName", string.Join(" ", relation.Reverse.FieldName));
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

                    if (relation.Constraint != RelationConstraint.None)
                        relationElement.SetAttribute("constraint", relation.Constraint.ToString().ToLower());

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

                if (rel.Action != MergeAction.Merge)
                    relationElement.SetAttribute("action", rel.Action.ToString());

                relationsNode.AppendChild(relationElement);
            }
        }

        private void FillEntities()
        {
            XmlNode entitiesNode = CreateElement("Entities");
            _ormXmlDocumentMain.DocumentElement.AppendChild(entitiesNode);

            foreach (EntityDefinition entity in _model.OwnEntities)
            {
                XmlElement entityElement = CreateElement("Entity");

                entityElement.SetAttribute("id", entity.Identifier);
                if (entity.Identifier != entity.Name)
                    entityElement.SetAttribute("name", entity.Name);

                if (!string.IsNullOrEmpty(entity.Description))
                    entityElement.SetAttribute("description", entity.Description);
                if (entity.Namespace != entity.Model.Namespace)
                    entityElement.SetAttribute("namespace", entity.Namespace);
                if (entity.Behaviour != EntityBehaviuor.Default)
                    entityElement.SetAttribute("behaviour", entity.Behaviour.ToString());
                if (entity.UseGenerics)
                    entityElement.SetAttribute("useGenerics", XmlConvert.ToString(entity.UseGenerics));
                //if (entity.MakeInterface)
                //    entityElement.SetAttribute("makeInterface", XmlConvert.ToString(entity.MakeInterface));
                if (entity.BaseEntity != null)
                    entityElement.SetAttribute("baseEntity", entity.BaseEntity.Identifier);
                if (entity.Disabled)
                    entityElement.SetAttribute("disabled", XmlConvert.ToString(entity.Disabled));
                if (entity.Action != MergeAction.Merge)
                    entityElement.SetAttribute("action", entity.Action.ToString());
                if (!string.IsNullOrEmpty(entity.FamilyName) && entity.FamilyName != entity.Name)
                    entityElement.SetAttribute("familyName", entity.FamilyName);

                if (entity.CacheCheckRequired)
                    entityElement.SetAttribute("cacheCheckRequired", XmlConvert.ToString(entity.CacheCheckRequired));

                if (entity.Interfaces.Count > 0 || entity.AutoInterface)
                {
                    XmlElement e = null;
                    if (entity.AutoInterface)
                    {
                        e = CreateElement("autoInterface");
                    }
                    else
                    {
                        e = CreateElement("interfaces");
                        foreach (TypeDefinition tp in entity.Interfaces)
                        {
                            var t = CreateElement("interface");
                            t.InnerText = tp.Identifier;
                            e.AppendChild(t);
                        }
                    }
                    entityElement.AppendChild(e);
                }

                XmlElement tablesElement = CreateElement("SourceFragments");
                if (!entity.InheritsBaseTables)
                {
                    tablesElement.SetAttribute("inheritsBase", XmlConvert.ToString(entity.InheritsBaseTables));
                    if (FillEntityTables(entity.GetSourceFragments(), tablesElement))
                        entityElement.AppendChild(tablesElement);
                }
                else
                {
                    FillEntityTables(entity.OwnSourceFragments, tablesElement);
                    entityElement.AppendChild(tablesElement);
                }


                FillEntityProperties(entityElement, entity);

                FillEntityRelations(entityElement, entity);

                if (entity.Extensions.Count > 0)
                {
                    foreach (var extension in entity.Extensions)
                    {
                        FillExtension(entityElement, extension);
                    }
                }

                entitiesNode.AppendChild(entityElement);
            }
        }

        private void FillEntityProperties(XmlNode entityElement, EntityDefinition entity)
        {
            XmlNode propertiesNode = CreateElement("Properties");
            HashSet<PropertyGroup> group2skip = new HashSet<PropertyGroup>();
            foreach (PropertyDefinition prop in entity.OwnProperties)
            {
                PropertyGroup group = prop.Group;
                if (group != null)
                {
                    if (!group2skip.Contains(group))
                    {
                        group2skip.Add(group);

                        XmlElement groupNode = CreateElement("Group");
                        groupNode.SetAttribute("name", group.Name);

                        if (!group.Hide)
                            groupNode.SetAttribute("hide", XmlConvert.ToString(group.Hide));

                        propertiesNode.AppendChild(groupNode);

                        foreach (var gp in entity.OwnProperties
                            .Where(item => group == item.Group))
                        {
                            FillEntityProperties(gp, groupNode);
                        }
                    }
                }
                else
                    FillEntityProperties(prop, propertiesNode);
            }

            entityElement.AppendChild(propertiesNode);
        }

        private void FillEntityRelations(XmlNode entityElement, EntityDefinition entity)
        {
            if (entity.One2ManyRelations.Count() > 0)
            {
                XmlNode relationsNode = CreateElement("Relations");

                foreach (var entityRelation in entity.One2ManyRelations)
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

                    if (entityRelation.Action != MergeAction.Merge)
                        relationNode.SetAttribute("action", entityRelation.Action.ToString());

                    relationsNode.AppendChild(relationNode);
                }

                entityElement.AppendChild(relationsNode);
            }
        }

        private bool FillEntityTables(IEnumerable<SourceFragmentRefDefinition> tables, XmlNode tablesElement)
        {
            foreach (SourceFragmentRefDefinition table in tables)
            {
                XmlElement tableElement = CreateElement("SourceFragment");
                tableElement.SetAttribute("ref", table.Identifier);
                if (table.Replaces != null)
                    tableElement.SetAttribute("replaces", table.Replaces.Identifier);

                if (table.AnchorTable != null)
                {
                    tableElement.SetAttribute("joinTableRef", table.AnchorTable.Identifier);
                    tableElement.SetAttribute("type", table.JoinType.ToString());
                    foreach (SourceFragmentRefDefinition.Condition c in table.Conditions)
                    {
                        XmlElement join = CreateElement("condition");
                        
                        if (!string.IsNullOrEmpty(c.LeftColumn))
                            join.SetAttribute("refColumn", c.LeftColumn);

                        if (!string.IsNullOrEmpty(c.RightColumn))
                            join.SetAttribute("joinColumn", c.RightColumn);
                        
                        if (!string.IsNullOrEmpty(c.RightConstant))
                            join.SetAttribute("constant", c.RightConstant);

                        tableElement.AppendChild(join);
                    }
                }
                tablesElement.AppendChild(tableElement);
            }
            return tables.Count() > 0;
        }

        private void FillEntityProperties(PropertyDefinition rp, XmlNode propertiesNode)
        {
            XmlElement propertyElement = null;
            if (rp is ScalarPropertyDefinition)
                propertyElement = CreateElement("Property");
            else if (rp is EntityPropertyDefinition)
                propertyElement = CreateElement("EntityProperty");
            else if (rp is CustomPropertyDefinition)
                propertyElement = CreateElement("CustomProperty");
            else
                throw new NotSupportedException(rp.GetType().ToString());

            if (rp.PropertyAccessLevel == AccessLevel.Private &&
                rp.Interface != null)
            {
                string[] ss = rp.Name.Split(':');
                if (ss.Length > 1)
                    propertyElement.SetAttribute("name", ss[1]);
                else
                    propertyElement.SetAttribute("name", rp.Name);
            }
            else
                propertyElement.SetAttribute("name", rp.Name);

            if (rp.Attributes != Field2DbRelations.None)
            {
                propertyElement.SetAttribute("attributes",
                    Enum.GetName(typeof(Field2DbRelations), rp.Attributes));
            }

            if (rp.SourceFragment != null)
                propertyElement.SetAttribute("table", rp.SourceFragment.Identifier);

            if (rp.PropertyType != null)
            {
                if (rp is ScalarPropertyDefinition || rp is CustomPropertyDefinition)
                    propertyElement.SetAttribute("type", rp.PropertyType.Identifier);
                else if (rp is EntityPropertyDefinition)
                    propertyElement.SetAttribute("referencedEntity", rp.PropertyType.Entity.Identifier);
                else
                    throw new NotSupportedException(rp.GetType().ToString());
            }

            if (!string.IsNullOrEmpty(rp.Description))
                propertyElement.SetAttribute("description", rp.Description);

            if (rp.FieldAccessLevel != AccessLevel.Private && !(rp is CustomPropertyDefinition))
                propertyElement.SetAttribute("classfieldAccessLevel", rp.FieldAccessLevel.ToString());

            if (rp.PropertyAccessLevel != AccessLevel.Public)
                propertyElement.SetAttribute("propertyAccessLevel", rp.PropertyAccessLevel.ToString());

            if (rp.PropertyAlias != rp.Name)
                propertyElement.SetAttribute("propertyAlias", rp.PropertyAlias);

            if (rp.Disabled)
                propertyElement.SetAttribute("disabled", XmlConvert.ToString(true));

            if (rp.Obsolete != ObsoleteType.None)
                propertyElement.SetAttribute("obsolete", rp.Obsolete.ToString());

            if (!string.IsNullOrEmpty(rp.ObsoleteDescripton))
                propertyElement.SetAttribute("obsoleteDescription", rp.ObsoleteDescripton);

            if (rp.EnablePropertyChanged && !(rp is CustomPropertyDefinition))
                propertyElement.SetAttribute("enablePropertyChanged", XmlConvert.ToString(rp.EnablePropertyChanged));

            if (rp.Action != MergeAction.Merge)
                propertyElement.SetAttribute("action", rp.Action.ToString());

            if (!string.IsNullOrEmpty(rp.DefferedLoadGroup) && !(rp is CustomPropertyDefinition))
                propertyElement.SetAttribute("defferedLoadGroup", rp.DefferedLoadGroup);

            if (!rp.GenerateAttribute && !(rp is CustomPropertyDefinition))
                propertyElement.SetAttribute("generateAttribute", XmlConvert.ToString(rp.GenerateAttribute));

            if (!string.IsNullOrEmpty(rp.AvailableFrom) && !(rp is CustomPropertyDefinition))
                propertyElement.SetAttribute("availableFrom", rp.AvailableFrom);

            if (!string.IsNullOrEmpty(rp.AvailableTo) && !(rp is CustomPropertyDefinition))
                propertyElement.SetAttribute("availableTo", rp.AvailableTo);

            if (rp.Interface != null)
            {
                propertyElement.SetAttribute("interface", rp.Interface.Identifier);

                if (!string.IsNullOrEmpty(rp.InterfaceProperty))
                    propertyElement.SetAttribute("interfaceProperty", rp.InterfaceProperty);
            }

            if (rp is ScalarPropertyDefinition)
            {
                ScalarPropertyDefinition property = rp as ScalarPropertyDefinition;

                if (!string.IsNullOrEmpty(property.SourceFieldAlias))
                    propertyElement.SetAttribute("fieldAlias", property.SourceFieldAlias);

                if (property.SourceField != null)
                {
                    propertyElement.SetAttribute("fieldName", property.SourceFieldExpression);

                    if (!string.IsNullOrEmpty(property.SourceType))
                        propertyElement.SetAttribute("fieldTypeName", property.SourceType);

                    if (property.SourceTypeSize.HasValue)
                        propertyElement.SetAttribute("fieldTypeSize", XmlConvert.ToString(property.SourceTypeSize.Value));

                    if (!property.IsNullable)
                        propertyElement.SetAttribute("fieldNullable", XmlConvert.ToString(property.IsNullable));

                    if (!string.IsNullOrEmpty(property.SourceField.DefaultValue))
                        propertyElement.SetAttribute("fieldDefault", property.SourceField.DefaultValue);
                }

            }
            else if (rp is EntityPropertyDefinition)
            {
                EntityPropertyDefinition property = rp as EntityPropertyDefinition;
                foreach (EntityPropertyDefinition.SourceField field in property.SourceFields)
                {
                    XmlElement fields = CreateElement("field");

                    fields.SetAttribute("relatedProperty", field.PropertyAlias);

                    if (!string.IsNullOrEmpty(field.SourceFieldExpression))
                        fields.SetAttribute("fieldName", field.SourceFieldExpression);

                    if (!string.IsNullOrEmpty(field.SourceType))
                        fields.SetAttribute("fieldTypeName", field.SourceType);

                    if (field.SourceTypeSize.HasValue)
                        fields.SetAttribute("fieldTypeSize", XmlConvert.ToString(field.SourceTypeSize.Value));

                    if (!field.IsNullable)
                        fields.SetAttribute("fieldNullable", XmlConvert.ToString(field.IsNullable));

                    if (!string.IsNullOrEmpty(field.SourceFieldAlias))
                        fields.SetAttribute("fieldAlias", field.SourceFieldAlias);

                    if (!string.IsNullOrEmpty(field.DefaultValue))
                        fields.SetAttribute("fieldDefault", field.DefaultValue);

                    propertyElement.AppendChild(fields);
                }
            }
            else if (rp is CustomPropertyDefinition)
            {
                CustomPropertyDefinition property = rp as CustomPropertyDefinition;
                XmlElement get = CreateElement("Get");
                FillBody(get, property.GetBody);
                propertyElement.AppendChild(get);

                if (property.SetBody != null)
                {
                    XmlElement set = CreateElement("Set");
                    FillBody(set, property.SetBody);
                    propertyElement.AppendChild(set);
                }
            }
            else
            {
                throw new NotSupportedException(rp.GetType().ToString());
            }

            if (rp.Extensions.Count > 0)
            {
                foreach (var extension in rp.Extensions)
                {
                    FillExtension(propertyElement, extension);
                }
            }

            propertiesNode.AppendChild(propertyElement);
        }

        private void FillBody(XmlElement element, CustomPropertyDefinition.Body body)
        {
            if (!string.IsNullOrEmpty(body.PropertyName))
            {
                XmlElement propElement = CreateElement("Property");
                propElement.SetAttribute("name", body.PropertyName);
                element.AppendChild(propElement);
            }
            else
            {
                if (!string.IsNullOrEmpty(body.CSCode))
                {
                    XmlElement cs = CreateElement("CS");
                    cs.AppendChild(CreateCData(body.CSCode));
                    element.AppendChild(cs);
                }

                if (!string.IsNullOrEmpty(body.VBCode))
                {
                    XmlElement vb = CreateElement("VB");
                    vb.AppendChild(CreateCData(body.VBCode));
                    element.AppendChild(vb);
                }
            }
        }

        private void FillTypes()
        {
            if (_model.OwnTypes.Count() == 0) return;
            
            XmlNode typesNode = CreateElement("Types");
            _ormXmlDocumentMain.DocumentElement.AppendChild(typesNode);
            foreach (TypeDefinition type in _model.OwnTypes)
            {
                XmlElement typeElement = CreateElement("Type");

                typeElement.SetAttribute("id", type.Identifier);

                XmlElement typeSubElement;
                if (type.IsClrType)
                {
                    typeSubElement = CreateElement("ClrType");
                    typeSubElement.SetAttribute("name", type.ClrType.FullName);
                }
                else if (type.IsUserType)
                {
                    typeSubElement = CreateElement("UserType");
                    typeSubElement.SetAttribute("name", type.GetTypeName(null));
                    if (type.UserTypeHint.HasValue && type.UserTypeHint != UserTypeHintFlags.None)
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
            foreach (SourceFragmentDefinition table in _model.OwnSourceFragments)
            {
                XmlElement tableElement = CreateElement("SourceFragment");
                tableElement.SetAttribute("id", table.Identifier);
                tableElement.SetAttribute("name", table.Name);
                if (!string.IsNullOrEmpty(table.Selector))
                    tableElement.SetAttribute("selector", table.Selector);

                if (table.Extensions.Count > 0)
                {
                    foreach (var extension in table.Extensions)
                    {
                        FillExtension(tableElement, extension);
                    }
                }

                tablesNode.AppendChild(tableElement);
            }
        }

        private XmlElement CreateElement(string name)
        {
            return _ormXmlDocumentMain.CreateElement(WXMLModel.NS_PREFIX, name, WXMLModel.NS_URI);
        }

        private XmlCDataSection CreateCData(string data)
        {
            return _ormXmlDocumentMain.CreateCDataSection(data);
        }

        private void FillFileDescriptions()
        {
            XmlElement root = _ormXmlDocumentMain.DocumentElement;

            if (!string.IsNullOrEmpty(_model.Namespace))
                root.SetAttribute("defaultNamespace", _model.Namespace);

            if (!string.IsNullOrEmpty(_model.SchemaVersion))
                root.SetAttribute("schemaVersion", _model.SchemaVersion);

            if (!string.IsNullOrEmpty(_model.EntityBaseTypeName))
                root.SetAttribute("entityBaseType", _model.EntityBaseTypeName);

            if (_model.EnableCommonPropertyChangedFire)
                root.SetAttribute("enableCommonPropertyChangedFire",
                    XmlConvert.ToString(_model.EnableCommonPropertyChangedFire));
            
            //if (!_model.GenerateEntityName)
            //    root.SetAttribute("generateEntityName",
            //        XmlConvert.ToString(_model.GenerateEntityName));

            if (_model.GenerateMode != GenerateModeEnum.Full)
                root.SetAttribute("generateMode", _model.GenerateMode.ToString());

            if (_model.AddVersionToSchemaName)
                root.SetAttribute("addVersionToSchemaName", "true");

            if (_model.GenerateSingleFile)
                root.SetAttribute("singleFile", "true");

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

            XmlComment commentsElement =
                _ormXmlDocumentMain.CreateComment(commentBuilder.ToString());
            _ormXmlDocumentMain.InsertBefore(commentsElement, root);
        }
    }

    public class WXMLDocument
    {
        private XmlDocument m_document;
        private string m_fileName;

        public WXMLDocument(string filename, XmlDocument document)
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

    public class WXMLDocumentSet : List<WXMLDocument>
    {
    }
}
