using System;

namespace WXML.Model
{
    [global::System.Serializable]
    public class WXMLException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public WXMLException() { }
        public WXMLException(string message) : base(message) { }
        public WXMLException(string message, Exception inner) : base(message, inner) { }
        protected WXMLException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }


    [global::System.Serializable]
    public class WXMLParserException : WXMLException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public WXMLParserException() { }
        public WXMLParserException(string message) : base(message) { }
        public WXMLParserException(string message, Exception inner) : base(message, inner) { }
        protected WXMLParserException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
