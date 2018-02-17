using System;

using Insight.GitProvider;
using Insight.Shared.Model;

using NUnit.Framework;

namespace Tests
{
    /// <summary>
    /// All tests read like this:
    /// Commits happened from bottom to top, but process top down from the newest to the oldest commit.
    /// </summary>
    [TestFixture]
    internal sealed class MovementTrackerTests
    {
        private MovementTracker _tracker;


        [Test]
        public void CreateFileOnLocationOfDeletedFile()
        {
            var cs3 = EmulateCommit_Add("location");

            var id = cs3.Id.ToString();

            var cs2 = EmulateCommit_Delete("location");

            Assert.AreNotEqual(id, cs2.Id.ToString());
            var newIdd = cs2.Id.ToString();

            var cs0 = EmulateCommit_Edit("location");

            Assert.AreEqual(newIdd, cs0.Id.ToString());
        }


        [Test]
        public void CyclicRenaming()
        {
            var cs2 = EmulateCommit_Rename("location_yesterday", "location_now");

            var id = cs2.Id.ToString();

            var cs1 = EmulateCommit_Rename("location_now", "location_yesterday");

            Assert.AreEqual(id, cs1.Id.ToString());

            var cs0 = EmulateCommit_Rename("location_yesterday", "location_now");

            Assert.AreEqual(id, cs0.Id.ToString());
        }


        [Test]
        public void DeletedItemIsTracked()
        {
            var cs2 = EmulateCommit_Delete("location");
            var id = cs2.Id.ToString();

            var cs0 = EmulateCommit_Edit("location");
            Assert.AreEqual(id, cs0.Id.ToString());
        }

        [Test]
        public void InvalidArguments_NonRenameHasPreviousServerPath()
        {
            var tracker = new MovementTracker();

            var ci = new ChangeItem
                     {
                             Kind = KindOfChange.Edit
                     };

            Assert.Throws<ArgumentException>(() => tracker.SetId(ci, "unexpected"));
        }

        public void InvalidArguments_RenameHasNoPreviousServerPath()
        {
            var tracker = new MovementTracker();

            var ci = new ChangeItem
                     {
                             Kind = KindOfChange.Rename
                     };

            Assert.Throws<ArgumentException>(() => tracker.SetId(ci, null));
        }


        [Test]
        public void ItemSeenFirst_GetsUniqueId([Values(
                                                       KindOfChange.Add,
                                                       KindOfChange.Rename,
                                                       KindOfChange.Delete,
                                                       KindOfChange.Edit
                                               )]
                                               KindOfChange kind)
        {
            var tracker = new MovementTracker();

            var previousServerPath = kind == KindOfChange.Rename ? "previous_path" : null;
            tracker.BeginChangeSet();

            var ci = new ChangeItem
                     {
                             ServerPath = "current_path",
                             Kind = kind
                     };

            tracker.SetId(ci, previousServerPath);

            var id = ci.Id.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(id));
            Assert.IsTrue(Guid.TryParse(id, out var uuid));
        }


        [Test]
        public void Regression_DuplicatedKey()
        {
            // Wtf
            var cs3 = EmulateCommit_Delete("bmp");
            var id = cs3.Id.ToString();

            var cs2 = EmulateCommit_Add("bmp");
            Assert.AreEqual(id, cs2.Id.ToString());

            var cs1 = EmulateCommit_Rename("bmp", "ico");
            Assert.AreNotEqual(id, cs1.Id.ToString());
            var newIdd = cs1.Id.ToString();

            var cs0 = EmulateCommit_Add("bmp");
            Assert.AreEqual(newIdd, cs0.Id.ToString());
        }

        [Test]
        public void Regression_DuplicatedKey2()
        {
            // Wtf
            var cs3 = EmulateCommit_Add("file");
            var id = cs3.Id.ToString();

            // Move file away so we can create a new version at this location in c3.
            var cs2 = EmulateCommit_Rename("file", "somewhere_else");
            Assert.AreNotEqual(id, cs2.Id.ToString());
            var newId = cs2.Id.ToString();

            var cs1 = EmulateCommit_Add("file");
            Assert.AreEqual(newId, cs1.Id.ToString());
        }

        [Test]
        public void RenamedItemIsTracked()
        {
            var cs2 = EmulateCommit_Rename("location_yesterday", "location_now");

            var id = cs2.Id.ToString();

            var cs1 = EmulateCommit_Edit("location_yesterday");

            Assert.AreEqual(id, cs1.Id.ToString());

            var cs0 = EmulateCommit_Rename("location_very_old", "location_yesterday");

            Assert.AreEqual(id, cs0.Id.ToString());
        }


        [SetUp]
        public void SetUp()
        {
            _tracker = new MovementTracker();
        }

        private ChangeItem EmulateCommit(KindOfChange kind, string currentServerPath, string previousServerPath)
        {
            // Note arg order changed here!
            var ci = new ChangeItem
                     {
                             ServerPath = currentServerPath,
                             Kind = kind
                     };

            var cs = new ChangeSet();
            cs.Id = new StringId(Guid.NewGuid().ToString());
            cs.Items.Add(ci);

            _tracker.BeginChangeSet();
            _tracker.SetId(ci, previousServerPath);
            _tracker.EndChangeSet();
            return ci;
        }

        private ChangeItem EmulateCommit_Add(string currentServerPath)
        {
            return EmulateCommit(KindOfChange.Add, currentServerPath, null);
        }

        private ChangeItem EmulateCommit_Delete(string currentServerPath)
        {
            return EmulateCommit(KindOfChange.Delete, currentServerPath, null);
        }


        private ChangeItem EmulateCommit_Edit(string currentServerPath)
        {
            return EmulateCommit(KindOfChange.Edit, currentServerPath, null);
        }

        private ChangeItem EmulateCommit_Rename(string fromServerPath, string toServerPath)
        {
            return EmulateCommit(KindOfChange.Rename, toServerPath, fromServerPath);
        }
    }
}