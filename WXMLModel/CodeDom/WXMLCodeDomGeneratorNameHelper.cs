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
            // todo: ������� ����� ��������������� ���
            Regex regex = new Regex("[\\W]+");
            return regex.Replace(p, "_");
        }


        public string GetEntityFileName(EntityDescription entity)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            string baseName = 
                // prefix for file name
                settings.FileNamePrefix + 
                // class name of the entity
                GetEntityClassName(entity) +
                // suffix for file name
                settings.FileNameSuffix;
            return baseName;
        }

        public string GetEntitySchemaDefFileName(EntityDescription entity)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            string baseName = 
                settings.FileNamePrefix + 
                GetEntitySchemaDefClassName(entity) +
                settings.FileNameSuffix;
            return baseName;
        }

    	/// <summary>
    	/// Gets class name of the entity using settings
    	/// </summary>
    	/// <param name="entity">The entity.</param>
    	/// <returns></returns>
    	public string GetEntityClassName(EntityDescription entity)
    	{
    		return GetEntityClassName(entity, false);
    	}

    	/// <summary>
		/// Gets class name of the entity using settings
		/// </summary>
		/// <param name="entity">The entity.</param>
		/// <param name="qualified">if set to <c>true</c> return qualified name.</param>
		/// <returns></returns>
        public string GetEntityClassName(EntityDescription entity, bool qualified)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            string en = entity.Name;
            
            if (entity.Model.ActiveEntities.Count(e => e.Name == en && e.Identifier != entity.Identifier) > 0)
            {
                if (string.IsNullOrEmpty(entity.GetSourceFragments().First().Selector))
                {
                    int idx = entity.Model.ActiveEntities
                        .Count(e => e.Name == en && e.Identifier.CompareTo(entity.Identifier) > 0);
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
				ns += entity.Namespace + ".";
			
            return ns + className;               
        }

        /// <summary>
        /// Gets the name of the schema definition class for entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public string GetEntitySchemaDefClassName(EntityDescription entity)
        {
            WXMLCodeDomGeneratorSettings settings = GetSettings();
            return 
                // name of the entity class name
                GetEntityClassName(entity) + 
                // entity
                settings.EntitySchemaDefClassNameSuffix + 
                (entity.Model.AddVersionToSchemaName ? entity.Model.SchemaVersion : String.Empty);
        }

        public string GetEntitySchemaDefClassQualifiedName(EntityDescription entity)
        {
            return string.Format("{0}.{1}", GetEntityClassName(entity, true), GetEntitySchemaDefClassName(entity));
        }

    	public string GetEntityInterfaceName(EntityDescription entity)
    	{
    		return GetEntityInterfaceName(entity, null, null, false);
    	}

    	public string GetEntityInterfaceName(EntityDescription entity, string prefix, string suffix, bool qualified)
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
            if (name.EndsWith("s"))
                return name + "es";
            if (name.EndsWith("y"))
                return name.Substring(0, name.Length - 1) + "ies";
            return name + "s";
        }
    }
}
