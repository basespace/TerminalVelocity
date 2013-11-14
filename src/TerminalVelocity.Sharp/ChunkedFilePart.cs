namespace Illumina.TerminalVelocity
{
    public class ChunkedFilePart
    {
        public long FileOffset { get; set; }

        public int Length { get; set; }

        public byte[] Content { get; set; }
    }
}
