using System.Linq;
using Insight;
using NUnit.Framework;

namespace Tests;

[TestFixture]
internal class ProjectTests
{
    [Test]
    public void NormalizeFileExtensions()
    {
        var proj = new Project { ExtensionsToInclude = "Xml, .cs; CS   ; JAVA " };

        var normalized = Project.NormalizeFileExtensions(proj.ExtensionsToInclude).ToList();

        // Distinct values.
        // , or ; are accepted as separator.
        // Trimming and ToLower
        Assert.That(normalized.Count, Is.EqualTo(3));
        Assert.That(normalized[0], Is.EqualTo(".xml"));
        Assert.That(normalized[1], Is.EqualTo(".cs"));
        Assert.That(normalized[2], Is.EqualTo(".java"));
    }

    [Test]
    public void NormalizeFileExtensions_EmptyOrNull()
    {
        var proj = new Project();
        proj.ExtensionsToInclude = "";

        // empty
        var normalized = Project.NormalizeFileExtensions(proj.ExtensionsToInclude).ToList();
        Assert.That(normalized.Count, Is.EqualTo(0));

        // null
        proj.ExtensionsToInclude = null;
        normalized = Project.NormalizeFileExtensions(proj.ExtensionsToInclude).ToList();
        Assert.That(normalized.Count, Is.EqualTo(0));
    }
}