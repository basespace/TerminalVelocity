using System.Text;
using NUnit.Framework;


namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class ByteArrayExtensionsTests
    {
        private static string TestArray = @"Hello

World";
        public static byte[] BODY_CRLF = new byte[] {13, 10, 13, 10};
        [Test]
        public void FindCrLfInByteArray()
        {
          
           byte[] input = Encoding.ASCII.GetBytes(TestArray);
           var index = input.IndexOf(BODY_CRLF);
            Assert.AreNotEqual(index, -1);
            Assert.AreEqual(index, 5);
        }

       
    }
}
