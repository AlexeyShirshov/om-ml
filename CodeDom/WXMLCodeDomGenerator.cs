using System;
using System.CodeDom;
using WXML.CodeDom.CodeDomPatterns;
using WXML.Model.Descriptors;
using LinqToCodedom.Generator;
using WXML.Model;

namespace WXML.CodeDom
{
    public partial class WXMLCodeDomGenerator
    {
        public static void SetMemberDescription(CodeTypeMember member, string description)
        {
            if (string.IsNullOrEmpty(description))
                return;
            member.Comments.Add(new CodeCommentStatement(string.Format("<summary>{1}{0}{1}</summary>", description, Environment.NewLine), true));
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
        public static class Delegates
        {
            public delegate void UpdateSetValueMethodDelegate(PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod);

            public static event WXML.CodeDom.WXMLCodeDomGeneratorNameHelper.GetSettingsDelegate SettingsRequied;

            private static WXMLCodeDomGeneratorSettings GetSettings()
            {
                WXMLCodeDomGeneratorSettings settings = null;
                var h = SettingsRequied;
                if (h != null)
                    settings = h();
                if (settings == null) throw new Exception("OrmCodeDomGeneratorSettings requied.");
                return settings;
            }

            public static UpdateSetValueMethodDelegate UpdateSetValueMethodMethod
            {
                get
                {
                    WXMLCodeDomGeneratorSettings settings = GetSettings();
                    if ((settings.LanguageSpecificHacks & LanguageSpecificHacks.SafeUnboxToEnum) ==
                        LanguageSpecificHacks.SafeUnboxToEnum)
                        return UpdateSetValueMethodDelegates.EnumPervUpdateSetValueMethod;
                    return UpdateSetValueMethodDelegates.DefaultUpdateSetValueMethod;
                }
            }

            /// <summary>
            /// void UpdateSetValueMethodDelegate(CodeMemberField field, PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod);
            /// </summary>
            public static class UpdateSetValueMethodDelegates
            {
                public static void DefaultUpdateSetValueMethod(PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod)
                {
                    //Type fieldRealType;
                    //fieldRealType = Type.GetType(field.Type.BaseType, false);

                    var setValueStatement = new CodeConditionStatement(
                        new CodeMethodInvokeExpression(
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(propertyDesc),
                            "Equals",
                            new CodeVariableReferenceExpression("fieldName"))
                        );

                	var fieldName = WXMLCodeDomGeneratorNameHelper.GetPrivateMemberName(propertyDesc.PropertyName);

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
                            Emit.declare("iconvVal", ()=>LinqToCodedom.Generator.CodeDom.VarRef("value") as IConvertible)
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
											new CodeCastExpression(propertyDesc.PropertyType,
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
											GetIConvertableMethodName(propertyDesc.PropertyType.ClrType.GetGenericArguments()[0]),
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
                                                 new CodeCastExpression(propertyDesc.PropertyType,
                                                     new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(new CodeTypeReference(typeof(Convert))), "ChangeType",
																		new CodeArgumentReferenceExpression("value"), new CodeTypeOfExpression(propertyDesc.PropertyType)))));
                    }
                    else
                    {
                        setValueStatement.TrueStatements.Add(new CodeAssignStatement(
                                                 new CodeFieldReferenceExpression(
                                                     new CodeThisReferenceExpression(), fieldName),
                                                 new CodeCastExpression(propertyDesc.PropertyType, new CodeArgumentReferenceExpression("value"))));
                    }
                    setValueStatement.TrueStatements.Add(new CodeMethodReturnStatement());
                    setvalueMethod.Statements.Add(setValueStatement);
                }

                private static string GetIConvertableMethodName(Type type)
                {
                    return "To" + type.Name;
                }

                public static void EnumPervUpdateSetValueMethod(PropertyDescription propertyDesc, CodeMemberMethod setvalueMethod)
                {
                	var fieldName = WXMLCodeDomGeneratorNameHelper.GetPrivateMemberName(propertyDesc.PropertyName);
                    if (propertyDesc.PropertyType.IsEnum)
                    {
                        var setValueStatement = new CodeConditionStatement(
                        new CodeMethodInvokeExpression(
                            WXMLCodeDomGeneratorHelper.GetFieldNameReferenceExpression(propertyDesc),
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
                                                                    new CodeTypeOfExpression(propertyDesc.PropertyType),
                                                                    "GetGenericArguments"
                                                                ),
                                                                new CodePrimitiveExpression(0)
                                                            )
                                    ),
                                    new CodeAssignStatement(
                                                         new CodeFieldReferenceExpression(
                                                             new CodeThisReferenceExpression(), fieldName),
                                                         new CodeCastExpression(
                                                         propertyDesc.PropertyType,
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
																 propertyDesc.PropertyType,
                                                                 new CodeMethodInvokeExpression(
                                                                    new CodeTypeReferenceExpression(typeof(Enum)),
                                                                    "ToObject",
                                // typeof(Nullable<int>).GetGenericArguments()[0]
																	new CodeTypeOfExpression(propertyDesc.PropertyType),
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
            }
        }
    }
}
