using System;
using System.Text.RegularExpressions;
using WXML.Model.Descriptors;
using System.Linq;

namespace WXML.CodeDom
{
    public class WXMLCodeDomGeneratorNameHelper
    {
        //public delegate WXMLCodeDomGeneratorSettings GetSettingsDelegate();

        //public static event GetSettingsDelegate OrmCodeDomGeneratorSettingsRequied;

        private WXMLCodeDomGeneratorSettings _settings;

        public WXMLCodeDomGeneratorNameHelper(WXMLCodeDomGeneratorSettings settings)
        {
            _settings = settings;
        }

        public string GetPrivateMemberName(string name)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();

            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return settings.PrivateMembersPrefix + name.Substring(0, 1).ToLower() + name.Substring(1);
        }

        public WXMLCodeDomGeneratorSettings GetSettings()
        {
            return _settings;
        }

        public static string GetSafeName(string p)
        {
            // todo: сделать более интелектуальным его
            Regex regex = new Regex("[\\W]+");
            return regex.Replace(p, "_");
        }


        public string GetEntityFileName(EntityDefinition entity)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            string baseName =
                // prefix for file name
                settings.FileNamePrefix +
                // class name of the entity
                GetEntityClassName(entity, false) +
                // suffix for file name
                settings.FileNameSuffix;
            return baseName;
        }

        public string GetEntitySchemaDefFileName(EntityDefinition entity)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            string baseName =
                settings.FileNamePrefix +
                GetEntitySchemaDefClassName(entity) +
                settings.FileNameSuffix;
            return baseName;
        }

        ///// <summary>
        ///// Gets class name of the entity using settings
        ///// </summary>
        ///// <param name="entity">The entity.</param>
        ///// <returns></returns>
        //public string GetEntityClassName(EntityDefinition entity)
        //{
        //    return GetEntityClassName(entity, false);
        //}

        /// <summary>
        /// Gets class name of the entity using settings
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="qualified">if set to <c>true</c> return qualified name.</param>
        /// <returns></returns>
        public string GetEntityClassName(EntityDefinition entity, bool qualified)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            string en = entity.Name;

            if (entity.Model != null && entity.Model.OwnEntities.Any(e => e.Name == en && e.Identifier != entity.Identifier))
            {
                if (string.IsNullOrEmpty(entity.GetSourceFragments().First().Selector))
                {
                    int idx = entity.Model.OwnEntities.Count(e => e.Name == en && e.Identifier != entity.Identifier);
                    en = en + idx;
                }
                else
                    en = entity.GetSourceFragments().First().Selector + en;
            }

            string className =
                // prefix from settings for class name
                settings.ClassNamePrefix +
                // entity's class name
                en +
                // suffix from settings for class name
                settings.ClassNameSuffix;

            string ns = string.Empty;

            if (qualified && !string.IsNullOrEmpty(entity.Namespace))
                ns = entity.Namespace + ".";

            return ns + className;
        }

        /// <summary>
        /// Gets the name of the schema definition class for entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public string GetEntitySchemaDefClassName(EntityDefinition entity)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            return
                // name of the entity class name
                GetEntityClassName(entity, false) +
                // entity
                settings.EntitySchemaDefClassNameSuffix +
                (entity.Model.AddVersionToSchemaName ? entity.Model.SchemaVersion : String.Empty);
        }

        public string GetEntitySchemaDefClassQualifiedName(EntityDefinition entity, bool addNamespace)
        {
            if (entity.NeedOwnSchema())
                return string.Format("{0}.{1}", GetEntityClassName(entity, addNamespace), GetEntitySchemaDefClassName(entity));
            else
                return GetEntitySchemaDefClassQualifiedName(entity.BaseEntity, true);
        }

        public string GetEntityInterfaceName(EntityDefinition entity)
        {
            return GetEntityInterfaceName(entity, null, null, false);
        }

        public string GetEntityInterfaceName(EntityDefinition entity, string prefix, string suffix, bool qualified)
        {
            string interfaceName = "I" + (prefix ?? string.Empty) + GetEntityClassName(entity, false) + (suffix ?? string.Empty);

            string ns = string.Empty;
            if (qualified && !string.IsNullOrEmpty(entity.Namespace))
            {
                ns += entity.Namespace + ".";
            }
            return ns + interfaceName;
        }

        public static string GetMultipleForm(string name)
        {
            switch (name.ToLower())
            {
                case "man":
                    return "men";
                case "woman":
                    return "women";
                case "mouse":
                    return "mice";
                case "tooth":
                    return "teeth";
                case "foot":
                    return "feet";
                case "child":
                    return "children";
                case "ox":
                    return "oxen";
                case "goose":
                    return "geese";
                case "sheep":
                    return "sheep";
                case "deer":
                    return "deer";
                case "swine":
                    return "swine";
            }

            if (name.EndsWith("s"))
                return name;
            if (name.EndsWith("f"))
                return name.Remove(name.Length - 1, 1) + "ves";
            if (name.EndsWith("fe"))
                return name.Remove(name.Length - 2, 2) + "ves";
            if (name.EndsWith("o"))
                return name + "es";
            if (name.EndsWith("y"))
                return name.Substring(0, name.Length - 1) + "ies";
            return name + "s";
        }
    }
}
