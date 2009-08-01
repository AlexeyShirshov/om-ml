using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model.Descriptors
{
    public class LinqSettingsDescriptor
    {

        public bool Enable
        {
            get;
            set;
        }

        public string ContextName
        {
            get;
            set;
        }

        public string FileName
        {
            get;
            set;
        }

        public ContextClassBehaviourType? ContextClassBehaviour
        {
            get;
            set;
        }

    }

    public enum ContextClassBehaviourType
    {
        BaseClass,
        PartialClass,
        BasePartialClass
    }
}
