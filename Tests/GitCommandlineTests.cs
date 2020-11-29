using System.IO;
using Insight.GitProvider;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    class GitCommandlineTests
    {
        [Test]
        //[Test, Ignore("")]
        public void Test()
        {
            var cli = new GitCommandLine(Constants.NUnitDirectory);
            var fullLog = cli.Log();
            File.WriteAllText("d:\\full.txt", fullLog);
            var logNoRenames = cli.LogWithoutRenames();
            File.WriteAllText("d:\\simple.txt", logNoRenames);
        }
    }
}
