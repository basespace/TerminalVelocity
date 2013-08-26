using System;

namespace Illumina.TerminalVelocity
{
    public interface ISimpleHttpGetByRangeClient : IDisposable
    {
        SimpleHttpResponse Get(Uri uri, long start, long length);
        int ConnectionTimeout { get; set; }
    }
}