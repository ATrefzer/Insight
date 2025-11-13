using System;
using System.Linq;
using Insight.GitProvider;
using Insight.Shared.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests;

// TODO Extend tests up to the full ArtifactSummary
[TestFixture]
internal class GitProviderTests
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
    ///     Found error of single commit not found!
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

            Assert.That(history.ChangeSets.Count, Is.EqualTo(1));

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

            Assert.That(history.ChangeSets.Count, Is.EqualTo(2));

            //                           C  A  M  D  R  C  Final
            AssertFile(history, "A.txt", 2, 1, 0, 1, 0, 0, "A.txt");


            // Empty history after cleanup
            var cleanHistory = GetCleanHistory(repoName);
            Assert.That(cleanHistory.ChangeSets.Count, Is.EqualTo(0));
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

            Assert.That(history.ChangeSets.Count, Is.EqualTo(3));

            //                           C  A  M  D  R  C  Final
            AssertFile(history, "A.txt", 3, 1, 1, 1, 0, 0, "A.txt");


            // Empty history after cleanup
            var cleanHistory = GetCleanHistory(repoName);
            Assert.That(cleanHistory.ChangeSets.Count, Is.EqualTo(0));
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

            Assert.That(history.ChangeSets.Count, Is.EqualTo(4));

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


            repo.Checkout("main");


            repo.ModifyFileAppend("A.txt", "Modify in main");
            repo.Commit("Modify A");

            repo.Merge("Feature");


            repo.ModifyFileAppend("B.txt", "Modify in main");
            var c = repo.Commit("Modify B after merge");


            var history = GetRawHistory(repoName);

            Assert.That(history.ChangeSets.Count, Is.EqualTo(5));

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


            repo.Checkout("main");


            repo.Merge("Feature");


            repo.ModifyFileAppend("A_renamed.txt", "Modify in main");
            var c = repo.Commit("Modify A_renamed after merge");


            var history = GetRawHistory(repoName);

            Assert.That(history.ChangeSets.Count, Is.EqualTo(4));


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


            repo.Checkout("main");
            repo.Rename("A.txt", "A_renamed.txt");
            repo.Commit("Renamed A.txt -> A_renamed.txt");

            repo.Merge("Feature");

            // Cleanup merge
            repo.DeleteFile("A.txt");
            repo.Commit("Merged");

            repo.ModifyFileAppend("A_renamed.txt", "Modify in main");
            var c = repo.Commit("Modify A_renamed after merge");


            var history = GetRawHistory(repoName);

            Assert.That(history.ChangeSets.Count, Is.EqualTo(5));


            //                           C  A  M  D  R  C  Final
            AssertFile(history, "A.txt", 4, 1, 2, 0, 1, 0, "A_renamed.txt");
        }
    }

    /// <summary>
    ///     Seed is the file name the file was first committed under.
    /// </summary>
    private void AssertFile(ChangeSetHistory history, string fileSeed, int changeSets, int add, int modify, int delete,
        int rename, int copy, string finalName)
    {
        var id = FindId(history, fileSeed);
        ClassicAssert.IsNotNull(id);
        var stats = Follow(history, id);
        Assert.That(stats.ChangesSets, Is.EqualTo(changeSets));
        Assert.That(stats.Add, Is.EqualTo(add), "Add");
        Assert.That(stats.Modify, Is.EqualTo(modify), "Modify");
        Assert.That(stats.Rename, Is.EqualTo(rename), "Rename");
        Assert.That(stats.Copy, Is.EqualTo(copy), "Copy");
        Assert.That(stats.Delete, Is.EqualTo(delete), "Delete");
        Assert.That(stats.FinalName, Is.EqualTo(finalName), "FinalName");
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
            provider.UpdateCache(null, false, null);
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