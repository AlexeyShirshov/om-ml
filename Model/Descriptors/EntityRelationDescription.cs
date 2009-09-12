using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace WXML.Model.Descriptors
{
    public class EntityRelationDefinition
    {
        public string AccessorDescription { get; set; }

        public EntityDefinition SourceEntity
        {
            get;
            set;
        }

        public EntityDefinition Entity
        {
            get;
            set;
        }

        public string PropertyAlias
        {
            get;
            set;
        }

        public bool Disabled
        {
            get;
            set;
        }

        public string AccessorName
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public MergeAction Action { get; set; }

        public EntityPropertyDefinition Property
        {
            get
            {
                EntityPropertyDefinition res = null;
                if (!string.IsNullOrEmpty(PropertyAlias))
                {
                    res = (EntityPropertyDefinition) Entity.GetProperties().SingleOrDefault(p => p.PropertyAlias == PropertyAlias);
                }
                else
                {
                    var lst = Entity.GetProperties()
                        .Where(p => p.PropertyType.IsEntityType && p.PropertyType.Entity == SourceEntity);
                    if (lst.Count() > 1)
                    {
                        throw new WXMLException(
                            string.Format(
                                "Возможно несколько вариантов связи от сущности '{0}' к '{1}'. Используйте PropertyAlias для указания свойства-связки.",
                                SourceEntity.Name, Entity.Name));
                    }
                    else if (lst.Count() == 0)
                    {
                        throw new WXMLException(
                            string.Format(
                                "Не возможно определить связь между сущностями '{0}' и '{1}'. Используйте PropertyAlias для указания свойства-связки.",
                                SourceEntity.Name, Entity.Name));
                    }
                    else if (lst.Count() > 0)
                    {
                        res = (EntityPropertyDefinition) lst.First();
                    }
                }
                return res;
            }
        }

        public RelationConstraint Constraint { get; set; }
    }
}
