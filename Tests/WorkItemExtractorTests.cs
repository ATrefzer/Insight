using System.Linq;

using Insight.Shared;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    internal sealed class WorkItemExtractorTests
    {
        [Test]
        public void Extract()
        {
            var text = "s50xTm-444,s23dtm-ffff s50er-5lkll";
            var regex = @"[a-zA-Z]+[a-zA-Z0-9]+\-[0-9]+";

            var extractor = new WorkItemExtractor(regex);
            var result = extractor.Extract(text);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("S50XTM-444", result[0].Title);
            Assert.AreEqual("S50ER-5", result[1].Title);
        }

        [Test]
        public void Extract_NoRegEx_Returns_EmptyList()
        {
            var extractor = new WorkItemExtractor("");
            var result = extractor.Extract("XX-44");
            Assert.IsTrue(!result.Any());
        }
    }
}