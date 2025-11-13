using System;
using System.IO;
using System.Reflection;
using System.Text;
using Insight.GitProvider;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Decoder = Insight.GitProvider.Decoder;

namespace Tests
{
    [TestFixture]
    internal sealed class DecoderTests
    {
        [Test]
        public void DecodeEscapedPaths()
        {
            const string escaped = @"file_with_umlauts_\303\244\303\266\303\274.txt";
            const string expected = "file_with_umlauts_äöü.txt";

            var decoded = Decoder.DecodeEscapedBytes(escaped);
            Assert.That(decoded, Is.EqualTo(expected));
        }

        [Test]
        public void FixEncoding()
        {
            // 1252
            const string encoded = "Ã¤Ã¶Ã¼";
            const string expected = "äöü";

            // In .net 7 ensure 1252 is available.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var decoded = Decoder.Decode1252(encoded);
            Assert.That(decoded, Is.EqualTo(expected));
        }

        // Accesses file system
        [Test]
        public void FixEncoding2()
        {
            // We have to tell about the process output encoding.
            // Much more reasonable than starting to recode strings

            var assembly = Assembly.GetAssembly(GetType());
            var directory = new FileInfo(assembly.Location).Directory.FullName;
            var resources = Path.Combine(directory, @"..\Tests\Resources");


            const string expected = "äöü";
            var cli = new GitCommandLine(Path.Combine(directory, ".."));
            var result = cli.Log(Path.Combine(resources, "file_with_umlauts_äöü.cs"));

            ClassicAssert.IsTrue(result.Contains(expected));

            var bytes = new byte[] { 0xc3, 0xa4, 0xc3, 0xb6, 0xc3, 0xbc };
            var str = Encoding.UTF8.GetString(bytes);
            Assert.That(str, Is.EqualTo(expected));


        }
    }
}