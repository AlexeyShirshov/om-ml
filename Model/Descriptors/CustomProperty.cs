using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WXML.Model.Descriptors
{
    public class CustomPropertyDefinition : PropertyDefinition
    {
        public class Body
        {
            public Body(string propertyName)
            {
                this.PropertyName = propertyName;
            }

            public Body(string vbCode, string csCode)
            {
                this.VBCode = vbCode;
                this.CSCode = csCode;
            }

            public string PropertyName { get; protected set; }
            public string VBCode { get; set; }
            public string CSCode { get; set; }
        }

        private Body _getBody;
        private Body _setBody;

        public CustomPropertyDefinition(string propertyName, TypeDefinition type, Body getBody, Body setBody,
            EntityDefinition entity)
        {
            Name = propertyName;
            PropertyType = type;
            _getBody = getBody;
            _setBody = setBody;
            Entity = entity;
        }

        public override SourceFragmentDefinition SourceFragment
        {
            get
            {
                return null;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        protected override PropertyDefinition _Clone()
        {
            throw new NotImplementedException();
        }

        public override bool HasMapping
        {
            get { return false; }
        }

        public Body GetBody
        {
            get
            {
                return _getBody;
            }
            set
            {
                _getBody = value;
            }
        }

        public Body SetBody
        {
            get
            {
                return _setBody;
            }
            set
            {
                _setBody = value;
            }
        }
    }
}
