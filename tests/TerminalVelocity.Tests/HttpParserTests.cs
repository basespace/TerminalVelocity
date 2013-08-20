using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;


namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class HttpParserTests
    {
        public const string SampleHttpResponse = @"HTTP/1.1 200 OK
Cache-Control: public, max-age=245
Content-Length: 21
Content-Type: application/xml; charset=utf-8
Expires: Wed, 06 Mar 2013 01:38:44 GMT
Last-Modified: Wed, 06 Mar 2013 01:33:44 GMT
Age: 806
Vary: *
Server: Microsoft-IIS/7.0
X-WNS-Expires: Wed, 06 Mar 2013 07:33:44 GMT
X-ABF-EndPoint: NCUS-00
X-Deployment: f9f33b8adfce41419a245661a81e54c1
X-AspNet-Version: 4.0.30319
X-Powered-By: ASP.NET
Date: Wed, 06 Mar 2013 01:34:38 GMT

<badge value=""none""/>";

        private const string SampleHttpRequest = @"GET {0} HTTP/1.1
Host: {1}
Connection: keep-alive
Cache-Control: no-cache
User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22
Accept: */*
Accept-Encoding: gzip
Accept-Language: en-US,en;q=0.8
Accept-Charset: utf-8;q=0.7,*;q=0.3
Range: bytes={2}-{3}

";

        
        private  const string SampleRedirectResponse = @"HTTP/1.1 301 Moved Permanently
X-Powered-By: PHP/5.4.13
Set-Cookie: tinyUUID=14bec0ab026ecebe48cd53f4; expires=Sat, 22-Mar-2014 05:28:37 GMT; path=/; domain=.tinyurl.com
Location: https://1000genomes.s3.amazonaws.com/release/20110521/ALL.chr9.phase1_release_v3.20101123.snps_indels_svs.genotypes.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1427000627&Signature=PFrSu5ZXoUl17mCRg3HwDORfkg4%3D
X-tiny: db 0.040312051773071
Content-type: text/html
Content-Length: 0
Connection: close
Date: Fri, 22 Mar 2013 05:28:37 GMT
Server: TinyURL/1.6

";
        [Test]
        public void Redirect301Request()
        {
             byte[] input = Encoding.ASCII.GetBytes(SampleRedirectResponse);
            int statusCode;
            var headers =HttpParser.GetHttpHeaders(input, input.IndexOf(ByteArrayExtensionsTests.BODY_CRLF), out statusCode);
            var response = new SimpleHttpResponse(statusCode, null, headers);
            Assert.AreEqual(statusCode, 301);
            Assert.AreEqual(response.Location, @"https://1000genomes.s3.amazonaws.com/release/20110521/ALL.chr9.phase1_release_v3.20101123.snps_indels_svs.genotypes.vcf.gz?AWSAccessKeyId=AKIAIYDIF27GS5AAXHQQ&Expires=1427000627&Signature=PFrSu5ZXoUl17mCRg3HwDORfkg4%3D");
        }

        [Test]
        public void CanWeCreateRequestCorrectly()
        {
            Uri uri = new Uri(@"http://google.com");
            var request = SimpleHttpGetByRangeClient.BuildHttpRequest(uri, 0, 100);
            var expected = string.Format(SampleHttpRequest, uri, uri.Host, 0, 99);
            Assert.AreEqual(request, expected);
        }

        [Test]
        public void CanWeParseHttpStatusCode()
        {
            byte[] input = Encoding.ASCII.GetBytes(SampleHttpResponse);
            int statusCode;
            var result = HttpParser.GetHttpHeaders(input, input.IndexOf(ByteArrayExtensionsTests.BODY_CRLF), out statusCode);
            Assert.AreEqual(statusCode, 200);
        }

        [Test]
        public void CanWeParseHttpStatusAndConvertToInt()
        {
            byte[] input = Encoding.ASCII.GetBytes(SampleHttpResponse);
            int statusCode;
            var result = HttpParser.GetHttpHeaders(input, input.IndexOf(ByteArrayExtensionsTests.BODY_CRLF), out statusCode);
            var response = new SimpleHttpResponse(statusCode, input, result);
            Assert.True(response.StatusCode == 200);
            Assert.True(response.WasSuccessful);
            Assert.True(response.IsStatusCodeRetryable);
        }

       [
    TestCase(200,  true, true),
        TestCase(201, true, true),
        TestCase(400, false, false),
        TestCase(413, false, true),
        TestCase(500, false, true),
        TestCase(503, false, true),
         TestCase(504, false, true)
        ]
        public void ValidateStatusCodes(  int statusCode, bool isSuccessful, bool isRetryable)
       {
          
            var response = new SimpleHttpResponse(statusCode, null, new Dictionary<string,string>());
             Assert.True(response.StatusCode == statusCode);
            Assert.True(response.WasSuccessful == isSuccessful);
            Assert.True(response.IsStatusCodeRetryable == isRetryable);
        }


        [Test]
        public void CanWeParseRandomHeaders()
        {
            byte[] input = Encoding.ASCII.GetBytes(SampleHttpResponse);
            int statusCode;
            var result = HttpParser.GetHttpHeaders(input, input.IndexOf(ByteArrayExtensionsTests.BODY_CRLF),out statusCode);

            Assert.True(result.Count == 14);
            Assert.True(result["Date"] == "Wed, 06 Mar 2013 01:34:38 GMT");
        }
    }
}
