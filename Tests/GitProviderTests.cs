using System;
using System.Collections.Generic;
using System.Linq;

using Insight.GitProvider;
using Insight.Shared.Model;

using NUnit.Framework;

namespace Tests
{
    // TODO Extend tests up to the full ArtifactSummary
    [TestFixture]
    class GitProviderTests
    {
        private sealed class FollowResult
        {
            public string FinalName { get; set; }
            public int Add { get; set; }
            public int Modify { get; set; }
            public int Rename { get; set; }
            public int Copy { get; set; }
            public int Delete { get; set; }
            public int ChangesSets { get; set; }
        }

        /// <summary>
        /// Found error of single commit not found!
        /// </summary>
        [Test]
        public void SingleBranch_SingleAdd()
        {
            var repoName = Guid.NewGuid().ToString();
            using (var repo = RepoBuilder.InitNewRepository(repoName))
            {
                repo.AddFile("A.txt");
                repo.Commit("Add A");
            
                
                var history = GetCleanHistory(repoName);

                Assert.AreEqual(1, history.ChangeSets.Count);

                //                           C  A  M  D  R  C  Final
                AssertFile(history, "A.txt", 1, 1, 0, 0, 0, 0, "A.txt");
            }
        }



        [Test]
        public void SingleBranch_AddRemove()
        {
            var repoName = Guid.NewGuid().ToString();
            using (var repo = RepoBuilder.InitNewRepository(repoName))
            {
                repo.AddFile("A.txt");
                repo.Commit("Add A");

                repo.DeleteFile("A.txt");
                repo.Commit("Delete A");
            
                
                var history = GetRawHistory(repoName);

                Assert.AreEqual(2, history.ChangeSets.Count);

                //                           C  A  M  D  R  C  Final
                AssertFile(history, "A.txt", 2, 1, 0, 1, 0, 0, "A.txt");


                // Empty history after cleanup
                var cleanHistory = GetCleanHistory(repoName);
                Assert.AreEqual(0, cleanHistory.ChangeSets.Count);
            }
        }

      
        [Test]
        public void SingleBranch_AddModifyRemove()
        {
            var repoName = Guid.NewGuid().ToString();
            using (var repo = RepoBuilder.InitNewRepository(repoName))
            {
                repo.AddFile("A.txt");
                repo.Commit("Add A");

                repo.ModifyFileAppend("A.txt", "M");
                repo.Commit("Modify A");

                repo.DeleteFile("A.txt");
                repo.Commit();
            
                
                var history = GetRawHistory(repoName);

                Assert.AreEqual(3, history.ChangeSets.Count);

                //                           C  A  M  D  R  C  Final
                AssertFile(history, "A.txt", 3, 1, 1, 1, 0, 0, "A.txt");


                // Empty history after cleanup
                var cleanHistory = GetCleanHistory(repoName);
                Assert.AreEqual(0, cleanHistory.ChangeSets.Count);
            }
        }


        [Test]
        public void SingleBranch_AddModifyRenameModify()
        {
            var repoName = Guid.NewGuid().ToString();
            using (var repo = RepoBuilder.InitNewRepository(repoName))
            {
                repo.AddFile("A.txt");
                repo.Commit("Add A");

                repo.ModifyFileAppend("A.txt", "M");
                repo.Commit("Modify A");

                repo.Rename("A.txt", "A_renamed.txt");
                repo.Commit();

                repo.ModifyFileAppend("A_renamed.txt", "M");
                repo.Commit("Modify A");
            
                
                var history = GetRawHistory(repoName);

                Assert.AreEqual(4, history.ChangeSets.Count);

                //                           C  A  M  D  R  C  Final
                AssertFile(history, "A.txt", 4, 1, 2, 0, 1, 0, "A_renamed.txt");
            }
        }


        [Test]
        public void TwoBranches_ModifySameFile_NoConflicts()
        {

        }

        [Test]
        public void TwoBranches_NoConflictMerge_ModifyAfterMerge()
        {
            var repoName = Guid.NewGuid().ToString();
            using (var repo = RepoBuilder.InitNewRepository(repoName))
            {
                repo.AddFile("A.txt");
                repo.Commit("Add A");

                repo.CreateBranch("Feature");
                repo.Checkout("Feature");

                repo.AddFile("B.txt"); // Just add something to the Feature branch
                repo.Commit("Add B");


                repo.Checkout("master");


                repo.ModifyFileAppend("A.txt", "Modify in master");
                repo.Commit("Modify A");

                repo.Merge("Feature");


                repo.ModifyFileAppend("B.txt", "Modify in master");
                var c = repo.Commit("Modify B after merge");
            
                
                var history = GetRawHistory(repoName);

                Assert.AreEqual(5, history.ChangeSets.Count);

                //                           C  A  M  D  R  C  Final
                AssertFile(history, "A.txt", 2, 1, 1, 0, 0, 0, "A.txt");

                //                           C  A  M  D  R  C  Final
                AssertFile(history, "B.txt", 2, 1, 1, 0, 0, 0, "B.txt");
            }
        }


