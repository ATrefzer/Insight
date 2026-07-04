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

    /// <summary>
    ///     A conflict resolution differs from both parents, so it is a change done
    ///     in the merge commit itself and has to show up in the history.
    /// </summary>
    [Test]
    public void TwoBranches_ConflictMerge_ResolutionCountsAsChange()
    {
        var repoName = Guid.NewGuid().ToString();
        using (var repo = RepoBuilder.InitNewRepository(repoName))
        {
            repo.AddFile("A.txt");
            repo.Commit("Add A");

            repo.CreateBranch("Feature");
            repo.Checkout("Feature");

            repo.ModifyFileAppend("A.txt", "Modified in Feature");
            repo.Commit("Modify A in Feature");

            repo.Checkout("main");

            repo.ModifyFileAppend("A.txt", "Modified in main");
            repo.Commit("Modify A in main");

            repo.Merge("Feature"); // Conflict in A.txt
            repo.WriteFile("A.txt", "Resolved");
            repo.Commit("Merge Feature into main");


            var history = GetRawHistory(repoName);

            Assert.That(history.ChangeSets.Count, Is.EqualTo(4));

            // Add, modify in Feature, modify in main, resolution in the merge commit
            //                           C  A  M  D  R  C  Final
            AssertFile(history, "A.txt", 4, 1, 3, 0, 0, 0, "A.txt");
        }
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
    ///     Merging unrelated histories creates a second root commit.
    /// </summary>
    [Test]
    public void TwoRoots_MergeUnrelatedHistories()
    {
        var repoName = Guid.NewGuid().ToString();
        using (var repo = RepoBuilder.InitNewRepository(repoName))
        {
            repo.AddFile("A.txt");
            repo.Commit("Add A");

            repo.CheckoutOrphan("Import");
            repo.DeleteFileFromDisk("A.txt");
            repo.AddFile("B.txt");
            repo.Commit("Add B as unrelated root");

            repo.Checkout("main");
            repo.Merge("Import");


            var history = GetRawHistory(repoName);

            // Two root commits and the merge commit (empty, nothing changed in it)
            Assert.That(history.ChangeSets.Count, Is.EqualTo(3));

            //                           C  A  M  D  R  C  Final
            AssertFile(history, "A.txt", 1, 1, 0, 0, 0, 0, "A.txt");
            AssertFile(history, "B.txt", 1, 1, 0, 0, 0, 0, "B.txt");
        }
    }

    /// <summary>
    ///     An octopus merge has more than two parents. File ids from all merged
    ///     branches have to survive the merge.
    /// </summary>
    [Test]
    public void OctopusMerge_ThreeParents_IdsSurvive()
    {
        var repoName = Guid.NewGuid().ToString();
        using (var repo = RepoBuilder.InitNewRepository(repoName))
        {
            repo.AddFile("A.txt");
            repo.Commit("Add A");

            repo.CreateBranch("Feature1");
            repo.CreateBranch("Feature2");

            repo.Checkout("Feature1");
            repo.AddFile("B.txt");
            repo.Commit("Add B");

            repo.Checkout("main");
            repo.Checkout("Feature2");
            repo.AddFile("C.txt");
            repo.Commit("Add C");

            repo.Checkout("main");

            // Build the merged tree (A + B + C) in the index and commit it with three parents.
            repo.AddFile("B.txt");
            repo.AddFile("C.txt");
            repo.CommitMerge("Octopus merge", "Feature1", "Feature2");

            repo.ModifyFileAppend("B.txt", "Modify after merge");
            repo.Commit("Modify B");


            var history = GetRawHistory(repoName);

            // Add A, Add B, Add C, octopus merge (empty), modify B
            Assert.That(history.ChangeSets.Count, Is.EqualTo(5));

            //                           C  A  M  D  R  C  Final
            AssertFile(history, "A.txt", 1, 1, 0, 0, 0, 0, "A.txt");
            AssertFile(history, "B.txt", 2, 1, 1, 0, 0, 0, "B.txt");
            AssertFile(history, "C.txt", 1, 1, 0, 0, 0, 0, "C.txt");
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
        // The history is ordered newest first, so the first item seen has the final name.
        if (result.FinalName == null)
        {
            result.FinalName = item.ServerPath;
        }

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