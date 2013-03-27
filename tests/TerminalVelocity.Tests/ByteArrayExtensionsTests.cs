using System.Text;
using Xunit;

namespace Illumina.TerminalVelocity.Tests
{
    public class ByteArrayExtensionsTests
    {
        private static string TestArray = @"Hello

World";
        public static byte[] BODY_CRLF = new byte[] {13, 10, 13, 10};
        [Fact]
        public void FindCrLfInByteArray()
        {
          
           byte[] input = Encoding.ASCII.GetBytes(TestArray);
           var index = input.IndexOf(BODY_CRLF);
            Assert.NotEqual(index, -1);
            Assert.Equal(index, 5);
        }

       
    }
}
