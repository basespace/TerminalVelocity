using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Illumina.TerminalVelocity
{
    public class SimpleHttpResponse
    {
        private const string ContentRange = @"bytes\s+(\d+)-(\d+)\/(\d+)";
        private static int[] REDIRECT_STATUSES = new int[] { 301, 307, 302, 303 };
        private static readonly Regex ContentRangeRegEx = new Regex(ContentRange, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
      
        public static ICollection<int> RetryableCodes = new ReadOnlyCollection<int>(new[] { 0, 413, 500, 503, 504 });

        public SimpleHttpResponse(){}
        
        public SimpleHttpResponse(int statusCode, Byte[] content, Dictionary<string, string> headers)
        {
            Content = content;
              StatusCode = statusCode;
            if (headers != null)
            {
                ContentLength = headers.ContainsKey(HttpParser.HttpHeaders.ContentLength)
                                    ? long.Parse(headers[HttpParser.HttpHeaders.ContentLength])
                                    : 0;

                if (headers.ContainsKey(HttpParser.HttpHeaders.ContentRange))
                {
                    var match = ContentRangeRegEx.Match(headers[HttpParser.HttpHeaders.ContentRange]);
                    ContentRangeStart = long.Parse(match.Groups[1].Value);
                    ContentRangeStop = long.Parse(match.Groups[2].Value);
                    ContentRangeLength = long.Parse(match.Groups[3].Value);
                }
                if (headers.ContainsKey(HttpParser.HttpHeaders.Location))
                {
                    Location = headers[HttpParser.HttpHeaders.Location];
                }
            }
            Headers = headers;

        }

        public int StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Location { get; set; }
        
        public long? ContentRangeStart { get; set; }
        public long? ContentRangeStop { get; set; }
        public long? ContentRangeLength { get; set; }
        public long ContentLength { get; set; }
        public byte[] Content { get; set; }

        public bool WasSuccessful
        {
            get { return  StatusCode >= 200 && StatusCode <= 300; }
        }

        public bool IsStatusCodeRedirect
        {
            get
            {
                if (!WasSuccessful)
                {
                    return REDIRECT_STATUSES.Contains(StatusCode);
                }
                return false;
            }
        }

        public bool IsStatusCodeRetryable
        {
            get
            {
                if (!WasSuccessful)
                {
                    return RetryableCodes.Contains(StatusCode);
                }

                return true;
            }
        }
    }
}
