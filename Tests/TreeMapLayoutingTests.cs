using Insight.Shared;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using Visualization.Controls.Data;
using Visualization.Controls.TreeMap;

namespace Tests
{
    [TestFixture]
    public class TreeMapLayoutingTests
    {
        /// <summary>
        /// Setting this flag to true allows to generate new test reference data.
        /// I don't want to commit the large binary files. So store the somewhere else.
        /// </summary>
        private const bool GenerateReferenceData = false;

        private string _resourceDirectory;

        private void GenerateTestHierarchicalReferenceFile(string path, double width, double height)
        {
            // Generate reference hierarchical data with layout info
            var generator = new HierarchicalDataBuilder();
            var root = generator.GenerateRandomHierarchy();

            // Needed by tree map algorithm
            root.SumAreaMetrics();

            var layout = new SquarifiedTreeMapLayout();
            layout.Layout(root, width, height);
            var file = new FilePersistence<HierarchicalData>();
            file.Write(path, root);
        }

        [Ignore("Don't want to store the large binary test data in the repo. Generate new one via GenerateTestHierarchicalReferenceFile"), Test]
        public void ReferenceLayout_LargeHierarchicalExample1()
        {
            // Loads a HierarchicalData bin file that includes the layout information and checks
            // if the layouting still produces the same result.

            double width = 100;
            double height = 100;
            var path = Path.Combine(_resourceDirectory, "large_hierarchical_example_treemap1.bin");

            if (GenerateReferenceData)
            {
#pragma warning disable CS0162 // Unreachable code detected
                GenerateTestHierarchicalReferenceFile(path, width, height);
#pragma warning restore CS0162 // Unreachable code detected
            }

            AssertLayouting(path, width, height);
        }

        private static void AssertLayouting(string path, double width, double height)
        {
            var layout = new SquarifiedTreeMapLayout();
            var binFile = new FilePersistence<HierarchicalData>();
            var data = binFile.Read(path);

            // Includes layout info
            var reference = data.Dump();

            Assert.IsTrue(File.Exists(path));

            var clone = (HierarchicalData)data.Clone();
            // Clone does not copy the layout.
            layout.Layout(clone, width, height);
            var result = clone.Dump();

            Assert.AreEqual(reference, result);
        }

        [Ignore("Don't want to store the large binary test data in the repo. Generate new one via GenerateTestHierarchicalReferenceFile"), Test]
        public void ReferenceLayout_LargeHierarchicalExample2()
        {
            // Loads a HierarchicalData bin file that includes the layout information and checks
            // if the layouting still produces the same result.


            const double width = 300;
            const double height = 100;
            var path = Path.Combine(_resourceDirectory, "large_hierarchical_example_treemap2.bin");

            if (GenerateReferenceData)
            {
#pragma warning disable CS0162 // Unreachable code detected
                GenerateTestHierarchicalReferenceFile(path, width, height);
#pragma warning restore CS0162 // Unreachable code detected
            }

            AssertLayouting(path, width, height);
        }

        [Ignore("Don't want to store the large binary test data in the repo. Generate new one via GenerateTestHierarchicalReferenceFile"), Test]
        public void ReferenceLayout_LargeHierarchicalExample3()
        {
            // Loads a HierarchicalData bin file that includes the layout information and checks
            // if the layouting still produces the same result.

            double width = 100;
            double height = 300;

            var path = Path.Combine(_resourceDirectory, "large_hierarchical_example_treemap3.bin");

            if (GenerateReferenceData)
            {
#pragma warning disable CS0162 // Unreachable code detected
                GenerateTestHierarchicalReferenceFile(path, width, height);
#pragma warning restore CS0162 // Unreachable code detected
            }

            AssertLayouting(path, width, height);
        }


        [SetUp]
        public void Setup()
        {
            var assembly = Assembly.GetAssembly(GetType());
            var directory = new FileInfo(assembly.Location).Directory?.FullName ?? ".";
            _resourceDirectory = Path.Combine(directory, @"..\Tests\Resources");
        }
    }
}
