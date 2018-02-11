using System.Collections.Generic;

using Insight.Shared.Model;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    internal sealed class StringIdTests
    {
        public void CanBeUsedAsKey()
        {
            var id1 = new StringId("a");
            var id2 = new StringId("a");

            var hash = new HashSet<Id>();
            hash.Add(id1);
            hash.Add(id2);

            Assert.AreEqual(1, hash.Count);
        }

        [Test]
        public void CanBeUsedAsKey_WorkItem()
        {
            var id1 = new StringId("a");
            var id2 = new StringId("a");

            var w1 = new WorkItem(id1);
            var w2 = new WorkItem(id2);

            var hash = new HashSet<WorkItem>();
            hash.Add(w1);
            hash.Add(w2);

            Assert.AreEqual(1, hash.Count);
        }
    }
}