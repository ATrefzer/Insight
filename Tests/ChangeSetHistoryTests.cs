using System;
using System.Collections.Generic;
using System.IO;
using Insight.Shared;
using Insight.Shared.Model;
using NUnit.Framework;

namespace Tests;

internal sealed class AcceptAllFilter : IFilter
{
    public bool IsAccepted(string path) => true;
}

internal sealed class IdentityAliasMapping : IAliasMapping
{
    public string GetAlias(string name) => name;
    public IEnumerable<string> GetReverse(string alias) => new[] { alias };
}

[TestFixture]
internal class ChangeSetHistoryTests
{
    /// <summary>
    ///     A file that was renamed keeps its id, but its older change items still carry the path it
    ///     had back then. That path no longer exists on disk once the file is renamed away from it.
    ///     GetArtifactSummary must still count those older commits for the (still alive) id - it must
    ///     not drop them just because their historical path doesn't exist anymore.
    /// </summary>
    [Test]
    public void GetArtifactSummary_CountsCommitsBeforeARename()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var currentPath = Path.Combine(tempDir, "New.cs");
            File.WriteAllText(currentPath, "class New {}");

            // Deliberately not created: this simulates the path the file had before it was
            // renamed away - it doesn't exist anymore.
            var oldPath = Path.Combine(tempDir, "Old.cs");

            const string id = "id-1";

            var rename = new ChangeSet { Id = "cs2", Date = new DateTime(2024, 1, 2), Committer = "dev" };
            rename.Items.Add(new ChangeItem
                              {
                                      Id = id, Kind = KindOfChange.Rename, ServerPath = "New.cs",
                                      FromServerPath = "Old.cs", LocalPath = currentPath
                              });

            var edit = new ChangeSet { Id = "cs1", Date = new DateTime(2024, 1, 1), Committer = "dev" };
            edit.Items.Add(new ChangeItem
                           {
                                   Id = id, Kind = KindOfChange.Edit, ServerPath = "Old.cs", LocalPath = oldPath
                           });

            // Newest first, like the real history.
            var history = new ChangeSetHistory(new List<ChangeSet> { rename, edit });

            var summary = history.GetArtifactSummary(new AcceptAllFilter(), new IdentityAliasMapping());

            Assert.That(summary.Count, Is.EqualTo(1));
            Assert.That(summary[0].Commits, Is.EqualTo(2));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
