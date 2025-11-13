using System.Linq;

using Insight.Shared;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests
{
    [TestFixture]
    internal sealed class WorkItemExtractorTests
    {
        [Test]
        public void Extract()
        {
            const string text = "s50xTm-444,s23dtm-ffff s50er-5lkll";
            const string regex = @"[a-zA-Z]+[a-zA-Z0-9]+\-[0-9]+";

            var extractor = new WorkItemExtractor(regex);
            var result = extractor.Extract(text);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Title, Is.EqualTo("S50XTM-444"));
            Assert.That(result[1].Title, Is.EqualTo("S50ER-5"));
        }

        [Test]
        public void Extract_NoRegEx_Returns_EmptyList()
        {
            var extractor = new WorkItemExtractor("");
            var result = extractor.Extract("XX-44");
            ClassicAssert.IsTrue(!result.Any());
        }
    }
}