        [Test]
        public void TwoBranches_NoConflictMerge_RenameFileInFeatureBranch()
        {
            var repoName = Guid.NewGuid().ToString();
            using (var repo = RepoBuilder.InitNewRepository(repoName))
            {
                repo.AddFile("A.txt");
                repo.Commit("Add A");

                repo.CreateBranch("Feature");
                repo.Checkout("Feature");

                repo.Rename("A.txt", "A_renamed.txt");
                repo.Commit("Renamed A.txt -> A_renamed.txt");


                repo.Checkout("master");


                repo.Merge("Feature");


                repo.ModifyFileAppend("A_renamed.txt", "Modify in master");
                var c = repo.Commit("Modify A_renamed after merge");
            
                
                var history = GetRawHistory(repoName);

                Assert.AreEqual(4, history.ChangeSets.Count);


                //                           C  A  M  D  R  C  Final
                AssertFile(history, "A.txt", 3, 1, 1, 0, 1, 0, "A_renamed.txt");
            }
        }



        [Test]
        public void TwoBranches_NoConflictMerge_RenameFileInMasterBranch()
        {
            var repoName = Guid.NewGuid().ToString();
            using (var repo = RepoBuilder.InitNewRepository(repoName))
            {
                repo.AddFile("A.txt");
                repo.Commit("Add A");


                repo.CreateBranch("Feature");
                repo.Checkout("Feature");
                repo.ModifyFileAppend("A.txt", "Modified in Feature");
                repo.Commit("Modified A in Feature");


                repo.Checkout("master");
                repo.Rename("A.txt", "A_renamed.txt");
                repo.Commit("Renamed A.txt -> A_renamed.txt");
                
                repo.Merge("Feature");


                repo.ModifyFileAppend("A_renamed.txt", "Modify in master");
                var c = repo.Commit("Modify A_renamed after merge");


                var history = GetRawHistory(repoName);

                Assert.AreEqual(5, history.ChangeSets.Count);


                //                           C  A  M  D  R  C  Final
                AssertFile(history, "A.txt", 4, 1, 2, 0, 1, 0, "A_renamed.txt");

            }
        }

        #region Test Infrastructure
        /// <summary>
        /// Seed is the file name the file was first committed under.
        /// </summary>
        private void AssertFile(ChangeSetHistory history, string fileSeed, int changeSets, int add, int modify, int delete, int rename, int copy, string finalName)
        {
            var id = FindId(history, fileSeed);
            Assert.IsNotNull(id);
            var stats = Follow(history, id);
            Assert.AreEqual(changeSets, stats.ChangesSets);
            Assert.AreEqual(add, stats.Add, "Add");
            Assert.AreEqual(modify, stats.Modify, "Modify");
            Assert.AreEqual(rename, stats.Rename, "Rename");
            Assert.AreEqual(copy, stats.Copy, "Copy");
            Assert.AreEqual(delete, stats.Delete, "Delete");
            Assert.AreEqual(finalName, stats.FinalName, "FinalName");
        }


        private GitProviderTests.FollowResult Follow(ChangeSetHistory history, string id)
        {
            var result = new GitProviderTests.FollowResult();

            foreach (var cs in history.ChangeSets)
            {
                var ofId = cs.Items.Where(item => item.Id == id).ToList();
                if (ofId.Any())
                {
                    result.ChangesSets++;
                }

                foreach (var item in ofId)
                {
                    IncrementOperations(result, item);
                }
            }

            return result;
        }

        private static void IncrementOperations(GitProviderTests.FollowResult result, ChangeItem item)
        {
            result.FinalName = item.ServerPath;
            if (item.IsAdd())
            {
                result.Add++;
            }

            if (item.IsEdit())
            {
                result.Modify++;
            }

            if (item.IsRename())
            {
                result.Rename++;
            }

            if (item.IsCopy())
            {
                result.Copy++;
            }

            if (item.IsDelete())
            {
                result.Delete++;
            }
        }

        private static string FindId(ChangeSetHistory history, string fileSeed)
        {
            string id = null;
            foreach (var cs in history.ChangeSets)
            {
                // old to new
                foreach (var item in cs.Items)
                {
                    if (item.ServerPath == fileSeed && item.IsAdd())
                    {
                        id = item.Id;
                        break;
                    }
                }
            }

            return id;
        }


        private ChangeSetHistory GetCleanHistory(string repoName)
        {
            using (var cache = new Cache())
            {
                var provider = new GitProvider();
                provider.Initialize(RepoBuilder.GetRepoPath(repoName), cache.ToString(), null);
                provider.UpdateCache(null, false);
                return provider.QueryChangeSetHistory();
            }
        }

        private ChangeSetHistory GetRawHistory(string repoName)
        {
            using (var cache = new Cache())
            {
                var provider = new GitProvider();
                provider.Initialize(RepoBuilder.GetRepoPath(repoName), cache.ToString(), null);
                var (history, graph) = provider.GetRawHistory(null);
                return history;
            }
        }

        #endregion
    }
}