using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Insight.GitProvider;
using Insight.Shared.Model;

using LibGit2Sharp;

using NUnit.Framework;

namespace Tests
{
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
        public void TestRepo_SingleAdd()
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
        public void TestRepo_AddRemove()
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

        /// <summary>
        /// Found error of single commit not found!
        /// </summary>
        [Test]
        public void TestRepo_AddModifyRemove()
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
        public void CheckFileInTree()
        {
            // Learning behaviour of libgit2
            var nunitRepoPath = @"D:\Private\repos\nunit";

            using (var repo = new Repository(nunitRepoPath))
            {
                var commit = repo.Lookup<Commit>("9a98666491219048fd86397c1d1c8ba364cba052");
                var file = commit.Tree["src/NUnitFramework/framework/Interfaces/IReflectionInfo.cs"];
                var name = file.Path;
                Trace.WriteLine(name);

                var notExisting = commit.Tree["src/xxx.cs"];
                Assert.IsNull(notExisting);
            }
        }


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


        private FollowResult Follow(ChangeSetHistory history, string id)
        {
            var result = new FollowResult();

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

        private static void IncrementOperations(FollowResult result, ChangeItem item)
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
    }
}