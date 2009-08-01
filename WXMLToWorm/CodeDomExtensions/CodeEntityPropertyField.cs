﻿using System;
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
		public CodeEntityPropertyField(WXMLCodeDomGeneratorSettings settings, PropertyDescription property)
		{
            Type = property.PropertyType.ToCodeType(settings);
			Name = new WXMLCodeDomGeneratorNameHelper(settings).GetPrivateMemberName(property.PropertyName);
            Attributes = WXMLCodeDomGenerator.GetMemberAttribute(property.FieldAccessLevel);
		}
	}
}
