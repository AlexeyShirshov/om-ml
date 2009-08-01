using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model
{
    [global::System.Serializable]
    public class OrmCodeGenException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public OrmCodeGenException() { }
        public OrmCodeGenException(string message) : base(message) { }
        public OrmCodeGenException(string message, Exception inner) : base(message, inner) { }
        protected OrmCodeGenException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }


    [global::System.Serializable]
    public class OrmXmlParserException : OrmCodeGenException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public OrmXmlParserException() { }
        public OrmXmlParserException(string message) : base(message) { }
        public OrmXmlParserException(string message, Exception inner) : base(message, inner) { }
        protected OrmXmlParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
