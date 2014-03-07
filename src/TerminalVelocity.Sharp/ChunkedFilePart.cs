using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Illumina.TerminalVelocity
{
    public class ChunkedFilePart
    {
        public long FileOffset { get; set; }

        public int Length { get; set; }

        public byte[] Content { get; set; }

        public int Chunk { get; set; }
    }
}
