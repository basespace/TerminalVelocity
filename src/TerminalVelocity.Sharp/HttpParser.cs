using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Illumina.TerminalVelocity
{
    public class HttpParser
    {
        private const string HttpStatusRegExString = @"HTTP/\d\.\d\s+(\d+)\s+.*";
        private static readonly Regex httpStatusRegEx = new Regex(HttpStatusRegExString,RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public static Dictionary<string, string> GetHttpHeaders(byte[] rawBytes, int bodyIndex, out int statusCode)
        {
            if (rawBytes.Length == 0)
            {
                throw new HttpException("response is empty");
            }
            string[] headers = Encoding.UTF8.GetString(rawBytes, 0, bodyIndex).Split(new [] { "\r\n", "\n" }, StringSplitOptions.None);
            if (headers.Length == 0)
            {
                throw new HttpException("response is empty");
            }
            var response = new Dictionary<string, string>();
            var match = httpStatusRegEx.Match(headers[0]);
            if (match.Length == 0)
            {
                throw new HttpException("response is empty");
            }

            statusCode = int.Parse(match.Groups[1].Value.Trim());

            for (int i = 1; i < headers.Count(); i++)
            {
                var headerTag = headers[i].IndexOf(':');
                response.Add(headers[i].Substring(0, headerTag), headers[i].Substring(headerTag + 1).Trim());
            }
            return response;
        }

        public static class HttpHeaders
        {
            public const string CacheControl = "Cache-Control";
            public const string ContentLength = "Content-Length";
            public const string ContentRange = "Content-Range";
            public const string Location = "Location";
        }
        
    }
}
