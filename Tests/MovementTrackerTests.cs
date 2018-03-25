﻿using System;
using System.Collections.Generic;
using System.Linq;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

using NUnit.Framework;

namespace Tests
{
    /// <summary>
    /// All tests read like this:
    /// Commits in the log happened from bottom to top, but are processed
    /// top down from the newest to the oldest commit.
    /// </summary>
    [TestFixture]
    internal sealed class MovementTrackerTests
    {
        private MovementTracker _tracker;
        private ChangeSet _currentChangeSet;



        // Ok this test convinced me that my strategy does not apply to git :)
        //[Test]
        //public void GitHistoryMagic()
        //{
        //    StartChangeSet();
        //    var ci2 = Track_Add(null, "location");
        //    EndChangeSet();

        //    StartChangeSet();
        //    var ci1 = Track_Edit("location");
        //    EndChangeSet();

        //    StartChangeSet();
        //    var ci0 = Track_Delete("location");
        //    EndChangeSet();
        //}

        [Test]
        public void CreateFileOnLocationOfDeletedFile()
        {
            // Act
            StartChangeSet();
            var ci2 = Track_Add(null, "location");
            EndChangeSet();

            StartChangeSet();
            var ci1 = Track_Delete("location");
            EndChangeSet();

            StartChangeSet();
            var ci0 = Track_Edit("location");
            EndChangeSet();

            // Assert
            var id = ci2.Id;
            Assert.AreNotEqual(id, ci1.Id);
            var oldId = ci1.Id;

            Assert.AreEqual(oldId, ci0.Id);
        }

        [Test]
        public void CyclicRenaming_KeepsId()
        {
            StartChangeSet();
            var ci2 = Track_Rename("location_yesterday", "location_now");
            EndChangeSet();

            StartChangeSet();
            var ci1 = Track_Rename("location_now", "location_yesterday");
            EndChangeSet();

            StartChangeSet();
            var ci0 = Track_Rename("location_yesterday", "location_now");
            EndChangeSet();

            var id = ci2.Id;
            Assert.AreEqual(id, ci1.Id);
            Assert.AreEqual(id, ci0.Id);
        }


        [Test]
        public void DeletedItemIsTracked()
        {
            StartChangeSet();
            var ci1 = Track_Delete("location");
            EndChangeSet();

            StartChangeSet();
            var ci0 = Track_Edit("location");
            EndChangeSet();

            var id = ci1.Id;
            Assert.AreEqual(id, ci0.Id);
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

            StartChangeSet();
            Assert.Throws<ArgumentException>(() => tracker.TrackId(ci));
            EndChangeSet();
        }

        [Test]
        public void InvalidArguments_RenameHasNoPreviousServerPath()
        {
            var tracker = new MovementTracker();

            var ci = new ChangeItem
                     {
                             Kind = KindOfChange.Rename
                     };

            StartChangeSet();
            Assert.Throws<ArgumentException>(() => tracker.TrackId(ci));
            EndChangeSet();
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

            var id = ci.Id;
            Assert.NotNull(id);
            Assert.IsTrue(Guid.TryParse(id.ToString(), out var uuid)); // Is a uuid
        }


        [Test]
        public void Regression_DuplicatedKey()
        {
            // Rename a file and add it again at the same location.

            StartChangeSet();
            var ci3 = Track_Delete("bmp");
            EndChangeSet();

            StartChangeSet();
            var ci2 = Track_Add(null, "bmp");
            EndChangeSet();

            StartChangeSet();
            var ci1 = Track_Rename("bmp", "ico");
            EndChangeSet();

            StartChangeSet();
            var ci0 = Track_Add(null, "bmp");
            EndChangeSet();

            var id = ci3.Id;
            Assert.AreEqual(id, ci2.Id);
            Assert.AreNotEqual(id, ci1.Id);
            Assert.AreEqual(ci1.Id, ci0.Id);
        }

        [Test]
        public void Regression_DuplicatedKey2()
        {
            // Act
            StartChangeSet();
            var ci2 = Track_Add(null, "file");
            EndChangeSet();

            StartChangeSet();
            // Move file away so we can create a new version at this location in ci2.
            var ci1 = Track_Rename("file", "somewhere_else");
            EndChangeSet();

            StartChangeSet();
            var ci0 = Track_Add(null, "file");
            EndChangeSet();

            // Assert
            var id = ci2.Id;
            Assert.AreNotEqual(id, ci1.Id);
            Assert.AreEqual(ci1.Id, ci0.Id);
        }

        [Test]
        public void RenamedItemIsTracked()
        {
            StartChangeSet();
            var ci2 = Track_Rename("location_yesterday", "location_now");
            EndChangeSet();

            StartChangeSet();
            var ci1 = Track_Edit("location_yesterday");
            EndChangeSet();

            StartChangeSet();
            var ci0 = Track_Rename("location_very_old", "location_yesterday");
            EndChangeSet();

            // Assert
            var id = ci2.Id;
            Assert.AreEqual(id, ci1.Id);
            Assert.AreEqual(id, ci0.Id);
        }


