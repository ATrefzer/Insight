using System;
using System.IO;
using System.Reflection;
using System.Text;
using Insight.GitProvider;
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

        // Accesses file system
        [Test]
        public void FixEncoding2()
        {
            // We have to tell about the process output encoding.
            // Much more reasonable than starting to recode strings

            var assembly = Assembly.GetAssembly(this.GetType());
            var directory = new FileInfo(assembly.Location).Directory.FullName;
            var resources = Path.Combine(directory, @"..\Tests\Resources");


            var expected = "äöü";
            var cli = new GitCommandLine(Path.Combine(directory, ".."));
            var result = cli.Log(Path.Combine(resources, "file_with_umlauts_äöü.cs"));

            Assert.IsTrue(result.Contains(expected));

            var bytes = new Byte[] { 0xc3, 0xa4, 0xc3, 0xb6, 0xc3, 0xbc };
            var str = Encoding.UTF8.GetString(bytes);
            Assert.AreEqual(expected, str);


        }
    }
}