using System;
using System.CodeDom;
using System.Reflection;
using WXML.CodeDom;

namespace WXML.Model.Descriptors
{
    public class TypeDefinition
    {
        private readonly string _id;
        private readonly string _userType;
        private readonly Type _clrType;
        private readonly EntityDefinition _entity;
        private readonly UserTypeHintFlags? _userTpeHint;
        private CodeTypeReference _tr;
        private Type _cachedUserType;
        private bool _checked;
        #region Ctors

        public TypeDefinition(CodeTypeReference tr)
        {
            _tr = tr;
        }

        public TypeDefinition(string id, string typeName, bool treatAsUserType)
            : this(id, typeName, null, treatAsUserType, null)
        {
        }

        public TypeDefinition(string id, string typeName)
            : this(id, typeName, null, false, null)
        {
        }

        public TypeDefinition(string id, EntityDefinition entity)
            : this(id, null, entity, false, null)
        {
        }

        public TypeDefinition(string id, Type type) : this(id, null, null, false, null)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            _clrType = type;
        }

        public TypeDefinition(string id, string typeName, UserTypeHintFlags? userTypeHint)
            : this(id, typeName, null, true, userTypeHint)
        {
            
        }

        protected TypeDefinition(string id, string typeName, EntityDefinition entity, bool treatAsUserType, UserTypeHintFlags? userTypeHint)
        {
            _id = id;
            if(!string.IsNullOrEmpty(typeName))
                if (treatAsUserType)
                {
                    _userType = typeName;
                    _userTpeHint = userTypeHint;
                }
                else
                {
                    _clrType = GetTypeByName(typeName);
                }
            _entity = entity;
        }

        #endregion

        public string Identifier
        {
            get { return _id; }
        }

        public Type ClrType
        {
            get
            {
                if (_clrType == null)
                    throw new InvalidOperationException("Valid only for ClrType. Use 'IsClrType' at first.");
                return _clrType;
            }
        }

        public EntityDefinition Entity
        {
            get { return _entity; }
        }

        public string GetTypeName(WXMLCodeDomGeneratorSettings settings)
        {
            if (IsClrType)
                return _clrType.FullName;
            if (IsUserType)
                return _userType;
            return new WXMLCodeDomGeneratorNameHelper(settings).GetEntityClassName(_entity, true);
        }

        public bool IsClrType
        {
            get
            {
                return _clrType != null;
            }
        }

        public bool IsUserType
        {
            get
            {
                return _clrType == null && !string.IsNullOrEmpty(_userType);
            }
        }

        public bool IsEntityType
        {
            get
            {
                return _entity != null;
            }
        }

        public bool IsValueType
        {
            get
            {
                return
                    (!IsEntityType) &&
                    (
                        ( IsClrType && typeof(ValueType).IsAssignableFrom(ClrType) )
                         ||
                        (IsUserType && UserTypeHint.HasValue && UserTypeHint.Value != UserTypeHintFlags.None)
                    );
                
            }
        }

        public bool IsEnum
        {
            get
            {
                return (IsValueType) &&
                       (
                           (IsClrType && ClrType.IsEnum)
                           ||
                           (IsUserType && UserTypeHint.HasValue &&
                            ((UserTypeHint.Value & UserTypeHintFlags.Enum) == UserTypeHintFlags.Enum))
                       );
            }
        }

        public bool IsNullableType
        {
            get
            {
                return (IsValueType) && 
                (
                    (IsClrType && ClrType.IsGenericType && typeof(Nullable<>).Equals(ClrType.GetGenericTypeDefinition()))
                    || (IsUserType && UserTypeHint.HasValue && ((UserTypeHint.Value & UserTypeHintFlags.Nullable) == UserTypeHintFlags.Nullable))
                );
            }
        }

        public UserTypeHintFlags? UserTypeHint
        {
            get { return _userTpeHint; }
        }

        public override string ToString()
        {
            if (IsClrType)
                return _clrType.FullName;
            if (IsUserType)
                return _userType;

            return _entity.Identifier;
        }
        public Type GetClrType()
        {
            if (_clrType != null)
                return _clrType;

            if (!string.IsNullOrEmpty(_userType))
            {
                if (_cachedUserType == null && !_checked)
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        Type type = assembly.GetType(_userType, false, true);
                        if (type != null)
                        {
                            _cachedUserType = type;
                            break;
                        }
                    }
                    _checked = true;
                }

                return _cachedUserType;
            }

            return null;
        }
        public static Type GetTypeByName(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName, false, true);
                if (type != null)
                    return type;
            }
            throw new TypeLoadException(String.Format("Cannot find type by given name '{0}'", typeName));
        }

        public CodeTypeReference ToCodeType(WXMLCodeDomGeneratorSettings settings)
        {
            if (_tr != null)
                return _tr;

            TypeDefinition propertyTypeDesc = this;

            var t = new CodeTypeReference(propertyTypeDesc.IsEntityType
                  ? new WXMLCodeDomGeneratorNameHelper(settings).GetEntityClassName(propertyTypeDesc.Entity, true)
                  : propertyTypeDesc.GetTypeName(settings));

            //if (IsUserType && (UserTypeHint & UserTypeHintFlags.Interface) == UserTypeHintFlags.Interface)
            //    t.Is

            return t;
        }
    }

    [Flags]
    public enum UserTypeHintFlags
    {
        None = 0x0000,
        Enum = 0x0001,
        ValueType = 0x0002,
        Nullable = 0x0004,
        Interface = 8
    }
}
