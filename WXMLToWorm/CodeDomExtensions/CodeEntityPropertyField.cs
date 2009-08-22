using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using WXML.Model.Descriptors;
using WXML.Model;
using WXML.CodeDom;

namespace WXMLToWorm.CodeDomExtensions
{
	public class CodeEntityPropertyField : CodeMemberField
	{
		public CodeEntityPropertyField(WXMLCodeDomGeneratorSettings settings, ScalarPropertyDefinition property)
		{
            Type = property.PropertyType.ToCodeType(settings);
			Name = new WXMLCodeDomGeneratorNameHelper(settings).GetPrivateMemberName(property.Name);
            Attributes = WXMLCodeDomGenerator.GetMemberAttribute(property.FieldAccessLevel);
		}
	}
}
