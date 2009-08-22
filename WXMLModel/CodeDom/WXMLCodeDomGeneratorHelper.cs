using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using WXML.Model.Descriptors;

namespace WXML.CodeDom
{
    public static class WXMLCodeDomGeneratorHelper
    {
        public static CodeExpression GetFieldNameReferenceExpression(WXMLCodeDomGeneratorSettings settings, PropertyDefinition propertyDesc)
        {
            string className = new WXMLCodeDomGeneratorNameHelper(settings).GetEntityClassName(propertyDesc.Entity, true) + ".Properties";
            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(className),
                                             propertyDesc.PropertyAlias);
        }

        public static CodeExpression GetEntityNameReferenceExpression(WXMLCodeDomGeneratorSettings settings, EntityDefinition entityDescription)
        {
            string className = new WXMLCodeDomGeneratorNameHelper(settings).GetEntityClassName(entityDescription, true) + ".Descriptor";
            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(className), "EntityName");
        }

        public static CodeExpression GetEntityClassReferenceExpression(WXMLCodeDomGeneratorSettings settings, EntityDefinition entityDesc)
        {
            string className = new WXMLCodeDomGeneratorNameHelper(settings).GetEntityClassName(entityDesc, true);
            return new CodeTypeReferenceExpression(className);
        }

        public static CodeTypeReference GetEntityClassTypeReference(WXMLCodeDomGeneratorSettings settings, EntityDefinition entityDesc)
        {
            string className = new WXMLCodeDomGeneratorNameHelper(settings).GetEntityClassName(entityDesc);
            return new CodeTypeReference(className);
        }

        public static CodeTypeOfExpression GetEntityClassTypeReferenceExpression(WXMLCodeDomGeneratorSettings settings, EntityDefinition entityDesc)
        {
            return new CodeTypeOfExpression(GetEntityClassTypeReference(settings, entityDesc));
        }

        public static CodeExpression GetPropertyReferenceExpression(WXMLCodeDomGeneratorSettings settings, ScalarPropertyDefinition propertyDesc)
        {
            return new CodePropertyReferenceExpression(GetEntityClassReferenceExpression(settings, propertyDesc.Entity), propertyDesc.Name);
        }

        const string REGION_PROPERTIES = "Properties";
        const string REGION_STATIC_MEMBERS = "Static members";
        const string REGION_BASE_TYPE_RELATED = "Base type related members";
        const string REGION_PRIVATE_FIELDS = "Private Fields";
        const string REGION_NESTED_TYPES = "Nested Types";
        const string REGION_CONSTRUCTORS = "Constructors";

        //public static CodeCompileUnit MergeCodeCompileUnits(IEnumerable<CodeCompileUnit> units)
        //{
        //    CodeCompileUnit merged = new CodeCompileUnit();
        //    Dictionary<string, CodeNamespace> addedNamespaces = new Dictionary<string, CodeNamespace>();
        //    foreach (CodeCompileUnit unit in units)
        //    {
        //        CodeNamespace targetNS;
        //        foreach (CodeNamespace sourceNamespace in unit.Namespaces)
        //        {
        //            if(!addedNamespaces.TryGetValue(sourceNamespace.Name, out targetNS))
        //            {
        //                targetNS = new CodeNamespace(sourceNamespace.Name);
        //            }

        //        }
        //    }
        //}

        public static void SetRegions(CodeTypeDeclaration typeDeclaration)
        {
            if (typeDeclaration.IsEnum)
                return;
            RegionsDictionary Regions = new RegionsDictionary();

            List<CodeTypeMember> members = new List<CodeTypeMember>(typeDeclaration.Members.Count);

            foreach (CodeTypeMember mem in typeDeclaration.Members)
            {
                members.Add(mem);
            }

            CodeTypeMember member;
            member = members.Find(IsCodeMemeberField);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_PRIVATE_FIELDS].Start);
            member = members.FindLast(IsCodeMemeberField);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_PRIVATE_FIELDS].End);

            member = members.Find(IsCodeMemeberProperty);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_PROPERTIES].Start);
            member = members.FindLast(IsCodeMemeberProperty);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_PROPERTIES].End);

            member = members.Find(IsCodeConstructor);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_CONSTRUCTORS].Start);
            member = members.FindLast(IsCodeConstructor);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_CONSTRUCTORS].End);

            member = members.Find(IsCodeMemberNestedTypes);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_NESTED_TYPES].Start);
            member = members.FindLast(IsCodeMemberNestedTypes);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_NESTED_TYPES].End);

            member = members.Find(IsCodeOverridenMethod);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_BASE_TYPE_RELATED].Start);
            member = members.FindLast(IsCodeOverridenMethod);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_BASE_TYPE_RELATED].End);

            member = members.Find(IsStatic);
            if (member != null)
                member.StartDirectives.Add(Regions[REGION_STATIC_MEMBERS].Start);
            member = members.FindLast(IsStatic);
            if (member != null)
                member.EndDirectives.Add(Regions[REGION_STATIC_MEMBERS].End);

            List<string> interfaces = new List<string>();

            members.FindAll(IsInterfaceImplementedMethod).ForEach(delegate(CodeTypeMember action)
            {
                CodeMemberMethod method = action as CodeMemberMethod;

                CodeTypeReference type = method.ImplementationTypes[0];

                string interfaceName = GetTypeName(type);

                string regionName = interfaceName + " members.";

                if (!interfaces.Contains(regionName))
                    interfaces.Add(regionName);
            });

            foreach (string interfaceName in interfaces)
            {
                member =
                    members.Find(
                        delegate(CodeTypeMember match)
                        {
                            return
                                IsInterfaceImplementedMethod(match) &&
                                GetTypeName((match as CodeMemberMethod).ImplementationTypes[0]).Equals(interfaceName);
                        });
                if (member != null)
                    member.StartDirectives.Add(Regions[interfaceName].Start);
                member =
                    members.FindLast(
                        delegate(CodeTypeMember match)
                        {
                            return
                                IsInterfaceImplementedMethod(match) &&
                                GetTypeName((match as CodeMemberMethod).ImplementationTypes[0]).Equals(interfaceName);
                        });
                if (member != null)
                    member.EndDirectives.Add(Regions[interfaceName].End);
            }


            members.FindAll(IsCodeMemberNestedTypes).ForEach(
                delegate(CodeTypeMember action)
                {
                    SetRegions(action as CodeTypeDeclaration);
                }
            );



            members.FindAll(IsCodeMemeberProperty).ForEach(SetSignatureRegion);
        }

        private static string GetTypeName(CodeTypeReference type)
        {
            string typeName = type.BaseType;
            for (int i = 0; i < type.TypeArguments.Count; i++)
            {
                if (i == 0)
                    typeName += "<";

                typeName += GetTypeName(type.TypeArguments[i]);

                if (i == type.TypeArguments.Count - 1)
                    typeName += ">";
            }
            return typeName;
        }

        public static bool IsCodeMemeberField(CodeTypeMember match)
        {
            return match is CodeMemberField && !IsStatic(match);
        }

        public static bool IsCodeMemeberProperty(CodeTypeMember match)
        {
            return match is CodeMemberProperty && !IsStatic(match);
        }

        public static bool IsCodeMemberNestedTypes(CodeTypeMember match)
        {
            return match is CodeTypeDeclaration && !IsStatic(match);
        }

        public static bool IsCodeConstructor(CodeTypeMember match)
        {
            return match is CodeConstructor && !IsStatic(match);
        }

        private static bool IsCodeMethod(CodeTypeMember match)
        {
            return typeof(CodeMemberMethod).Equals(match.GetType()) && !IsStatic(match);
        }

        private static bool IsStatic(CodeTypeMember match)
        {
            return (match.Attributes & MemberAttributes.Static) == MemberAttributes.Static;
        }

        private static bool IsCodeOverridenMethod(CodeTypeMember match)
        {
            return
                IsCodeMethod(match) &&
                ((match as CodeMemberMethod).Attributes & MemberAttributes.Override) == MemberAttributes.Override
                && !IsInterfaceImplementedMethod(match);
        }

        private static bool IsInterfaceImplementedMethod(CodeTypeMember match)
        {
            return
                   IsCodeMethod(match) &&
                   (match as CodeMemberMethod).ImplementationTypes != null && (match as CodeMemberMethod).ImplementationTypes.Count > 0;
        }

        public static void SetSignatureRegion(CodeTypeMember action)
        {

        }

        class CodeRegion
        {
            public readonly CodeRegionDirective Start;
            public readonly CodeRegionDirective End;

            public CodeRegion(string title)
            {
                Start = new CodeRegionDirective(CodeRegionMode.Start, title);
                End = new CodeRegionDirective(CodeRegionMode.End, title);
            }
        }

        class RegionsDictionary
        {
            private readonly Dictionary<string, CodeRegion> _dictionary;

            public RegionsDictionary()
            {
                _dictionary = new Dictionary<string, CodeRegion>();
            }

            public CodeRegion Add(string name, string title)
            {
                _dictionary.Add(name, new CodeRegion(title));
                return _dictionary[name];
            }

            public CodeRegion Add(string name)
            {
                return Add(name, name);
            }

            /*
                        public void Remove(string name)
                        {
                            _dictionary.Remove(name);
                        }
            */

            public CodeRegion this[string name]
            {
                get
                {
                    if (_dictionary.ContainsKey(name))
                        return _dictionary[name];
                    else
                        return Add(name);
                }
            }
        }

        /*
                class MemberSearchCriteria
                {
                    public readonly CodeTypeMember SearchObject;
                    public readonly object CriteriaValue;

                    public MemberSearchCriteria(CodeTypeMember searchObject, object criteriaValue)
                    {
                        SearchObject = searchObject;
                        CriteriaValue = criteriaValue;
                    }
                }
        */
    }
}
