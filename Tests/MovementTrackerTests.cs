using System;
using System.Collections.Generic;

using Insight.Shared.Model;
using Insight.Shared.VersionControl;

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
            // Act
            StartChangeSet();
            var cs2 = Track_Add("location");
            EndChangeSet();

            StartChangeSet();
            var cs1 = Track_Delete("location");
            EndChangeSet();

            StartChangeSet();
            var cs0 = Track_Edit("location");
            EndChangeSet();

            // Assert
            var id = cs2.Id.ToString();
            Assert.AreNotEqual(id, cs1.Id.ToString());
            var newIdd = cs1.Id.ToString();

            Assert.AreEqual(newIdd, cs0.Id.ToString());
        }

        // TODO 
        // add as copy
        // multiple copies as add


        [Test]
        public void CyclicRenaming_KeepsId()
        {
            StartChangeSet();
            var cs2 = Track_Rename("location_yesterday", "location_now");
            EndChangeSet();

            StartChangeSet();
            var cs1 = Track_Rename("location_now", "location_yesterday");
            EndChangeSet();

            StartChangeSet();
            var cs0 = Track_Rename("location_yesterday", "location_now");
            EndChangeSet();

            var id = cs2.Id.ToString();
            Assert.AreEqual(id, cs1.Id.ToString());
            Assert.AreEqual(id, cs0.Id.ToString());
        }


        [Test]
        public void DeletedItemIsTracked()
        {
            StartChangeSet();
            var cs2 = Track_Delete("location");
            EndChangeSet();

            StartChangeSet();
            var cs0 = Track_Edit("location");
            EndChangeSet();

            var id = cs2.Id.ToString();
            Assert.AreEqual(id, cs0.Id.ToString());
        }

        [Test]
        public void InvalidArguments_NonRenameHasPreviousServerPath()
        {
            var tracker = new MovementTracker();

            var ci = new ChangeItem
                     {
                             Kind = KindOfChange.Edit,
                             FromServerPath = "unexpected"
                     };

            Assert.Throws<ArgumentException>(() => tracker.TrackId(ci));
        }

        public void InvalidArguments_RenameHasNoPreviousServerPath()
        {
            var tracker = new MovementTracker();

            var ci = new ChangeItem
                     {
                             Kind = KindOfChange.Rename
                     };

            Assert.Throws<ArgumentException>(() => tracker.TrackId(ci));
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
            tracker.BeginChangeSet(new ChangeSet());

            var ci = new ChangeItem
                     {
                             ServerPath = "current_path",
                             FromServerPath = previousServerPath,
                             Kind = kind
                     };

            tracker.BeginChangeSet(new ChangeSet());
            tracker.TrackId(ci);
            tracker.ApplyChangeSet(new List<ChangeItem>());

            var id = ci.Id.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(id));
            Assert.IsTrue(Guid.TryParse(id, out var uuid));
        }


        [Test]
        public void Regression_DuplicatedKey()
        {
            StartChangeSet();
            var cs3 = Track_Delete("bmp");
            EndChangeSet();

            StartChangeSet();
            var cs2 = Track_Add("bmp");
            EndChangeSet();

            StartChangeSet();
            var cs1 = Track_Rename("bmp", "ico");
            EndChangeSet();

            StartChangeSet();
            var cs0 = Track_Add("bmp");
            EndChangeSet();

            var id = cs3.Id.ToString();
            Assert.AreEqual(id, cs2.Id.ToString());
            Assert.AreNotEqual(id, cs1.Id.ToString());
            var newIdd = cs1.Id.ToString();
            Assert.AreEqual(newIdd, cs0.Id.ToString());
        }

        [Test]
        public void Regression_DuplicatedKey2()
        {
            // Act
            StartChangeSet();
            var cs3 = Track_Add("file");
            EndChangeSet();

            StartChangeSet();

            // Move file away so we can create a new version at this location in c3.
            var cs2 = Track_Rename("file", "somewhere_else");
            EndChangeSet();

            StartChangeSet();
            var cs1 = Track_Add("file");
            EndChangeSet();

            // Assert
            var id = cs3.Id.ToString();
            Assert.AreNotEqual(id, cs2.Id.ToString());
            var newId = cs2.Id.ToString();
            Assert.AreEqual(newId, cs1.Id.ToString());
        }

        [Test]
        public void RenamedItemIsTracked()
        {
            StartChangeSet();
            var cs2 = Track_Rename("location_yesterday", "location_now");
            EndChangeSet();

            StartChangeSet();
            var cs1 = Track_Edit("location_yesterday");
            EndChangeSet();

            StartChangeSet();
            var cs0 = Track_Rename("location_very_old", "location_yesterday");
            EndChangeSet();

            // Assert
            var id = cs2.Id.ToString();
            Assert.AreEqual(id, cs1.Id.ToString());
            Assert.AreEqual(id, cs0.Id.ToString());
        }


        [SetUp]
        public void SetUp()
        {
            _tracker = new MovementTracker();
        }

        // TODO
        [Test]
        public void Svn_AddDelete_IsReplaceByMove1()
        {
            StartChangeSet();
            var cs2 = Track_Delete("location_yesterday");
            var cs1 = Track_Add("location_yesterday", "location_now");
            var cs = EndChangeSet();
        }

        //    var cs0 = Track_Edit("location_yesterday");
        //    _tracker.ApplyChangeSet(result);

        //    var id = cs2.Id.ToString();

        //    var cs1 = TrackCommit_Rename("location_now", "location_yesterday");

        //    Assert.AreEqual(id, cs1.Id.ToString());

        //    var cs0 = TrackCommit_Rename("location_yesterday", "location_now");

        //    Assert.AreEqual(id, cs0.Id.ToString());
        //}

        private List<ChangeItem> EndChangeSet()
        {
            var result = new List<ChangeItem>();
            _tracker.ApplyChangeSet(result);
            return result;
        }

        private void StartChangeSet()
        {
            _tracker.BeginChangeSet(new ChangeSet());
        }

        private ChangeItem Track_Add(string currentServerPath, string previousServerPath = null)
        {
            return TrackOperation(KindOfChange.Add, currentServerPath, previousServerPath);
        }

        private ChangeItem Track_Delete(string currentServerPath)
        {
            return TrackOperation(KindOfChange.Delete, currentServerPath, null);
        }

        private ChangeItem Track_Edit(string currentServerPath)
        {
            return TrackOperation(KindOfChange.Edit, currentServerPath, null);
        }

        private ChangeItem Track_Rename(string fromServerPath, string toServerPath)
        {
            return TrackOperation(KindOfChange.Rename, toServerPath, fromServerPath);
        }

        private ChangeItem TrackOperation(KindOfChange kind, string currentServerPath, string previousServerPath)
        {
            // Note arg order changed here!
            var ci = new ChangeItem
                     {
                             ServerPath = currentServerPath,
                             FromServerPath = previousServerPath,
                             Kind = kind
                     };

            var cs = new ChangeSet();
            cs.Id = new StringId(Guid.NewGuid().ToString());
            cs.Items.Add(ci);

            _tracker.TrackId(ci);
            return ci;
        }
    }
}