using Insight.GitProvider;
using Insight.Shared.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    class GraphTests
    {
        [Test]
        public void DeleteSharedHistory_RemovesAllOccurences()
        {
            // Graph
            var graph = new Graph();
            graph.UpdateGraph("child", "parent");
            graph.UpdateGraph("parent", null);

            // Changesets
            var commit_child = new ChangeSet();
            commit_child.Id = "child";

            var commit_parent = new ChangeSet();
            commit_parent.Id = "parent";

            var commits = new List<ChangeSet>();
            commits.Add(commit_child);
            commits.Add(commit_parent);
            commit_child.Items.Add(new ChangeItem { Id = "fileAId", ServerPath = "server_path" });
            commit_child.Items.Add(new ChangeItem { Id = "fileBId", ServerPath = "server_path" });
            commit_parent.Items.Add(new ChangeItem { Id = "fileAId", ServerPath = "server_path" });
            commit_parent.Items.Add(new ChangeItem { Id = "fileBId", ServerPath = "server_path" });

            // Which files to remove and starting from which changeset
            var filesToRemove = new Dictionary<string, string>();
            filesToRemove.Add("fileAId", "child");
            filesToRemove.Add("fileBId", "child");

            // Act
            graph.DeleteSharedHistory(commits, filesToRemove);

            Assert.AreEqual(0, commit_child.Items.Count);
            Assert.AreEqual(0, commit_parent.Items.Count);
        }


        //[Test]
        //public void DeleteSharedHistory_Regression()
        //{

        //    // Graph
        //    var graph = new Graph();
        //    graph.UpdateGraph("child", "parent");
        //    graph.UpdateGraph("parent", null);

        //    // Changesets
        //    var commit_1 = new ChangeSet();
        //    commit_1.Id = "commit1";
        //    var commit_2 = new ChangeSet();
        //    commit_2.Id = "commit2";
        //    var commit_3 = new ChangeSet();
        //    commit_3.Id = "commit3";
        //    var commit_4 = new ChangeSet();
        //    commit_4.Id = "commit4";

        //    var commit_parent = new ChangeSet();
        //    commit_parent.Id = "parent";

        //    var commits = new List<ChangeSet>();
        //    commits.Add(commit_child);
        //    commits.Add(commit_parent);
        //    commit_child.Items.Add(new ChangeItem { Id = "fileAId", ServerPath = "server_path" });
        //    commit_child.Items.Add(new ChangeItem { Id = "fileBId", ServerPath = "server_path" });
        //    commit_parent.Items.Add(new ChangeItem { Id = "fileAId", ServerPath = "server_path" });
        //    commit_parent.Items.Add(new ChangeItem { Id = "fileBId", ServerPath = "server_path" });

        //    // Which files to remove and starting from which changeset
        //    var filesToRemove = new Dictionary<string, string>();
        //    filesToRemove.Add("fileAId", "child");
        //    filesToRemove.Add("fileBId", "child");

        //    // Act
        //    graph.DeleteSharedHistory(commits, filesToRemove);

        //    Assert.AreEqual(0, commit_child.Items.Count);
        //    Assert.AreEqual(0, commit_parent.Items.Count);
        //}
    }
}
