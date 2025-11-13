using System.Diagnostics;

using LibGit2Sharp;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests
{
    [TestFixture]
    internal class LibGit2Tests
    {
        [Ignore("Only for learning behavior of LibGit2Sharp")]
        [Test]
        public void CheckFileInTree()
        {
            using var repo = new Repository(Constants.NUnitDirectory);

            var commit = repo.Lookup<Commit>("9a98666491219048fd86397c1d1c8ba364cba052");
            var file = commit.Tree["src/NUnitFramework/framework/Interfaces/IReflectionInfo.cs"];
            var name = file.Path;
            Trace.WriteLine(name);

            var notExisting = commit.Tree["src/xxx.cs"];
            Assert.That(notExisting, Is.Null);
        }
    }
}