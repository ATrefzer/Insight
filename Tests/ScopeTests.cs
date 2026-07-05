using System;
using Insight.GitProvider;
using NUnit.Framework;

namespace Tests;

[TestFixture]
internal class ScopeTests
{
    [Test]
    public void Update_KeepsReverseMapInSync()
    {
        var scope = new Scope();
        var id = Guid.Parse(scope.Add("A.txt"));

        scope.Update("A.txt", "A_renamed.txt");

        Assert.That(scope.GetServerPath(id), Is.EqualTo("A_renamed.txt"));
        Assert.That(scope.GetIdOrDefault("A_renamed.txt"), Is.EqualTo(id.ToString()));
        Assert.That(scope.GetIdOrDefault("A.txt"), Is.Null);
    }

    [Test]
    public void Update_AfterRename_RemoveCleansBothMaps()
    {
        var scope = new Scope();
        var id = Guid.Parse(scope.Add("A.txt"));

        scope.Update("A.txt", "A_renamed.txt");
        scope.Remove("A_renamed.txt");

        Assert.That(scope.IsKnown("A_renamed.txt"), Is.False);
        Assert.That(scope.IsKnown(id), Is.False);
    }
}
