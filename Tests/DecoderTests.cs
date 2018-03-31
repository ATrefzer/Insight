using System;
using System.IO;
using System.Text;

using NUnit.Framework;

using Decoder = Insight.GitProvider.Decoder;

namespace Tests
{
    [TestFixture]
    sealed class DecoderTests
    {
        [Test]
        public void DecodeEscaptedPaths()
        {
            var escaped = @"file_with_umlauts_\303\244\303\266\303\274.txt";
            var expected = "file_with_umlauts_äöü.txt";

            var decoded = Decoder.DecodeEscapedBytes(escaped);
            Assert.AreEqual(expected, decoded);
        }

        [Test]
        public void FixEncoding()
        {
            // 1252
            var encoded = @"Ã¤Ã¶Ã¼";
            var expected = "äöü";

            var decoded = Decoder.DecodeUtf8(encoded);
            Assert.AreEqual(expected, decoded);
        }

        [Test]
        public void FixEncoding2()
        {
            var bytes = new Byte[] { 0xc3, 0xa4, 0xc3, 0xb6, 0xc3, 0xbc };
            var str = Encoding.UTF8.GetString(bytes);

            // 1252
            var encoded = @"Ã¤Ã¶Ã¼";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(encoded));
            var expected = "äöü";

            // TODO Why?
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var decoded = reader.ReadLine();
                Assert.AreEqual(expected, decoded);
            }
        }
    }
}