using System.Collections.Generic;
using Insight;
using Insight.Shared;
using Insight.Shared.Model;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests
{
    [TestFixture]
    internal sealed class AliasMappingTest
    {
        /// <summary>
        /// Transforms the local files to contribution to use aliases for the developers
        /// If two developers are mapped to the same alias then their works is combined.
        /// </summary>
        [Test]
        public void TransformContribution()
        {
            var mapping = Substitute.For<IAliasMapping>();

            mapping.GetAlias("DeveloperA").Returns("MappedA");
            mapping.GetAlias("DeveloperB").Returns("MappedA");
            mapping.GetAlias("DeveloperC").Returns("MappedC");

            var localFileToContribution = new Dictionary<string, Contribution>();

            var file1Work = new Dictionary<string, uint>{
                {"DeveloperA",10},
                {"DeveloperB", 20}};
            localFileToContribution.Add("file1", new Contribution(file1Work));

            var file2Work = new Dictionary<string, uint>{
                {"DeveloperA",10},
                {"DeveloperC", 20}};
            localFileToContribution.Add("file2", new Contribution(file2Work));


            // Act
            var analyzer = new Analyzer(null, null);
            var result = analyzer.AliasTransformContribution(localFileToContribution, mapping);

            // Assert

            Assert.That(result.Count, Is.EqualTo(2));

            // File 1
            // Work was summed up since both developers are assigned the same alias
            file1Work = result["file1"].DeveloperToContribution;
            Assert.That(file1Work.Count, Is.EqualTo(1));
            Assert.That(file1Work["MappedA"], Is.EqualTo(30));


            // File 2
            file2Work = result["file2"].DeveloperToContribution;
            Assert.That(file2Work.Count, Is.EqualTo(2));
            Assert.That(file2Work["MappedA"], Is.EqualTo(10));
            Assert.That(file2Work["MappedC"], Is.EqualTo(20));
        }
    }
}
