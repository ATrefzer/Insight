using Insight.GitProvider;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    class DecoderTests
    {
        [Test]
        public void DecodeEscaptedPaths()
        {
            var escaped = @"file_with_umlauts_\303\244\303\266\303\274.txt";
            var expected = "file_with_umlauts_äöü.txt";

            var decoded = Decoder.Decode(escaped);
            Assert.AreEqual(expected, decoded);
        }
    }
}