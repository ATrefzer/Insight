using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Insight.SvnProvider;
using Insight.Shared.Model;

namespace Tests
{
    [TestFixture]
    internal sealed class MovementTrackingTests
    {
        [Test]
        public void CanMapAllOlderFIleIdsToLatest()
        {
            // From latest to oldest revision
            // commit revision: old -> new

            // 100 name3 -> name4
            // ...
            // 080 name2 -> name3
            // ...
            // 021 Modified name2
            // 020 name1 -> name2
            // ...
            // 000 Added name1

            var tracker = new MovementTracking();

            var id1 = new StringId("name1");
            var id2 = new StringId("name2");
            var id3 = new StringId("name3");
            var id4 = new StringId("name4");

            // Track all renamings
            tracker.Add(100, id4, 80, id3);
            tracker.Add(80, id3, 20, id2);
            tracker.Add(20, id2, 0, id1);

            

            Assert.AreEqual(id4, tracker.GetLatestId(id1, 0));
            Assert.AreEqual(id4, tracker.GetLatestId(id2, 20));
            Assert.AreEqual(id4, tracker.GetLatestId(id3, 80));
            Assert.AreEqual(id4, tracker.GetLatestId(id4, 100));

            // Arbitrary commit maps id correclty
            Assert.AreEqual(id4, tracker.GetLatestId(id2, 21));
        }

        [Test]
        public void NoEntryMapsToLatest()
        {
            var tracker = new MovementTracking();

            var id1 = new StringId("name1");
            Assert.AreEqual(id1, tracker.GetLatestId(id1, 0));
        }
    }
}
