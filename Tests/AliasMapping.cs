using System.Collections.Generic;
using Insight;
using Insight.Shared;
using Insight.Shared.Model;
using NSubstitute;
using NUnit.Framework;

namespace Tests
{
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
            var analyzer = new Analyzer(null, null, null);
            var result = analyzer.AliasTransformContribution(localFileToContribution, mapping);

            // Assert

            Assert.AreEqual(2, result.Count);

            // File 1
            // Work was summed up since both developers are assigned the same alias
            file1Work = result["file1"].DeveloperToContribution;
            Assert.AreEqual(1, file1Work.Count);
            Assert.AreEqual(30, file1Work["MappedA"]);


            // File 2
            file2Work = result["file2"].DeveloperToContribution;
            Assert.AreEqual(2, file2Work.Count);
            Assert.AreEqual(10, file2Work["MappedA"]);
            Assert.AreEqual(20, file2Work["MappedC"]);
        }
    }
}
