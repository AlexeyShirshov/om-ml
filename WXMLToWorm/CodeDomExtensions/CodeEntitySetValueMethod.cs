using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using WXML.Model.Descriptors;
using Worm.Entities;
using Worm.Entities.Meta;
using WXML.Model;
using WXML.CodeDom;

namespace WXMLToWorm.CodeDomExtensions
{
	public class CodeEntitySetValueMethod : CodeMemberMethod
	{
        //WXMLCodeDomGeneratorSettings _settings
		public CodeEntitySetValueMethod(WXMLCodeDomGeneratorSettings settings, EntityDescription entity)
		{
			Name = "SetValueOptimized";
			// тип возвращаемого значения
			ReturnType = null;
			// модификаторы доступа
			Attributes = MemberAttributes.Public;
			if (entity.BaseEntity != null)
				Attributes |= MemberAttributes.Override;
			else
				ImplementationTypes.Add(new CodeTypeReference(typeof(IOptimizedValues)));
			Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "propertyAlias"));
			Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEntitySchema), "schema"));
			Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "value"));
			Statements.Add(
				new CodeVariableDeclarationStatement(
					new CodeTypeReference(typeof(string)),
					"fieldName",
					new CodeArgumentReferenceExpression("propertyAlias")
				)
			);

			foreach (var property in entity.CompleteEntity.Properties)
			{
				if (property.Disabled)
					continue;

                WXMLCodeDomGenerator.Delegates.GetUpdateSetValueMethodMethod(settings)(property, this);
			}

			if (entity.BaseEntity != null)
				Statements.Add(
					new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
							new CodeBaseReferenceExpression(),
							"SetValueOptimized"
							),
						new CodeArgumentReferenceExpression("propertyAlias"),
						new CodeArgumentReferenceExpression("schema"),
						new CodeArgumentReferenceExpression("value")
						)
					);
		}
	}
}
