using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Illumina.TerminalVelocity
{
    public static class Helpers
    {
        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        /// <summary>
        /// Returns the length of the resource by doing a quick get request
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static long GetContentLength(this Uri uri)
        {
            var retry = 0;
            var maxRetries = 5;
            var maxTimeOut = 600;

            while (retry++ <= maxRetries)
            {
                try
                {
                    var client = new SimpleHttpGetByRangeClient(uri);
                    var response = client.Get(uri, 0, 1);

                    if (response != null)
                    {
                        if (response.IsStatusCodeRedirect && !String.IsNullOrWhiteSpace(response.Location))
                        {
                            if (response.Location != uri.AbsoluteUri)
                            {
                                uri = new Uri(response.Location);
                                return GetContentLength(uri);
                            }
                            else
                            {
                                throw new ArgumentException("Supplied Url has no source");
                            }
                        }
                        else if (response.WasSuccessful && response.ContentRangeLength >= 0)
                        {
                            return response.ContentRangeLength.Value;
                        }
                        else if (response.StatusCode == 416)  //usually means zero byte file
                        {
                            return response.ContentLength;
                        }
                        else
                        {
                            throw new Exception("Response was not successful, status code: " + response.StatusCode);
                        }

                    }
                }
                catch
                {
                    if (retry > maxRetries)
                        throw;

                    var delay = (int) Math.Min(maxTimeOut, Math.Pow(retry, 5));
                    System.Threading.Thread.Sleep(delay);
                }
            }
            throw new Exception("Response was not successful");
        }
    }
}
