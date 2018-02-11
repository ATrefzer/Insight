using System;
using System.Runtime.Serialization;

namespace Insight.Shared.Exceptions
{
    [Serializable]
    public class ProviderException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ProviderException()
        {
        }

        public ProviderException(string message) : base(message)
        {
        }

        public ProviderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ProviderException(
                SerializationInfo info,
                StreamingContext context) : base(info, context)
        {
        }
    }
}