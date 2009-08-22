using System;
using System.CodeDom;
using WXML.Model.Descriptors;
using LinqToCodedom.Generator;
using WXML.Model;

namespace WXML.CodeDom
{
    public class WXMLCodeDomGenerator
    {
        private readonly WXMLCodeDomGeneratorSettings _generatorSettings;

        public WXMLCodeDomGenerator(WXMLCodeDomGeneratorSettings settings)
        {
            _generatorSettings = settings;
        }

        protected WXMLCodeDomGeneratorSettings Settings
        {
            get { return _generatorSettings; }
        }

        public static void SetMemberDescription(CodeTypeMember member, string description)
        {
            if (string.IsNullOrEmpty(description))
                return;
            member.Comments.Add(new CodeCommentStatement(string.Format("<summary>{1}{0}{1}</summary>", description, Environment.NewLine), true));
        }

        public static MemberAttributes GetMemberAttribute(ScalarPropertyDefinition p)
        {
            if (p.Group != null && p.Group.Hide)
                return MemberAttributes.Family;
            return GetMemberAttribute(p.PropertyAccessLevel);
        }

        public static MemberAttributes GetMemberAttribute(AccessLevel accessLevel)
        {
            switch (accessLevel)
            {
                case AccessLevel.Private:
                    return MemberAttributes.Private;
                case AccessLevel.Family:
                    return MemberAttributes.Family;
                case AccessLevel.Assembly:
                    return MemberAttributes.Assembly;
                case AccessLevel.Public:
                    return MemberAttributes.Public;
                case AccessLevel.FamilyOrAssembly:
                    return MemberAttributes.FamilyOrAssembly;
                default:
                    return 0;
            }
        }

        public void DefaultUpdateSetValueMethod(PropertyDefinition propertyDesc, CodeMemberMethod setvalueMethod)
        {
            //Type fieldRealType;
            //fieldRealType = Type.GetType(field.Type.BaseType, false);

            var setValueStatement = new CodeConditionStatement(
                new CodeMethodInvokeExpression(
                    WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc),
                    "Equals",
                    new CodeVariableReferenceExpression("fieldName"))
                );

            var fieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.Name);

            //setValueStatement.TrueStatements.Add(
            //    new CodeVariableDeclarationStatement(typeof(IConvertible), "vConv",
            //        new Codety)
            //    );

            //old: simple cast
            //setValueStatement.TrueStatements.Add(new CodeAssignStatement(
            //                         new CodeFieldReferenceExpression(
            //                             new CodeThisReferenceExpression(), field.Name),
            //                         new CodeCastExpression(field.Type,
            //                                                new CodeArgumentReferenceExpression(
            //                                                    "value"))));