        [SetUp]
        public void SetUp()
        {
            _tracker = new MovementTracker();
        }

        
        [Test]
        public void Svn_AddDelete_IsReplacedByMove()
        {
            StartChangeSet();
            var ci2 = Track_Delete("location_yesterday");
            var ci1 = Track_Add("location_yesterday", "location_now");
            var cs = EndChangeSet();

            Assert.AreEqual(1, cs.Count);
            Assert.AreEqual(1, _tracker.Warnings.Count);
            Assert.AreEqual(KindOfChange.Rename, cs.Single().Kind);
        }

        [Test]
        public void Svn_AddDelete_IsReplacedByMove_Distinguishes()
        {
            // Recognises this case also if it occurs multiple times in the change set
            StartChangeSet();
            var ci3 = Track_Delete("from_location2");
            var ci2 = Track_Add("from_location2", "now_location2" ); 
            var ci1 = Track_Delete("from_location1");
            var ci0 = Track_Add("from_location1", "now_location1"); 
            var cs = EndChangeSet();

            Assert.AreEqual(2, cs.Count);
            Assert.AreEqual(2, _tracker.Warnings.Count);
            Assert.AreEqual(2, cs.Count(x => x.Kind == KindOfChange.Rename));
        }

        [Test]
        public void Svn_AddDelete_IsNotReplacedByMove_BecauseNoDelete()
        {
            StartChangeSet();
            var ci1 = Track_Add("location_yesterday", "location_now" );
            var cs = EndChangeSet();

            Assert.AreEqual(1, cs.Count);
            Assert.AreEqual(0, _tracker.Warnings.Count);
            Assert.AreEqual(KindOfChange.Add, cs.Single().Kind);
        }

        [Test]
        public void Svn_ConvertMultipleCopiesToAdd_BasedOnAdd()
        {
            StartChangeSet();
            var ci2 = Track_Delete("from_location");
            var ci1 = Track_Add("from_location", "to_location1"); 
            var ci0 = Track_Add("from_location", "to_location2");
            var cs = EndChangeSet();

            Assert.AreEqual(3, cs.Count);
            Assert.AreEqual(2, _tracker.Warnings.Count);
            Assert.AreEqual(2, cs.Count(x => x.Kind == KindOfChange.Add));
            Assert.AreEqual(1, cs.Count(x => x.Kind == KindOfChange.Delete));
        }

        [Test]
        public void Svn_ConvertMultipleCopiesToAdd_BasedOnRename()
        {
            StartChangeSet();
            var ci2 = Track_Delete("from_location");
            var ci1 = Track_Rename("from_location", "to_location1"); // note the order!
            var ci0 = Track_Rename("from_location", "to_location2"); // note the order!
            var cs = EndChangeSet();

            Assert.AreEqual(3, cs.Count);
            Assert.AreEqual(2, _tracker.Warnings.Count);
            Assert.AreEqual(2, cs.Count(x => x.Kind == KindOfChange.Add));
            Assert.AreEqual(1, cs.Count(x => x.Kind == KindOfChange.Delete));
        }

        [Test]
        public void Svn_ConvertRenameToAddIfSourceIsModified()
        {
            // Very special case found only once!
            StartChangeSet();
            var ci2 = Track_Edit("from_location");
            var ci1 = Track_Rename("from_location", "to_location1"); // note the order!
            
            var cs = EndChangeSet();

            Assert.AreEqual(2, cs.Count);
            Assert.AreEqual(1, _tracker.Warnings.Count);
            Assert.AreEqual(1, cs.Count(x => x.Kind == KindOfChange.Add)); // Converted
            Assert.AreEqual(1, cs.Count(x => x.Kind == KindOfChange.Edit));
        }

        [Test]
        public void Git_TreatRenameAsAdd_IfFileIsEditedLater()
        {
            StartChangeSet();
            var ci2 = Track_Edit("the_file");
            var cs = EndChangeSet();

            StartChangeSet();
            var ci1 = Track_Rename("the_file", "to_somwhere_else");
            cs = EndChangeSet();

            Assert.AreEqual(1, cs.Count);
            Assert.AreEqual(1, _tracker.Warnings.Count);
            Assert.AreEqual(1, cs.Count(x => x.Kind == KindOfChange.Add)); // Converted
        }


    
        private List<ChangeItem> EndChangeSet()
        {
            var result = new List<ChangeItem>();
            _tracker.ApplyChangeSet(result);
            return result;
        }

        private void StartChangeSet()
        {
            _currentChangeSet = new ChangeSet();
            _currentChangeSet.Id = new StringId(Guid.NewGuid().ToString());
            _tracker.BeginChangeSet(_currentChangeSet);
        }

        private ChangeItem Track_Add(string previousServerPath, string currentServerPath)
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

            _currentChangeSet.Items.Add(ci);

            _tracker.TrackId(ci);
            return ci;
        }
    }
}