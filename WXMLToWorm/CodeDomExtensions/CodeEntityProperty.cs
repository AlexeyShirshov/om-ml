using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using WXML.Model.Descriptors;
using LinqToCodedom.CodeDomPatterns;
using WXML.Model;
using Worm.Entities.Meta;
using WXML.CodeDom;

namespace WXMLToWorm.CodeDomExtensions
{
	public class CodeEntityProperty : CodeMemberProperty
	{
        private WXMLCodeDomGeneratorSettings _settings;

		public CodeEntityProperty(WXMLCodeDomGeneratorSettings settings, PropertyDefinition property)
		{
            _settings = settings;
            Type = property.PropertyType.ToCodeType(_settings);
			HasGet = true;
			HasSet = true;
			Name = property.Name;
            Attributes = WXMLCodeDomGenerator.GetMemberAttribute(property.PropertyAccessLevel);
			if (property.Group != null && property.Group.Hide)
				Attributes = MemberAttributes.Family;

			var fieldName = new WXMLCodeDomGeneratorNameHelper(_settings).GetPrivateMemberName(property.Name);
			if (!property.FromBase)
			{
				CodeMethodInvokeExpression getUsingExpression = new CodeMethodInvokeExpression(
					new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Read"),
					WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(_settings, property)
					);

				if (property.PropertyType.IsEntityType && property.PropertyType.Entity.CacheCheckRequired)
				{
					getUsingExpression.Parameters.Add(new CodePrimitiveExpression(true));
				}

				CodeStatement[] getInUsingStatements = new CodeStatement[]
				                                       	{
				                                       		new CodeMethodReturnStatement(
				                                       			new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
				                                       			                                 fieldName))
				                                       	};

                if (property.Entity.HasPkFlatEntity)
                    GetStatements.Add(new LinqToCodedom.CodeDomPatterns.CodeUsingStatement(
                        getUsingExpression,
                        getInUsingStatements)
                    );
                else
                    GetStatements.AddRange(getInUsingStatements);

				if (property.Entity.Model.EnableReadOnlyPropertiesSetter ||
				    !property.HasAttribute(WXML.Model.Field2DbRelations.ReadOnly) || property.HasAttribute(WXML.Model.Field2DbRelations.PK))
				{
					CodeExpression setUsingExpression = new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "Write"),
						WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(settings, property)
						);

					List<CodeStatement> setInUsingStatements = new List<CodeStatement>();
					if(!property.Entity.EnableCommonEventRaise)
					{
						setInUsingStatements.Add(new CodeVariableDeclarationStatement(
						                         	typeof (bool),
						                         	"notChanged",
						                         	new CodeBinaryOperatorExpression(
						                         		new CodeFieldReferenceExpression(
						                         			new CodeThisReferenceExpression(),
						                         			fieldName
						                         			),
						                         		property.PropertyType.IsValueType
						                         			? CodeBinaryOperatorType.ValueEquality
						                         			: CodeBinaryOperatorType.IdentityEquality,
						                         		new CodePropertySetValueReferenceExpression()
						                         		)
						                         	));
						setInUsingStatements.Add(new CodeVariableDeclarationStatement(
                                                    property.PropertyType.ToCodeType(_settings),
						                         	"oldValue",
						                         	new CodeFieldReferenceExpression(
						                         		new CodeThisReferenceExpression(),
						                         		fieldName
						                         		)
						                         	));
					}
					setInUsingStatements.Add(new CodeAssignStatement(
					                                       			new CodeFieldReferenceExpression(
					                                       				new CodeThisReferenceExpression(),
					                                       				fieldName
					                                       				),
					                                       			new CodePropertySetValueReferenceExpression()
					                                       			)
					                                       	);
					if(!property.Entity.EnableCommonEventRaise)
					{
						setInUsingStatements.Add(new CodeConditionStatement(
						new CodeBinaryOperatorExpression(
							new CodePrimitiveExpression(false),
							CodeBinaryOperatorType.ValueEquality,
							new CodeVariableReferenceExpression("notChanged")
							),
						new CodeExpressionStatement(
							new CodeMethodInvokeExpression(
								new CodeThisReferenceExpression(),
								"RaisePropertyChanged",
								WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(_settings ,property),
								new CodeVariableReferenceExpression("oldValue")
								)
							)
						));
					}

                    if (property.Entity.HasPkFlatEntity)
                        SetStatements.Add(new CodeUsingStatement(setUsingExpression,setInUsingStatements.ToArray()));
                    else
                        SetStatements.AddRange(setInUsingStatements.ToArray());
                }
				else
					HasSet = false;
			}
			else if( property.IsRefreshed)
			{
				Attributes |= MemberAttributes.New;
				GetStatements.Add(
				new CodeMethodReturnStatement(
					new CodeCastExpression(
                        property.PropertyType.ToCodeType(_settings),
						new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name)
					)
				)
				);
				if (property.Entity.Model.EnableReadOnlyPropertiesSetter ||
				    !property.HasAttribute(WXML.Model.Field2DbRelations.ReadOnly) || property.HasAttribute(WXML.Model.Field2DbRelations.PK))
				{
					SetStatements.Add(
						new CodeAssignStatement(
							new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), property.Name),
							new CodePropertySetValueReferenceExpression()
							)
						);
				}
				else
				{
					HasSet = false;
				}
			}
			
			CodeAttributeDeclaration declaration = new CodeAttributeDeclaration(new CodeTypeReference(typeof(EntityPropertyAttribute)));

            if (!string.IsNullOrEmpty(property.PropertyAlias))
            {
                declaration.Arguments.Add(
                    new CodeAttributeArgument("PropertyAlias", WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(_settings, property)));
            }

			CustomAttributes.Add(declaration);

            WXMLCodeDomGenerator.SetMemberDescription(this, property.Description);	
		}
	}
}
