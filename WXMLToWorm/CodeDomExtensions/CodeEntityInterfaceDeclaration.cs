using System.CodeDom;
using WXML.Model.Descriptors;
using WXML.Model;
using WXML.CodeDom;

namespace WXMLToWorm.CodeDomExtensions
{
	public class CodeEntityInterfaceDeclaration : CodeTypeDeclaration
	{
		private CodeEntityTypeDeclaration m_entityTypeDeclaration;
		private readonly CodeTypeReference m_typeReference;

		private string m_namePrefix;
		private string m_nameSuffix;

		public string NamePrefix
		{
			get
			{
				return m_namePrefix;
			}
			set
			{
				m_namePrefix = value;
			}
		}

		public string NameSuffix
		{
			get
			{
				return m_nameSuffix;
			}
			set
			{
				m_nameSuffix = value;
			}
		}

		public CodeEntityInterfaceDeclaration()
		{
			m_typeReference = new CodeTypeReference();

			IsClass = false;
            IsPartial = false;
			IsInterface = true;

            var settings = WXMLCodeDomGeneratorNameHelper.GetSettings();
            if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.AllowPartialInterfaces) == LanguageSpecificHacks.AllowPartialInterfaces)
                IsPartial = true;
		}

		public CodeEntityInterfaceDeclaration(CodeEntityTypeDeclaration entityTypeDeclaration) : this(entityTypeDeclaration, null, null)
		{			
		}

		public CodeEntityInterfaceDeclaration(CodeEntityTypeDeclaration entityTypeDeclaration, string prefix, string suffix)
			: this()
		{
			NamePrefix = prefix;
			NameSuffix = suffix;

			EntityTypeDeclaration = entityTypeDeclaration;

			m_typeReference.BaseType = FullName;
		    
			
		}

		public string FullName
		{
			get
			{
				return WXMLCodeDomGeneratorNameHelper.GetEntityInterfaceName(Entity, NamePrefix, NameSuffix, true);
			}
		}

		public new string Name
		{
			get
			{
				if (Entity != null)
					return WXMLCodeDomGeneratorNameHelper.GetEntityInterfaceName(Entity, NamePrefix, NameSuffix, false);
				return null;
			}
		}

		public EntityDescription Entity
		{
			get
			{
				EntityDescription entity = null;
				if (EntityTypeDeclaration != null)
					entity = EntityTypeDeclaration.Entity;
				return entity;
			}
		}

		public CodeTypeReference TypeReference
		{
			get { return m_typeReference; }
		}

		public CodeEntityTypeDeclaration EntityTypeDeclaration
		{
			get { return m_entityTypeDeclaration; }
			set
			{
				m_entityTypeDeclaration = value;
				EnsureData();
			}
		}

		private CodeTypeReference m_baseInterfaceTypeReference;

		protected internal void EnsureData()
		{
			base.Name = Name;
			if(Entity != null && Entity.BaseEntity != null && Entity.BaseEntity.MakeInterface)
			{
				if(m_baseInterfaceTypeReference != null)
				{
					BaseTypes.Remove(m_baseInterfaceTypeReference);
				}
				m_baseInterfaceTypeReference =
					new CodeTypeReference(WXMLCodeDomGeneratorNameHelper.GetEntityInterfaceName(Entity.BaseEntity, NamePrefix, NameSuffix, true));
				BaseTypes.Add(m_baseInterfaceTypeReference);
			}
		}
	}
}