            // new: solves problem of direct casts with Nullable<>
            if (propertyDesc.PropertyType.IsNullableType && propertyDesc.PropertyType.IsClrType && propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].IsValueType && !propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].Equals(typeof(Guid)))
            {
                setValueStatement.TrueStatements.Add(
                    //new CodeVariableDeclarationStatement(typeof(IConvertible), "iconvVal",
                    //                                     CodePatternAsExpression(new CodeTypeReference(typeof(IConvertible)),
                    //                                                             new CodeArgumentReferenceExpression("value")))
                    Emit.declare("iconvVal", () => LinqToCodedom.Generator.CodeDom.VarRef("value") as IConvertible)
                );
                setValueStatement.TrueStatements.Add(
                    new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("iconvVal"),
                                                         CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
                        new CodeStatement[]
									{
										new CodeAssignStatement(
											new CodeFieldReferenceExpression(
												new CodeThisReferenceExpression(), fieldName),
											new CodeCastExpression(propertyDesc.PropertyType.ToCodeType(Settings),
											                       new CodeArgumentReferenceExpression(
											                       	"value")))
									},
                        new CodeStatement[]
									{
										//System.Threading.Thread.CurrentThread.CurrentCulture
										new CodeAssignStatement(
											new CodeFieldReferenceExpression(
												new CodeThisReferenceExpression(), fieldName),
											new CodeMethodInvokeExpression(
											new CodeVariableReferenceExpression("iconvVal"),
											"To" + propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].Name,
											new CodePropertyReferenceExpression(
												new CodePropertyReferenceExpression(
													new CodeTypeReferenceExpression(typeof(System.Threading.Thread)),
													"CurrentThread"
													),
													"CurrentCulture"
												)
											)
											)
										
									}
                        )
                    );
            }
            else if (propertyDesc.PropertyType.IsValueType && (!propertyDesc.PropertyType.IsNullableType || !(propertyDesc.PropertyType.IsClrType && propertyDesc.PropertyType.ClrType.GetGenericArguments()[0].Equals(typeof(Guid)))))
            {
                //old: simple cast
                setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                                         new CodeFieldReferenceExpression(
                                             new CodeThisReferenceExpression(), fieldName),
                                         new CodeCastExpression(propertyDesc.PropertyType.ToCodeType(Settings),
                                             new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Convert))), "ChangeType",
                                                                new CodeArgumentReferenceExpression("value"), new CodeTypeOfExpression(propertyDesc.PropertyType.ToCodeType(Settings))))));
            }
            else
            {
                setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                                         new CodeFieldReferenceExpression(
                                             new CodeThisReferenceExpression(), fieldName),
                                         new CodeCastExpression(propertyDesc.PropertyType.ToCodeType(Settings), new CodeArgumentReferenceExpression("value"))));
            }
            setValueStatement.TrueStatements.Add(new CodeMethodReturnStatement());
            setvalueMethod.Statements.Add(setValueStatement);
        }

        public void EnumPervUpdateSetValueMethod(PropertyDefinition propertyDesc, CodeMemberMethod setvalueMethod)
        {
            var fieldName = new WXMLCodeDomGeneratorNameHelper(Settings).GetPrivateMemberName(propertyDesc.Name);
            if (propertyDesc.PropertyType.IsEnum)
            {
                var setValueStatement = new CodeConditionStatement(
                new CodeMethodInvokeExpression(
                    WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(Settings, propertyDesc),
                    "Equals",
                    new CodeVariableReferenceExpression("fieldName"))

                );
                if (propertyDesc.PropertyType.IsNullableType)
                {
                    setValueStatement.TrueStatements.Add(
                        new CodeConditionStatement(
                            new CodeBinaryOperatorExpression(
                                new CodeArgumentReferenceExpression("value"),
                                CodeBinaryOperatorType.IdentityEquality,
                                new CodePrimitiveExpression(null)
                            ),
                            new CodeStatement[]
                                {
                                    new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), fieldName),
                                                         new CodePrimitiveExpression(null))
                                },
                            new CodeStatement[]
                                {
                                    new CodeVariableDeclarationStatement(
                                        new CodeTypeReference(typeof(Type)),
                                        "t",
                                        new CodeArrayIndexerExpression(
                                                                new CodeMethodInvokeExpression(
                                                                    new CodeTypeOfExpression(propertyDesc.PropertyType.ToCodeType(Settings)),
                                                                    "GetGenericArguments"
                                                                ),
                                                                new CodePrimitiveExpression(0)
                                                            )
                                    ),
                                    new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), fieldName),
                                                         new CodeCastExpression(
                                                         propertyDesc.PropertyType.ToCodeType(Settings),
                                                         new CodeMethodInvokeExpression(
                                                            new CodeTypeReferenceExpression(typeof(Enum)),
                                                            "ToObject",
                                                            // typeof(Nullable<int>).GetGenericArguments()[0]
                                                            new CodeVariableReferenceExpression("t"),
                                                            new CodeArgumentReferenceExpression(
                                                                                    "value")
                                    )))
                    
                                                            
                                 }

                        )
                    );
                }
                else
                {
                    setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), fieldName),
                                                         new CodeCastExpression(
                                                         propertyDesc.PropertyType.ToCodeType(Settings),
                                                         new CodeMethodInvokeExpression(
                                                            new CodeTypeReferenceExpression(typeof(Enum)),
                                                            "ToObject",
                        // typeof(Nullable<int>).GetGenericArguments()[0]
                                                            new CodeTypeOfExpression(propertyDesc.PropertyType.ToCodeType(Settings)),
                                                            new CodeArgumentReferenceExpression(
                                                                                    "value")
                                    ))));
                }
                setValueStatement.TrueStatements.Add(new CodeMethodReturnStatement());
                setvalueMethod.Statements.Add(setValueStatement);
            }
            else
            {
                DefaultUpdateSetValueMethod(propertyDesc, setvalueMethod);
            }
        }

        public void UpdateSetValueMethodMethod(PropertyDefinition definition, CodeMemberMethod method)
        {
            if ((Settings.LanguageSpecificHacks & LanguageSpecificHacks.SafeUnboxToEnum) ==
                        LanguageSpecificHacks.SafeUnboxToEnum)
                EnumPervUpdateSetValueMethod(definition, method);
            DefaultUpdateSetValueMethod(definition, method);
        }
    }
}
