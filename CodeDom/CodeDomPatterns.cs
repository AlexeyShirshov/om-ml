using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;

namespace WXML.CodeDom.CodeDomPatterns
{
    public static class CommonPatterns
    {
        //public static CodeExpression CodeIsExpression(CodeTypeReference typeReference, CodeExpression expression)
        //{
        //    // typeof({type}).IsAssignableFrom({expression}.GetType())
        //    return new CodeMethodInvokeExpression(
        //        new CodeTypeOfExpression(typeReference),
        //        "IsAssignableFrom",
        //        new CodeMethodInvokeExpression(
        //            expression,
        //            "GetType"
        //        )
        //        );
        //}

        //public static CodeExpression CodeAsExpression(CodeTypeReference typeReference, CodeExpression expression)
        //{
        //    throw new NotImplementedException();
        //}

        //public static CodeStatement CodePatternLock(CodeExpression lockExpression, params CodeStatement[] statements)
        //{
        //    if (lockExpression == null)
        //        throw new ArgumentNullException("lockExpression");

        //    string lockCachedExpression = GetUniqueId("lockCachedExpression");

        //    CodeStatement lockStatement = new CodeConditionStatement(
        //        new CodeBinaryOperatorExpression(lockExpression, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)),
        //        new CodeVariableDeclarationStatement(
        //            new CodeTypeReference(typeof(object)),
        //            lockCachedExpression,
        //            lockExpression
        //        ),
        //        new CodeExpressionStatement(
        //        new CodeMethodInvokeExpression(
        //            new CodeMethodReferenceExpression(
        //                new CodeTypeReferenceExpression(new CodeTypeReference(typeof (System.Threading.Monitor))),
        //                typeof (System.Threading.Monitor).GetMethod("Enter").Name
        //                ),
        //            new CodeVariableReferenceExpression(lockCachedExpression)
        //            )
        //        ),
        //        new CodeTryCatchFinallyStatement(
        //            statements,
        //            new CodeCatchClause[] {},
        //            new CodeStatement[] {
        //                (CodeStatement)
        //                new CodeExpressionStatement(
        //                    new CodeMethodInvokeExpression(
        //                        new CodeMethodReferenceExpression(
        //                            new CodeTypeReferenceExpression(new CodeTypeReference(typeof(System.Threading.Monitor))),
        //                            typeof(System.Threading.Monitor).GetMethod("Exit").Name
        //                        ),
        //                        new CodeVariableReferenceExpression(lockCachedExpression)
        //                    )
        //                )
        //            }
        //        )
        //    );

        //    return lockStatement;
        //}     

        //public static CodeStatement[] CodePatternUsingStatement(CodeExpression usingExpression, params CodeStatement[] statements)
        //{
        //    List<CodeStatement> result = new List<CodeStatement>();
        //    string usingVariableName = GetUniqueId("usingVariable");
        //    result.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(IDisposable)), usingVariableName));
        //    result.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(usingVariableName),new CodePrimitiveExpression(null)));
        //    CodeStatement[] tryStatements = new CodeStatement[statements.Length + 1];
        //    Array.Copy(statements, 0, tryStatements, 1, statements.Length);
        //    tryStatements[0] =
        //        new CodeAssignStatement(new CodeArgumentReferenceExpression(usingVariableName), usingExpression);
        //    result.Add(
        //        new CodeTryCatchFinallyStatement(
        //            tryStatements,
        //       new CodeCatchClause[] {
        //                             },
        //       new CodeStatement[] {
        //                               new CodeConditionStatement(
        //                                   new CodeBinaryOperatorExpression(
        //                                       new CodeVariableReferenceExpression(usingVariableName),
        //                                       CodeBinaryOperatorType.IdentityInequality,
        //                                       new CodePrimitiveExpression(null)
        //                                       ),
        //                                   new CodeExpressionStatement(
        //                                       new CodeMethodInvokeExpression(
        //                                           new CodeCastExpression(typeof(IDisposable), new CodeArgumentReferenceExpression(usingVariableName)),
        //                                           "Dispose",
        //                                           new CodeExpression[]{}
        //                                           )
        //                                       )
        //                                   )
        //                           }
        //        )
        //    );
        //    return result.ToArray();
        //}

        //private static string GetUniqueId()
        //{
        //    return (s_uniqueId++).ToString();
        //}

        //private static int s_uniqueId = 0;

        //private static string GetUniqueId(string prefix)
        //{
        //    return prefix + "_" + GetUniqueId();
        //}
    }
}
