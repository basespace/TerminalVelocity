using System;
using System.Runtime.Serialization;

namespace Illumina.TerminalVelocity
{
  

    [Serializable]
    public class SimpleHttpClientException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SimpleHttpClientException(SimpleHttpResponse response)
        {
            HttpResponse = response;
        }

        public SimpleHttpClientException(SimpleHttpResponse response, string message) : base(message)
        {
            HttpResponse = response;
        }

        public SimpleHttpClientException(SimpleHttpResponse response, string message, Exception inner) : base(message, inner)
        {
            HttpResponse = response;
        }

        protected SimpleHttpClientException(SimpleHttpResponse response,
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            HttpResponse = response;
        }

        public SimpleHttpResponse HttpResponse { get; set; }
    }
}
