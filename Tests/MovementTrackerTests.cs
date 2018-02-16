using Insight.Shared.Model;
using Insight.SvnProvider;

using NUnit.Framework;

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

            var rev100 = new NumberId(100);
            var rev80 = new NumberId(80);
            var rev20 = new NumberId(20);
            var rev0 = new NumberId(0);

            // Track all renamings
            tracker.Add(rev100, id4, rev80, id3);
            tracker.Add(rev80, id3, rev20, id2);
            tracker.Add(rev20, id2, rev0, id1);

            Assert.AreEqual(id4, tracker.GetLatestId(id1, rev0));
            Assert.AreEqual(id4, tracker.GetLatestId(id2, rev20));
            Assert.AreEqual(id4, tracker.GetLatestId(id3, rev80));
            Assert.AreEqual(id4, tracker.GetLatestId(id4, rev100));

            // Arbitrary commit maps id correclty
            Assert.AreEqual(id4, tracker.GetLatestId(id2, new NumberId(21)));
        }

        [Test]
        public void NoEntryMapsToLatest()
        {
            var tracker = new MovementTracking();

            var rev0 = new NumberId(0);
            var id1 = new StringId("name1");
            Assert.AreEqual(id1, tracker.GetLatestId(id1, rev0));
        }
    }
}